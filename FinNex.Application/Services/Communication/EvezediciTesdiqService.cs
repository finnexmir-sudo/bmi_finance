// FinNex.Application.Services.Communication/EvezediciTesdiqService.cs
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Communication
{
    public class EvezediciTesdiqService : IEvezediciTesdiqService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBildirisService _bildirisService;

        public EvezediciTesdiqService(IUnitOfWork unitOfWork, IBildirisService bildirisService)
        {
            _unitOfWork = unitOfWork;
            _bildirisService = bildirisService;
        }

        public async Task<Result<IList<EvezediciTesdiqDto>>> GetGozleyenlerAsync(int evezediciIsciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<EvezediciTesdiq>()
                    .HamisiniGetirAsync(
                        predicate: x => x.EvezediciIsciId == evezediciIsciId
                                     && x.Status == EvezediciTesdiqStatus.Gozlemede
                                     && !x.Silinib,
                        include: q => q
                            .Include(e => e.Mezuniyyet)
                                .ThenInclude(m => m.Isci),
                        izlemeden: true);

                var dtos = list.Select(e => ToDto(e)).ToList();
                return Result<IList<EvezediciTesdiqDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<EvezediciTesdiqDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> QebulEtAsync(int tesdiqId, int isciId)
        {
            var e = await _unitOfWork.Repository<EvezediciTesdiq>()
                .GetirAsync(
                    predicate: x => x.Id == tesdiqId && x.EvezediciIsciId == isciId,
                    include: q => q.Include(x => x.Mezuniyyet).ThenInclude(m => m.Isci));

            if (e == null) return Result.Fail("Tapılmadı.");

            e.Status = EvezediciTesdiqStatus.Qebul;
            e.CavabTarixi = DateTime.Now;

            await _unitOfWork.Repository<EvezediciTesdiq>().YenileAsync(e);
            await _unitOfWork.YaddaSaxlaAsync();

            // Müraciət göndərən işçiyə bildiriş
            await _bildirisService.YaratAsync(
                isciId: e.Mezuniyyet.IsciId,
                nov: BildirisNovu.EvezediciQebul,
                bashliq: "Əvəzedici qəbul etdi",
                metn: $"{e.Mezuniyyet.Isci.TamAd}, əvəzedici sizin məzuniyyət müraciətini qəbul etdi.",
                redirectUrl: $"/User/Mezuniyyet/Detail/{e.MezuniyyetId}",
                mezuniyyetId: e.MezuniyyetId);

            return Result.Ok("Sorğu qəbul edildi.");
        }

        public async Task<Result> ReddEtAsync(int tesdiqId, int isciId, string? qeyd)
        {
            var e = await _unitOfWork.Repository<EvezediciTesdiq>()
                .GetirAsync(
                    predicate: x => x.Id == tesdiqId && x.EvezediciIsciId == isciId,
                    include: q => q.Include(x => x.Mezuniyyet).ThenInclude(m => m.Isci));

            if (e == null) return Result.Fail("Tapılmadı.");

            e.Status = EvezediciTesdiqStatus.Redd;
            e.CavabTarixi = DateTime.Now;
            e.Qeyd = qeyd;

            await _unitOfWork.Repository<EvezediciTesdiq>().YenileAsync(e);
            await _unitOfWork.YaddaSaxlaAsync();

            // Müraciət göndərən işçiyə bildiriş
            await _bildirisService.YaratAsync(
                isciId: e.Mezuniyyet.IsciId,
                nov: BildirisNovu.EvezediciRedd,
                bashliq: "Əvəzedici rədd etdi",
                metn: $"Əvəzedici sizin məzuniyyət müraciətini rədd etdi.{(qeyd != null ? " Səbəb: " + qeyd : "")}",
                redirectUrl: $"/User/Mezuniyyet/Detail/{e.MezuniyyetId}",
                mezuniyyetId: e.MezuniyyetId);

            return Result.Ok("Sorğu rədd edildi.");
        }

        public async Task<Result> YaratAsync(int mezuniyyetId, int evezediciIsciId)
        {
            try
            {
                var entity = new EvezediciTesdiq
                {
                    MezuniyyetId = mezuniyyetId,
                    EvezediciIsciId = evezediciIsciId,
                    Status = EvezediciTesdiqStatus.Gozlemede
                };

                await _unitOfWork.Repository<EvezediciTesdiq>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // Əvəzediciyə bildiriş göndər
                var mezuniyyet = await _unitOfWork.Repository<Mezuniyyet>()
                    .GetirAsync(
                        predicate: x => x.Id == mezuniyyetId,
                        include: q => q.Include(m => m.Isci));

                if (mezuniyyet != null)
                {
                    await _bildirisService.YaratAsync(
                        isciId: evezediciIsciId,
                        nov: BildirisNovu.EvezediciSorgu,
                        bashliq: "Əvəzedici sorğusu",
                        metn: $"{mezuniyyet.Isci.TamAd} sizi əvəzedici seçib " +
                              $"({mezuniyyet.BaslamaTarixi:dd.MM.yyyy} — {mezuniyyet.BitmeTarixi:dd.MM.yyyy}). " +
                              $"Qəbul və ya rədd edin.",
                        redirectUrl: $"/User/Inbox",
                        mezuniyyetId: mezuniyyetId);
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<EvezediciTesdiqDto?>> GetByMezuniyyetAsync(int mezuniyyetId)
        {
            var e = await _unitOfWork.Repository<EvezediciTesdiq>()
                .GetirAsync(
                    predicate: x => x.MezuniyyetId == mezuniyyetId && !x.Silinib,
                    include: q => q.Include(x => x.Mezuniyyet).ThenInclude(m => m.Isci));

            if (e == null) return Result<EvezediciTesdiqDto?>.Ok(null);
            return Result<EvezediciTesdiqDto?>.Ok(ToDto(e));
        }

        private static EvezediciTesdiqDto ToDto(EvezediciTesdiq e) => new()
        {
            Id = e.Id,
            MezuniyyetId = e.MezuniyyetId,
            MezuniyyetIsciAd = e.Mezuniyyet?.Isci?.TamAd ?? "",
            MezuniyyetNov = e.Mezuniyyet?.Nov.ToString() ?? "",
            BaslamaTarixi = e.Mezuniyyet?.BaslamaTarixi ?? DateTime.MinValue,
            BitmeTarixi = e.Mezuniyyet?.BitmeTarixi ?? DateTime.MinValue,
            IsGunu = e.Mezuniyyet?.IsGunlerininSayi ?? 0,
            Status = e.Status,
            Qeyd = e.Qeyd,
            CavabTarixi = e.CavabTarixi,
            YaradilmaTarixi = e.YaradilmaTarixi
        };
    }
}