// TapshiriqService.cs
using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Communication
{
    public class TapshiriqService : ITapshiriqService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBildirisService _bildirisService;
        private readonly IMapper _mapper;
        private readonly IJetonTeklifleriService _teklifService;

        public TapshiriqService(IUnitOfWork unitOfWork, IBildirisService bildirisService, IMapper mapper, IJetonTeklifleriService teklifService)
        {
            _unitOfWork = unitOfWork;
            _bildirisService = bildirisService;
            _mapper = mapper;
            _teklifService = teklifService;
        }

        public async Task<Result<IList<TapshiriqListDto>>> GetMenimTapshiriqlarimAsync(int isciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<Tapshiriq>()
                    .HamisiniGetirAsync(
                        predicate: x => x.TeyinOlunanIsciId == isciId && !x.Silinib,
                        include: q => q
                            .Include(t => t.YaradanIsci)
                            .Include(t => t.TeyinOlunanIsci),
                        izlemeden: true);

                // Gecikmiş tapşırıqları avtomatik yenilə
                foreach (var t in list.Where(t =>
                    t.SonTarix < DateTime.Today &&
                    t.Status != TapshiriqStatus.Tamamlandi &&
                    t.Status != TapshiriqStatus.LegvEdildi &&
                    t.Status != TapshiriqStatus.Gecikdi))
                {
                    t.Status = TapshiriqStatus.Gecikdi;
                    await _unitOfWork.Repository<Tapshiriq>().YenileAsync(t);
                }
                await _unitOfWork.YaddaSaxlaAsync();

                return Result<IList<TapshiriqListDto>>.Ok(
                    list.OrderByDescending(x => x.YaradilmaTarixi)
                        .Select(ToListDto).ToList());
            }
            catch (Exception ex)
            {
                return Result<IList<TapshiriqListDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<IList<TapshiriqListDto>>> GetVerdiklerimAsync(int isciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<Tapshiriq>()
                    .HamisiniGetirAsync(
                        predicate: x => x.YaradanIsciId == isciId && !x.Silinib,
                        include: q => q
                            .Include(t => t.YaradanIsci)
                            .Include(t => t.TeyinOlunanIsci),
                        izlemeden: true);

                return Result<IList<TapshiriqListDto>>.Ok(
                    list.OrderByDescending(x => x.YaradilmaTarixi)
                        .Select(ToListDto).ToList());
            }
            catch (Exception ex)
            {
                return Result<IList<TapshiriqListDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<IList<TapshiriqListDto>>> GetKomandaTapshiriqlariAsync(int isciId)
        {
            try
            {
                // İşçinin departamentini tap
                var teyinat = await _unitOfWork.Repository<IsciTeyinat>()
                    .GetirAsync(x => x.IsciId == isciId && x.Aktivdir);

                if (teyinat == null)
                    return Result<IList<TapshiriqListDto>>.Ok(new List<TapshiriqListDto>());

                var list = await _unitOfWork.Repository<Tapshiriq>()
                    .HamisiniGetirAsync(
                        predicate: x => !x.Silinib &&
                            x.TeyinOlunanIsci.IsciTeyinatlari
                                .Any(t => t.Aktivdir && t.DepartamentId == teyinat.DepartamentId),
                        include: q => q
                            .Include(t => t.YaradanIsci)
                            .Include(t => t.TeyinOlunanIsci)
                                .ThenInclude(i => i.IsciTeyinatlari),
                        izlemeden: true);

                return Result<IList<TapshiriqListDto>>.Ok(
                    list.OrderByDescending(x => x.YaradilmaTarixi)
                        .Select(ToListDto).ToList());
            }
            catch (Exception ex)
            {
                return Result<IList<TapshiriqListDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<TapshiriqDetailDto>> GetDetayAsync(int id, int isciId)
        {
            try
            {
                var t = await _unitOfWork.Repository<Tapshiriq>()
                    .GetirAsync(
                        predicate: x => x.Id == id && !x.Silinib,
                        include: q => q
                            .Include(t => t.YaradanIsci)
                            .Include(t => t.TeyinOlunanIsci)
                            .Include(t => t.Sherhler)
                                .ThenInclude(s => s.MuellifIsci));

                if (t == null) return Result<TapshiriqDetailDto>.Fail("Tapılmadı.");

                var dto = new TapshiriqDetailDto
                {
                    Id = t.Id,
                    Bashliq = t.Bashliq,
                    Tesvir = t.Tesvir,
                    YaradanAd = t.YaradanIsci.TamAd,
                    YaradanIsciId = t.YaradanIsciId,
                    TeyinOlunanAd = t.TeyinOlunanIsci.TamAd,
                    TeyinOlunanIsciId = t.TeyinOlunanIsciId,
                    SonTarix = t.SonTarix,
                    Prioritet = t.Prioritet,
                    Status = t.Status,
                    TamamlanmaFaizi = t.TamamlanmaFaizi,
                    Qeyd = t.Qeyd,
                    YaradilmaTarixi = t.YaradilmaTarixi,
                    Sherhler = t.Sherhler
                        .Where(s => !s.Silinib)
                        .OrderBy(s => s.YaradilmaTarixi)
                        .Select(s => new TapshiriqSherhDto
                        {
                            Id = s.Id,
                            MuellifAd = s.MuellifIsci.TamAd,
                            MuellifIsciId = s.MuellifIsciId,
                            Metn = s.Metn,
                            YaradilmaTarixi = s.YaradilmaTarixi
                        }).ToList()
                };

                return Result<TapshiriqDetailDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<TapshiriqDetailDto>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> YaratAsync(TapshiriqCreateDto dto)
        {
            try
            {
                var entity = new Tapshiriq
                {
                    Bashliq = dto.Bashliq,
                    Tesvir = dto.Tesvir,
                    YaradanIsciId = dto.YaradanIsciId,
                    TeyinOlunanIsciId = dto.TeyinOlunanIsciId,
                    SonTarix = dto.SonTarix,
                    Prioritet = dto.Prioritet,
                    Qeyd = dto.Qeyd,
                    Status = TapshiriqStatus.Yeni
                };

                await _unitOfWork.Repository<Tapshiriq>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // Tapşırıq alanına bildiriş
                var yaradan = await _unitOfWork.Repository<Isci>()
                    .IdIleGetirAsync(dto.YaradanIsciId);

                await _bildirisService.YaratAsync(
                    isciId: dto.TeyinOlunanIsciId,
                    nov: BildirisNovu.YeniTapshiriq,
                    bashliq: "Yeni tapşırıq",
                    metn: $"{yaradan?.TamAd} sizə tapşırıq verdi: {dto.Bashliq}",
                    redirectUrl: $"/User/Tapshiriq/Detay/{entity.Id}");

                return Result.Ok("Tapşırıq yaradıldı.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> StatusYenileAsync(TapshiriqUpdateDto dto, int isciId)
        {
            try
            {
                var t = await _unitOfWork.Repository<Tapshiriq>()
                    .GetirAsync(x => x.Id == dto.Id);

                if (t == null) return Result.Fail("Tapılmadı.");
                if (t.TeyinOlunanIsciId != isciId && t.YaradanIsciId != isciId)
                    return Result.Fail("İcazəniz yoxdur.");

                t.Status = dto.Status;
                t.TamamlanmaFaizi = dto.TamamlanmaFaizi;
                if (dto.Qeyd != null) t.Qeyd = dto.Qeyd;

                await _unitOfWork.Repository<Tapshiriq>().YenileAsync(t);
                await _unitOfWork.YaddaSaxlaAsync();

                // Tapşırıq tamamlandısa yaradan işçiyə bildiriş
                if (dto.Status == TapshiriqStatus.Tamamlandi)
                {
                    t.TamamlanmaFaizi = 100;
                    await _bildirisService.YaratAsync(
                        isciId: t.YaradanIsciId,
                        nov: BildirisNovu.TapshiriqTamamlandi,
                        bashliq: "Tapşırıq tamamlandı",
                        metn: $"'{t.Bashliq}' tapşırığı tamamlandı.",
                        redirectUrl: $"/User/Tapshiriq/Detay/{t.Id}");
                    _ = _teklifService.TapshiriqTamamlandiAsync(t.Id);
                }
                else
                    t.TamamlanmaFaizi = dto.TamamlanmaFaizi;

                return Result.Ok("Yeniləndi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> SherhEleveEtAsync(int tapshiriqId, int isciId, string metn)
        {
            try
            {
                var t = await _unitOfWork.Repository<Tapshiriq>()
                    .IdIleGetirAsync(tapshiriqId);

                if (t == null) return Result.Fail("Tapılmadı.");
                if (t.TeyinOlunanIsciId != isciId && t.YaradanIsciId != isciId)
                    return Result.Fail("İcazəniz yoxdur.");

                var sherh = new TapshiriqSherh
                {
                    TapshiriqId = tapshiriqId,
                    MuellifIsciId = isciId,
                    Metn = metn
                };

                await _unitOfWork.Repository<TapshiriqSherh>().YaratAsync(sherh);
                await _unitOfWork.YaddaSaxlaAsync();

                // Digər tərəfə bildiriş
                int alanIsciId = isciId == t.YaradanIsciId
                    ? t.TeyinOlunanIsciId
                    : t.YaradanIsciId;

                await _bildirisService.YaratAsync(
                    isciId: alanIsciId,
                    nov: BildirisNovu.YeniMesaj,
                    bashliq: "Tapşırığa şərh",
                    metn: $"'{t.Bashliq}' tapşırığına yeni şərh əlavə edildi.",
                    redirectUrl: $"/User/Tapshiriq/Detay/{tapshiriqId}");

                return Result.Ok("Şərh əlavə edildi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> SilAsync(int id, int isciId)
        {
            var t = await _unitOfWork.Repository<Tapshiriq>()
                .GetirAsync(x => x.Id == id);

            if (t == null) return Result.Fail("Tapılmadı.");
            if (t.YaradanIsciId != isciId) return Result.Fail("Yalnız yaradan silə bilər.");

            await _unitOfWork.Repository<Tapshiriq>().YumshakSilAsync(id);
            await _unitOfWork.YaddaSaxlaAsync();
            return Result.Ok("Silindi.");
        }

        public async Task<Result<IList<TapshiriqListDto>>> GetGecikenleriAsync()
        {
            var list = await _unitOfWork.Repository<Tapshiriq>()
                .HamisiniGetirAsync(
                    predicate: x => !x.Silinib &&
                        x.SonTarix < DateTime.Today &&
                        x.Status != TapshiriqStatus.Tamamlandi &&
                        x.Status != TapshiriqStatus.LegvEdildi,
                    include: q => q
                        .Include(t => t.YaradanIsci)
                        .Include(t => t.TeyinOlunanIsci),
                    izlemeden: true);

            return Result<IList<TapshiriqListDto>>.Ok(list.Select(ToListDto).ToList());
        }

        private static TapshiriqListDto ToListDto(Tapshiriq t) => new()
        {
            Id = t.Id,
            Bashliq = t.Bashliq,
            Tesvir = t.Tesvir,
            YaradanAd = t.YaradanIsci?.TamAd ?? "",
            YaradanIsciId = t.YaradanIsciId,
            TeyinOlunanAd = t.TeyinOlunanIsci?.TamAd ?? "",
            TeyinOlunanIsciId = t.TeyinOlunanIsciId,
            SonTarix = t.SonTarix,
            Prioritet = t.Prioritet,
            Status = t.Status,
            TamamlanmaFaizi = t.TamamlanmaFaizi,
            YaradilmaTarixi = t.YaradilmaTarixi
        };

        public async Task<Result> EditAsync(TapshiriqEditDto dto)
        {
            try
            {
                var t = await _unitOfWork.Repository<Tapshiriq>()
                    .GetirAsync(x => x.Id == dto.Id);

                if (t == null) return Result.Fail("Tapılmadı.");
                if (t.YaradanIsciId != dto.YaradanIsciId)
                    return Result.Fail("Yalnız yaradan redaktə edə bilər.");
                if (t.Status == TapshiriqStatus.Tamamlandi ||
                    t.Status == TapshiriqStatus.LegvEdildi)
                    return Result.Fail("Bu tapşırıq redaktə edilə bilməz.");

                t.Bashliq = dto.Bashliq;
                t.Tesvir = dto.Tesvir;
                t.TeyinOlunanIsciId = dto.TeyinOlunanIsciId;
                t.SonTarix = dto.SonTarix;
                t.Prioritet = dto.Prioritet;
                t.Qeyd = dto.Qeyd;

                await _unitOfWork.Repository<Tapshiriq>().YenileAsync(t);
                await _unitOfWork.YaddaSaxlaAsync();

                // Tapşırıq alanına bildiriş
                await _bildirisService.YaratAsync(
                    isciId: t.TeyinOlunanIsciId,
                    nov: BildirisNovu.YeniTapshiriq,
                    bashliq: "Tapşırıq yeniləndi",
                    metn: $"'{t.Bashliq}' tapşırığı yeniləndi.",
                    redirectUrl: $"/User/Tapshiriq/Detay/{t.Id}");

                return Result.Ok("Tapşırıq yeniləndi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }
        public async Task<Result<IList<TapshiriqListDto>>> GetYaxinlasanlarAsync(DateTime tarix)
        {
            try
            {
                var start = tarix.Date;
                var end = tarix.Date.AddDays(1);

                var data = await _unitOfWork.Repository<Tapshiriq>()
                    .HamisiniGetirAsync(
                        x => !x.Silinib
                             && x.SonTarix >= start
                             && x.SonTarix < end
                             && x.Status != TapshiriqStatus.Tamamlandi
                             && x.Status != TapshiriqStatus.LegvEdildi
                    );

                var dto = _mapper.Map<IList<TapshiriqListDto>>(data);
                return Result<IList<TapshiriqListDto>>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<IList<TapshiriqListDto>>.Fail($"Tapşırıqlar gətirilə bilmədi: {ex.Message}");
            }
        }
    }
}