// FinNex.Application.Services.Communication/EvezediciTesdiqService.cs
using FinNex.Application.Common.Extensions;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Communication
{
    public class EvezediciTesdiqService : IEvezediciTesdiqService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBildirisService _bildirisService;
        private readonly IBildirisRouter _bildirisRouter;
        private readonly UserManager<AppUser> _userManager;
        private readonly IJetonTeklifleriService _teklifService;

        public EvezediciTesdiqService(
            IUnitOfWork unitOfWork,
            IBildirisService bildirisService,
            IBildirisRouter bildirisRouter,
            UserManager<AppUser> userManager,
            IJetonTeklifleriService teklifService)
        {
            _unitOfWork = unitOfWork;
            _bildirisService = bildirisService;
            _bildirisRouter = bildirisRouter;
            _userManager = userManager;
            _teklifService = teklifService;
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
                    include: q => q.Include(x => x.Mezuniyyet).ThenInclude(m => m.Isci)
                                    .Include(x => x.Mezuniyyet).ThenInclude(m => m.Isci)
                                        .ThenInclude(i => i.IsciTeyinatlari));

            if (e == null) return Result.Fail("Tapılmadı.");

            e.Status = EvezediciTesdiqStatus.Qebul;
            e.CavabTarixi = DateTime.Now;

            await _unitOfWork.Repository<EvezediciTesdiq>().YenileAsync(e);

            // Məzuniyyət statusunu əvəzedici gözləmədən sonrakı addıma keçir
            var mezuniyyet = e.Mezuniyyet;
            if (mezuniyyet.Status == MezuniyyetStatus.Gozlemede)
            {
                // Müraciət sahibinin rolunu yoxla
                var appUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.IsciId == mezuniyyet.IsciId);

                MezuniyyetStatus sonrakiStatus;

                if (appUser != null && await _userManager.IsInRoleAsync(appUser, RoleNames.Rehber))
                {
                    sonrakiStatus = MezuniyyetStatus.HrTesdiqinde;
                }
                else if (appUser != null && await _userManager.IsInRoleAsync(appUser, RoleNames.SobeReisi))
                {
                    sonrakiStatus = MezuniyyetStatus.RehberTesdiqinde;
                }
                else
                {
                    // Adi işçi — şöbə rəisi varsa ona, yoxsa rəhbərə
                    var teyinat = mezuniyyet.Isci.IsciTeyinatlari?.FirstOrDefault(t => t.Aktivdir);
                    var sobeReisiVar = teyinat != null && await _unitOfWork.Repository<IsciStrukturRolu>()
                        .MovcuddurmuAsync(x =>
                            x.DepartamentId == teyinat.DepartamentId &&
                            x.RolTipi == StrukturRolTipi.SobeReisi &&
                            x.IsciId != mezuniyyet.IsciId &&
                            x.Aktivdir);

                    sonrakiStatus = sobeReisiVar
                        ? MezuniyyetStatus.SobeReisiTesdiqinde
                        : MezuniyyetStatus.RehberTesdiqinde;
                }

                mezuniyyet.Status = sonrakiStatus;
                await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(mezuniyyet);
            }

            await _unitOfWork.YaddaSaxlaAsync();

            // Müraciət göndərən işçiyə bildiriş
            await _bildirisService.YaratAsync(
                isciId: e.Mezuniyyet.IsciId,
                nov: BildirisNovu.EvezediciQebul,
                bashliq: "Əvəzedici qəbul etdi",
                metn: $"{e.Mezuniyyet.Isci.TamAd}, əvəzedici sizin məzuniyyət müraciətini qəbul etdi.",
                redirectUrl: $"/User/Mezuniyyet/Detail/{e.MezuniyyetId}",
                mezuniyyetId: e.MezuniyyetId);

            // Növbəti mərhələ təsdiqçilərinə — indi müraciət aktiv mərhələyə düşdü
            await NotifyNextStageApproversAsync(e.Mezuniyyet);

            await _teklifService.EvezediciQebulEdildiAsync(tesdiqId);

            return Result.Ok("Sorğu qəbul edildi.");
        }

        private async Task NotifyNextStageApproversAsync(Mezuniyyet m)
        {
            var dovr = $"{m.BaslamaTarixi:dd.MM.yyyy} – {m.BitmeTarixi:dd.MM.yyyy}";
            var bashliq = "Yeni məzuniyyət müraciəti";
            var metn = $"{m.Isci?.TamAd ?? $"İşçi #{m.IsciId}"} ({dovr}) məzuniyyət müraciəti göndərdi (əvəzedici qəbul edildi).";

            switch (m.Status)
            {
                case MezuniyyetStatus.SobeReisiTesdiqinde:
                    var teyinat = m.Isci?.IsciTeyinatlari?.FirstOrDefault(t => t.Aktivdir);
                    if (teyinat != null)
                    {
                        await _bildirisRouter.NotifyDepartmentRoleAsync(
                            teyinat.DepartamentId, StrukturRolTipi.SobeReisi,
                            BildirisNovu.MezuniyyetMuraciet, bashliq, metn,
                            redirectUrl: $"/User/Tesdiq/MezuniyyetDetal/{m.Id}?rol=SobeReisi",
                            mezuniyyetId: m.Id, exceptIsciId: m.IsciId);
                    }
                    break;

                case MezuniyyetStatus.RehberTesdiqinde:
                    await _bildirisRouter.NotifyRolesAsync(
                        new[] { RoleNames.Rehber, RoleNames.Admin },
                        BildirisNovu.MezuniyyetMuraciet, bashliq, metn,
                        redirectUrl: $"/User/Tesdiq/MezuniyyetDetal/{m.Id}?rol=Rehber",
                        mezuniyyetId: m.Id, exceptIsciId: m.IsciId);
                    break;

                case MezuniyyetStatus.HrTesdiqinde:
                    await _bildirisRouter.NotifyRolesAsync(
                        new[] { RoleNames.HR, RoleNames.Admin },
                        BildirisNovu.MezuniyyetMuraciet, bashliq, metn,
                        redirectUrl: $"/User/Tesdiq/MezuniyyetDetal/{m.Id}?rol=Hr",
                        mezuniyyetId: m.Id, exceptIsciId: m.IsciId);
                    break;
            }
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
                        redirectUrl: $"/User/Inbox?tab=sorgular",
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
            MezuniyyetNov = e.Mezuniyyet != null ? e.Mezuniyyet.Nov.Adi() : "",
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