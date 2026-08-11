using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Icaze;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FinNex.Domain;

namespace FinNex.Application.Services
{
    public class IcazeService : IIcazeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IBildirisRouter _bildirisRouter;
        private readonly IJetonService _jetonService;

        public IcazeService(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IMapper mapper,
            IBildirisRouter bildirisRouter,
            IJetonService jetonService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _mapper = mapper;
            _bildirisRouter = bildirisRouter;
            _jetonService = jetonService;
        }

        public async Task<Result<IList<IcazeListDto>>> GetIsciIcazeleriAsync(int isciId)
        {
            try
            {
                // Bağlanmamış icazələrdə cihaz çıxış/qayıdışını xam punch-lardan bərpa et
                // ki, faktiki saat dəqiq olsun (HR İzləmə səhifəsi ilə eyni mənbə).
                await IcazeCihazVaxtBerpaEtAsync();

                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        x => x.IsciId == isciId,
                        include: q => q
                            .Include(m => m.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Departament)
                            .Include(m => m.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Vezife)
                            .Include(m => m.EvezEdenIsci)
                            .Include(m => m.CixisGiris),
                        izlemeden: true);

                var dtos = list
                    .OrderByDescending(x => x.IcazeTarixi)
                    .Select(MapToListDto)
                    .ToList();

                // Effektiv saat (nahar çıxılmaqla) üçün nahar parametrlərini cari dəyərdən doldur.
                var isPrm = await _unitOfWork.Repository<IsParametri>()
                    .Query().AsNoTracking().Where(p => !p.Silinib).FirstOrDefaultAsync();
                foreach (var d in dtos)
                {
                    d.NaharDeqiqe = isPrm?.NaharMuddetDeqiqe ?? 45;
                }

                return Result<IList<IcazeListDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Icazeler getirilmedi: {ex.Message}");
            }
        }

        public async Task<int> NaharMuddetDeqiqeAsync()
        {
            var p = await _unitOfWork.Repository<IsParametri>()
                .Query().AsNoTracking().Where(x => !x.Silinib).FirstOrDefaultAsync();
            return p?.NaharMuddetDeqiqe ?? 45;
        }

        public async Task<Result<IcazeListDto>> YaratAsync(IcazeCreateDto dto)
        {
            try
            {
                if (dto.BitisSaati <= dto.BaslamaSaati)
                    return Result<IcazeListDto>.Fail("Bitme saati baslama saatindan sonra olmalidir.");

                // Səbəb məcburidir
                if (string.IsNullOrWhiteSpace(dto.Sebeb))
                    return Result<IcazeListDto>.Fail("İcazə səbəbi mütləq qeyd edilməlidir.");

                // Maksimum müddət — adi icazə 3 saat (180 dəq). Nahara çıxmayan işçi nahar
                // fasiləsi qədər kredit qazanır → pəncərə o qədər uzun ola bilər, ƏVƏZİNDƏ
                // sayğacdan həmin müddət çıxılır (NaharCixilmaSaat). Beləcə sayılan icazə
                // hər halda 3 saatı keçmir. Nahar müddəti IsParametri-dən oxunur (default 45).
                var yaratParam = await _unitOfWork.Repository<IsParametri>()
                    .Query().AsNoTracking().Where(x => !x.Silinib).FirstOrDefaultAsync();
                var yaratNaharDeq = yaratParam?.NaharMuddetDeqiqe ?? 45;

                var icazeDeq = (int)(dto.BitisSaati - dto.BaslamaSaati).TotalMinutes;
                var maxDeq = dto.NaharNezereAlinmasin ? 180 + yaratNaharDeq : 180;
                if (icazeDeq > maxDeq)
                {
                    var maxSaat = maxDeq / 60; var maxQaliq = maxDeq % 60;
                    // Çıxışlıq hal şəkilçisi son sözə görə dəyişir: "...dəqiqədən" / "...saatdan".
                    var maxMetn = maxQaliq > 0
                        ? $"{maxSaat} saat {maxQaliq} dəqiqədən"
                        : $"{maxSaat} saatdan";
                    return Result<IcazeListDto>.Fail(
                        dto.NaharNezereAlinmasin
                            ? $"Nahar icazəyə qatıldıqda icazə {maxMetn} çox ola bilməz."
                            : $"İcazə 3 saatdan çox ola bilməz. Nahara çıxmırsınızsa müraciətdə qeyd edin (max {180 + yaratNaharDeq} dəqiqə).");
                }

                // YENİ AXİN:
                //   Rəhbər müraciəti   → birbaşa Tesdiqlenib (özü Rəhbər olduğu üçün)
                //   HR müraciəti        → birbaşa Tesdiqlenib (özü HR olduğu üçün)
                //   SobeReisi müraciəti → Rəhbər → Tesdiqlenib
                //   Adi işçi:
                //       Rəhbər varsa   → RehberTesdiqinde
                //       Rəhbər yoxdursa → HrTesdiqinde (HR fallback)
                //
                //   Şöbə Rəisi artıq təsdiq zəncirindən ÇIXARILDI.
                //   Yalnız bildiriş alır (tesdiqden sonra).

                IcazeStatus ilkinStatus;

                if (dto.MuracietSahibiRehberdirmi)
                {
                    // Rəhbər özü müraciət edir — heç kim tələb olunmur, birbaşa tesdiq
                    ilkinStatus = IcazeStatus.Tesdiqlenib;
                }
                else if (dto.MuracietSahibiHrdirmi)
                {
                    // HR özü müraciət edir — Rəhbər təsdiqindən keçsin
                    ilkinStatus = IcazeStatus.RehberTesdiqinde;
                }
                else if (dto.MuracietSahibiSobeReisidirmi)
                {
                    // Şöbə rəisi öz addımını keçir — Rəhbərə gedir
                    ilkinStatus = IcazeStatus.RehberTesdiqinde;
                }
                else
                {
                    // Adi işçi — Rəhbər varmı yoxla
                    bool rehberVar = await _unitOfWork.Repository<IsciStrukturRolu>()
                        .MovcuddurmuAsync(x =>
                            x.RolTipi == StrukturRolTipi.Rehber &&
                            x.Aktivdir);

                    ilkinStatus = rehberVar
                        ? IcazeStatus.RehberTesdiqinde
                        : IcazeStatus.HrTesdiqinde;
                }

                var entity = new Icaze
                {
                    IsciId = dto.IsciId,
                    EvezEdenIsciId = dto.EvezEdenIsciId,
                    IcazeTarixi = dto.IcazeTarixi,
                    BaslamaSaati = dto.BaslamaSaati,
                    BitisSaati = dto.BitisSaati,
                    Sebeb = dto.Sebeb,
                    NaharNezereAlinmasin = dto.NaharNezereAlinmasin,
                    Status = ilkinStatus
                };

                await _unitOfWork.Repository<Icaze>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // Rəhbər özü müraciət edibsə — IcazeCixisGiris dərhal yarat
                if (ilkinStatus == IcazeStatus.Tesdiqlenib)
                {
                    // Jeton ödəməsi varsa — yoxla və entityə yaz
                    if (dto.JetonOdenenSaat > 0)
                    {
                        var icazeSaati = (decimal)(dto.BitisSaati - dto.BaslamaSaati).TotalHours;
                        if (dto.JetonOdenenSaat > icazeSaati)
                            return Result<IcazeListDto>.Fail(
                                $"Jeton ödənişi ({dto.JetonOdenenSaat:0.##} saat) icazənin ümumi saatından ({icazeSaati:0.##} saat) çox ola bilməz.");
                        var balans = await _jetonService.AktivSaatBalansiAsync(dto.IsciId);
                        if (dto.JetonOdenenSaat > balans)
                            return Result<IcazeListDto>.Fail(
                                $"Jeton balansı kifayət etmir ({balans:0.##} saat mövcud, {dto.JetonOdenenSaat:0.##} saat tələb olundu).");
                        entity.JetonOdenenSaat = dto.JetonOdenenSaat;
                    }
                    entity.RehberTesdiq = true;
                    entity.RehberTesdiqTarixi = DateTime.Now;
                    entity.HrTesdiq = true;
                    entity.HrTesdiqTarixi = DateTime.Now;
                    await _unitOfWork.Repository<Icaze>().YenileAsync(entity);
                    await _unitOfWork.YaddaSaxlaAsync();
                    if (dto.JetonOdenenSaat > 0)
                    {
                        var jetonRes = await _ConsumeJetonsForIcazeAsync(entity);
                        if (!jetonRes.Success)
                            return Result<IcazeListDto>.Fail(jetonRes.Message ?? "Jeton xərclənmə xətası.");
                    }
                    await _YaratCixisGirisAsync(entity.Id, false);
                    await _GecikmeYenileAsync(entity);
                    await NotifySobeReisiAsync(entity);
                    await NotifyHrMalumatAsync(entity);
                }

                var saved = await _unitOfWork.Repository<Icaze>()
                    .GetirAsync(
                        x => x.Id == entity.Id,
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Departament)
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Vezife)
                            .Include(i => i.EvezEdenIsci)
                            .Include(i => i.CixisGiris),
                        izlemeden: true);

                await NotifyApproversForCreateAsync(entity);

                return Result<IcazeListDto>.Ok(MapToListDto(saved!), "Icaze muracietiniz gonderildi.");
            }
            catch (Exception ex)
            {
                return Result<IcazeListDto>.Fail($"Yaradilmadi: {ex.Message}");
            }
        }

        public async Task<Result> LegvEtAsync(int icazeId, int isciId)
        {
            try
            {
                var icaze = await _unitOfWork.Repository<Icaze>().IdIleGetirAsync(icazeId);

                if (icaze == null)
                    return Result.Fail("Icaze tapilmadi.");

                if (icaze.IsciId != isciId)
                    return Result.Fail("Bu icaze size aid deyil.");

                if (icaze.Status == IcazeStatus.Tesdiqlenib || icaze.Status == IcazeStatus.ImtinaEdildi)
                    return Result.Fail("Hələ təsdiq olunmamış icazə ləğv edilə bilər.");

                // CixisGiris varsa statusunu LegvEdildi et
                var cixisGiris = await _unitOfWork.Repository<IcazeCixisGiris>()
                    .GetirAsync(x => x.IcazeId == icazeId);
                if (cixisGiris != null)
                {
                    cixisGiris.Status = IcazeCixisGirisStatus.LegvEdildi;
                    await _unitOfWork.Repository<IcazeCixisGiris>().YenileAsync(cixisGiris);
                }

                await _unitOfWork.Repository<Icaze>().YumshakSilAsync(icazeId);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok("Icaze legv edildi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Legv zamani xeta: {ex.Message}");
            }
        }

        // Rəhbər/HR — təsdiqlənmiş icazəni ləğv edir (sahibə bağlı deyil).
        // Səbəb məcburi; keçmiş icazə ləğv olunmur; jeton istifadə olunubsa geri qaytarılır.
        public async Task<Result> RehberHrLegvEtAsync(int icazeId, int legvEdenIsciId, string sebeb)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sebeb))
                    return Result.Fail("Ləğv səbəbi mütləqdir.");

                var icaze = await _unitOfWork.Repository<Icaze>().IdIleGetirAsync(icazeId);
                if (icaze == null)
                    return Result.Fail("İcazə tapılmadı.");
                if (icaze.Status != IcazeStatus.Tesdiqlenib)
                    return Result.Fail("Yalnız təsdiqlənmiş icazə ləğv edilə bilər.");
                if (icaze.IcazeTarixi.Date < DateTime.Today)
                    return Result.Fail("Keçmiş icazəni ləğv etmək mümkün deyil.");

                // CixisGiris varsa ləğv et
                var cixisGiris = await _unitOfWork.Repository<IcazeCixisGiris>()
                    .GetirAsync(x => x.IcazeId == icazeId);
                if (cixisGiris != null)
                {
                    cixisGiris.Status = IcazeCixisGirisStatus.LegvEdildi;
                    await _unitOfWork.Repository<IcazeCixisGiris>().YenileAsync(cixisGiris);
                }

                // Jeton geri qaytar (istifadə olunubsa) — stage edir, aşağıdakı save ilə birgə
                if (icaze.JetonOdenenSaat > 0)
                    await _jetonService.IcazeJetonuGeriQaytarAsync(icaze.IsciId, icaze.JetonOdenenSaat);

                icaze.Status        = IcazeStatus.ImtinaEdildi;   // ayrıca "ləğv" statusu yoxdur
                icaze.ImtinaSebebi  = $"Ləğv (Rəhbər/HR): {sebeb.Trim()}";
                icaze.Silinib       = true;
                icaze.SilinmeTarixi = DateTime.Now;
                await _unitOfWork.Repository<Icaze>().YenileAsync(icaze);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok("İcazə ləğv edildi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Ləğv xətası: {ex.Message}");
            }
        }

        public async Task<Result<IcazeDetailDto>> GetDetayAsync(int icazeId)
        {
            try
            {
                // Cihaz çıxış/qayıdışını bərpa et ki, faktiki saat dəqiq olsun.
                await IcazeCihazVaxtBerpaEtAsync();

                var icaze = await _unitOfWork.Repository<Icaze>()
                    .GetirAsync(
                        x => x.Id == icazeId,
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Departament)
                            .Include(i => i.EvezEdenIsci)
                            .Include(i => i.CixisGiris),
                        izlemeden: true);

                if (icaze == null)
                    return Result<IcazeDetailDto>.Fail("Tapilmadi.");

                var dto = new IcazeDetailDto
                {
                    Id = icaze.Id,
                    IsciId = icaze.IsciId,
                    IsciAdSoyad = icaze.Isci?.TamAd ?? "-",
                    SobeAdi = icaze.Isci?.IsciTeyinatlari?
                        .Where(t => t.Aktivdir && t.Departament != null)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault() ?? "-",
                    EvezEdenAdSoyad = icaze.EvezEdenIsci?.TamAd ?? "—",
                    IcazeTarixi = icaze.IcazeTarixi,
                    BaslamaSaati = icaze.BaslamaSaati,
                    BitisSaati = icaze.BitisSaati,
                    IcazeSaati = icaze.IcazeSaati,
                    Sebeb = icaze.Sebeb,
                    Status = icaze.Status,
                    ImtinaSebebi = icaze.ImtinaSebebi,
                    Birdefelik = icaze.Birdefelik,
                    JetonOdenenSaat = icaze.JetonOdenenSaat,
                    NaharNezereAlinmasin = icaze.NaharNezereAlinmasin,
                    SobeReisiTesdiq = icaze.SobeReisiTesdiq,
                    SobeReisiTesdiqTarixi = icaze.SobeReisiTesdiqTarixi,
                    RehberTesdiq = icaze.RehberTesdiq,
                    RehberTesdiqTarixi = icaze.RehberTesdiqTarixi,
                    HrTesdiq = icaze.HrTesdiq,
                    HrTesdiqTarixi = icaze.HrTesdiqTarixi,
                    CixisVaxt = icaze.CixisGiris?.CixisVaxt,
                    QayidisVaxt = icaze.CixisGiris?.QayidisVaxt,
                    CixisGirisStatus = icaze.CixisGiris?.Status,
                    FaktikiSaat = IcazeFaktikiSaat(icaze.CixisGiris?.CixisVaxt, icaze.CixisGiris?.QayidisVaxt,
                                                   icaze.Birdefelik, icaze.IcazeTarixi, icaze.BitisSaati),
                };

                return Result<IcazeDetailDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<IcazeDetailDto>.Fail($"Xeta: {ex.Message}");
            }
        }

        public async Task<Result<IList<IcazeListDto>>> GetAllAsync()
        {
            try
            {
                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                            .Include(i => i.EvezEdenIsci)
                            .Include(i => i.CixisGiris),
                        izlemeden: true);

                return Result<IList<IcazeListDto>>.Ok(
                    list.OrderByDescending(x => x.IcazeTarixi).Select(MapToListDto).ToList());
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Icazeler getirilmedi: {ex.Message}");
            }
        }

        // Şöbə rəisi artıq təsdiq zəncirindən çıxarılıb — bu metod köhnə qeydlər üçün saxlanılır
        public async Task<Result<IList<IcazeListDto>>> GetGozlemededeAsync()
        {
            try
            {
                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        predicate: x => x.Status == IcazeStatus.SobeReisiTesdiqinde,
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Departament)
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Vezife)
                            .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                return Result<IList<IcazeListDto>>.Ok(
                    list.OrderByDescending(x => x.IcazeTarixi).Select(MapToListDto).ToList());
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<IList<IcazeListDto>>> GetSobeyeGoreIcazelerAsync(int departamentId, int sobeReisiIsciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        predicate: x => x.Status == IcazeStatus.SobeReisiTesdiqinde
                                     && x.IsciId != sobeReisiIsciId
                                     && x.Isci.IsciTeyinatlari
                                            .Any(t => t.Aktivdir && t.DepartamentId == departamentId),
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Departament)
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Vezife)
                            .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                return Result<IList<IcazeListDto>>.Ok(
                    list.OrderByDescending(x => x.IcazeTarixi).Select(MapToListDto).ToList());
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<IList<IcazeListDto>>> GetRehberTesdiqindeAsync()
        {
            try
            {
                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        predicate: x => x.Status == IcazeStatus.RehberTesdiqinde,
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Departament)
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Vezife)
                            .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                return Result<IList<IcazeListDto>>.Ok(
                    list.OrderByDescending(x => x.IcazeTarixi).Select(MapToListDto).ToList());
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<IList<IcazeListDto>>> GetHrTesdiqindeAsync()
        {
            try
            {
                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        predicate: x => x.Status == IcazeStatus.HrTesdiqinde,
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Departament)
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Vezife)
                            .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                return Result<IList<IcazeListDto>>.Ok(
                    list.OrderByDescending(x => x.IcazeTarixi).Select(MapToListDto).ToList());
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        // Şöbə rəisi — köhnə qeydlər üçün saxlanılır (yeni axında çağırılmır)
        public async Task<Result> SobeReisiTesdiqAsync(int id, bool status, string? qeyd, int sobeReisiId = 0)
        {
            var icaze = await _unitOfWork.Repository<Icaze>().GetirAsync(x => x.Id == id);
            if (icaze == null) return Result.Fail("İcazə tapılmadı.");

            if (icaze.Status != IcazeStatus.SobeReisiTesdiqinde)
                return Result.Fail($"Bu müraciət artıq emal edilib (status: {icaze.Status}).");

            icaze.SobeReisiTesdiq = status;
            icaze.SobeReisiId = sobeReisiId > 0 ? sobeReisiId : icaze.SobeReisiId;
            icaze.SobeReisiTesdiqTarixi = DateTime.Now;
            icaze.Status = status ? IcazeStatus.RehberTesdiqinde : IcazeStatus.ImtinaEdildi;
            if (!status) icaze.ImtinaSebebi = qeyd;

            await _unitOfWork.Repository<Icaze>().YenileAsync(icaze);
            await _unitOfWork.YaddaSaxlaAsync();

            if (status)
            {
                await NotifyIsciProgressAsync(icaze, "Şöbə rəisi", true, qeyd);
                await NotifyAllRehberAsync(icaze);
            }
            else
            {
                await NotifyIsciProgressAsync(icaze, "Şöbə rəisi", false, qeyd);
            }
            return Result.Ok("Şöbə rəisi qərarı qeydə alındı.");
        }

        // Rəhbər təsdiq edir → Tesdiqlenib + IcazeCixisGiris yaranır
        public async Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd, int rehberId = 0, decimal jetonOdenenSaat = 0, bool? naharNezereAlinmasin = null, bool birdefelik = false)
        {
            var icaze = await _unitOfWork.Repository<Icaze>().GetirAsync(x => x.Id == id);
            if (icaze == null) return Result.Fail("İcazə tapılmadı.");

            if (icaze.Status != IcazeStatus.RehberTesdiqinde)
                return Result.Fail($"Bu müraciət artıq emal edilib (status: {icaze.Status}).");

            // Nahar parametrləri IsParametri-dən (default 45 dəq)
            var isParam = await _unitOfWork.Repository<IsParametri>()
                .Query().Where(x => !x.Silinib).FirstOrDefaultAsync();
            var naharDeq = isParam?.NaharMuddetDeqiqe ?? 45;

            // Forma nahar seçimini göndərməyibsə (null) — işçinin müraciətdəki seçimi SAXLANILIR.
            // Əvvəl parametr `bool = false` idi: təsdiq səhifəsində checkbox render olunmayanda
            // (nahara toxunmayan pəncərələrdə) işçinin seçimi səssizcə silinirdi.
            var naharSecimi = naharNezereAlinmasin ?? icaze.NaharNezereAlinmasin;

            var icazeSaatiRaw = (decimal)icaze.IcazeSaati;
            // Nahar çıxılması SABİT fasilə müddətidir (bax: NaharCixilmaSaat şərhi).
            var naharCix = naharSecimi
                ? (decimal)NaharCixilmaSaat(icaze.BaslamaSaati, icaze.BitisSaati, naharDeq)
                : 0m;
            var efektivSaat = Math.Max(0m, icazeSaatiRaw - naharCix);

            // Jeton miqdarı yalnız təsdiq vəziyyətində nəzərə alınır
            if (status && jetonOdenenSaat > 0)
            {
                if (jetonOdenenSaat > efektivSaat)
                    return Result.Fail(
                        $"Jeton ödənişi ({jetonOdenenSaat:0.##} saat) effektiv icazə saatından " +
                        $"({efektivSaat:0.##} saat) çox ola bilməz.");

                var balans = await _jetonService.AktivSaatBalansiAsync(icaze.IsciId);
                if (jetonOdenenSaat > balans)
                    return Result.Fail(
                        $"İşçinin aktiv jeton balansı kifayət etmir " +
                        $"({balans:0.##} saat mövcud, {jetonOdenenSaat:0.##} saat tələb olundu).");
            }

            icaze.RehberTesdiq = status;
            icaze.RehberId = rehberId > 0 ? rehberId : icaze.RehberId;
            icaze.RehberTesdiqTarixi = DateTime.Now;
            icaze.JetonOdenenSaat = status ? jetonOdenenSaat : 0;
            icaze.NaharNezereAlinmasin = status && naharSecimi;

            if (!status)
            {
                icaze.Status = IcazeStatus.ImtinaEdildi;
                icaze.ImtinaSebebi = qeyd;
                await _unitOfWork.Repository<Icaze>().YenileAsync(icaze);
                await _unitOfWork.YaddaSaxlaAsync();
                await NotifyIsciProgressAsync(icaze, "Rəhbər", false, qeyd);
                return Result.Ok("Rəhbər qərarı qeydə alındı.");
            }

            // Rəhbər SON təsdiqdir — HR addımı çıxarıldı (təsdiq rəhbərlə yekunlaşır).
            icaze.Status = IcazeStatus.Tesdiqlenib;
            icaze.Birdefelik = birdefelik;   // rəhbər birdəfəlik (qayıtmayacaq) işarələyə bilər

            await _unitOfWork.Repository<Icaze>().YenileAsync(icaze);
            await _unitOfWork.YaddaSaxlaAsync();

            await NotifyIsciProgressAsync(icaze, "Rəhbər", true, qeyd);

            // Finalization — rəhbər son təsdiq olduğu üçün həmişə icra olunur:
            // jeton FIFO xərclənir, çıxış/giriş qeydi yaranır, gecikmə yenilənir,
            // şöbə rəisi və HR məlumatlandırılır.
            var jetonRes = await _ConsumeJetonsForIcazeAsync(icaze);
            if (!jetonRes.Success)
                return Result.Fail(jetonRes.Message ?? "Jeton xərclənmə xətası.");

            await _YaratCixisGirisAsync(icaze.Id, icaze.Birdefelik);
            await _GecikmeYenileAsync(icaze);
            await NotifySobeReisiAsync(icaze);
            await NotifyHrMalumatAsync(icaze);

            return Result.Ok("Rəhbər qərarı qeydə alındı.");
        }

        // HR təsdiq edir — birdefelik seçimi ilə
        public async Task<Result> HrTesdiqAsync(int id, bool status, string? qeyd, int hrId = 0, bool birdefelik = false)
        {
            var icaze = await _unitOfWork.Repository<Icaze>().GetirAsync(x => x.Id == id);
            if (icaze == null) return Result.Fail("İcazə tapılmadı.");

            if (icaze.Status != IcazeStatus.HrTesdiqinde)
                return Result.Fail($"Bu müraciət artıq emal edilib (status: {icaze.Status}).");

            icaze.HrTesdiq = status;
            icaze.HrId = hrId > 0 ? hrId : icaze.HrId;
            icaze.HrTesdiqTarixi = DateTime.Now;
            icaze.Status = status ? IcazeStatus.Tesdiqlenib : IcazeStatus.ImtinaEdildi;
            if (!status) icaze.ImtinaSebebi = qeyd;
            if (status) icaze.Birdefelik = birdefelik;

            await _unitOfWork.Repository<Icaze>().YenileAsync(icaze);
            await _unitOfWork.YaddaSaxlaAsync();

            await NotifyIsciProgressAsync(icaze, "HR", status, qeyd);

            if (status)
            {
                // Jeton ödənişi rəhbər mərhələsində qeyd olunmuşsa, indi xərclənir
                var jetonRes = await _ConsumeJetonsForIcazeAsync(icaze);
                if (!jetonRes.Success)
                    return Result.Fail(jetonRes.Message ?? "Jeton xərclənmə xətası.");

                await _YaratCixisGirisAsync(icaze.Id, birdefelik);
                await _GecikmeYenileAsync(icaze);
                await NotifySobeReisiAsync(icaze);
            }

            return Result.Ok("HR qərarı qeydə alındı.");
        }

        // İcazə tam təsdiq olduqdan sonra rəhbərin qeyd etdiyi miqdarı
        // FIFO ilə işçinin aktiv jetonlarından xərcləyir. JetonOdenenSaat
        // 0-dırsa heç nə edilmir.
        private async Task<Result> _ConsumeJetonsForIcazeAsync(Icaze icaze)
        {
            if (icaze.JetonOdenenSaat <= 0) return Result.Ok();

            var res = await _jetonService.IcazeUcunFifoJetonXercleAsync(
                icaze.IsciId, icaze.JetonOdenenSaat, icaze.Id);

            if (!res.Success) return Result.Fail(res.Message ?? "Jeton xərclənmədi.");

            // Görünən qeyd: əvəzləşdirmə Xərcləmə Tarixçəsində adi redim kimi görünsün
            await _jetonService.JetonEvezlesdirmeQeydiAsync(
                icaze.IsciId, icaze.JetonOdenenSaat, icaze.IcazeTarixi, icaze.BaslamaSaati, icaze.BitisSaati,
                "Adi icazəyə jeton əvəzləşdirməsi (rəhbər təsdiqində tutuldu)");

            return Result.Ok();
        }

        // ════════════════════════════════════════════════════════
        // IcazeCixisGiris köməkçisi
        // ════════════════════════════════════════════════════════

        private async Task _YaratCixisGirisAsync(int icazeId, bool birdefelik)
        {
            var movcud = await _unitOfWork.Repository<IcazeCixisGiris>()
                .GetirAsync(x => x.IcazeId == icazeId);
            if (movcud != null) return; // artıq var

            var cixis = new IcazeCixisGiris
            {
                IcazeId = icazeId,
                Birdefelik = birdefelik,
                Status = IcazeCixisGirisStatus.Gozlenir
            };
            await _unitOfWork.Repository<IcazeCixisGiris>().YaratAsync(cixis);
            await _unitOfWork.YaddaSaxlaAsync();
        }

        // ════════════════════════════════════════════════════════
        // Bildiriş köməkçiləri
        // ════════════════════════════════════════════════════════

        private async Task NotifyApproversForCreateAsync(Icaze ic)
        {
            var isciAd = await GetIsciAdAsync(ic.IsciId);
            var dovr = $"{ic.IcazeTarixi:dd.MM.yyyy} {ic.BaslamaSaati:hh\\:mm}–{ic.BitisSaati:hh\\:mm}";
            var bashliq = "Yeni icazə müraciəti";
            var metn = $"{isciAd} ({dovr}) icazə müraciəti göndərdi.";

            switch (ic.Status)
            {
                case IcazeStatus.RehberTesdiqinde:
                    // Rəhbər + Admin Identity rolları ilə bildiriş (main konvensiyası —
                    // MezuniyyetService və s. ilə eyni). Struktur rolu cədvəlinə güvənmir.
                    await _bildirisRouter.NotifyRolesAsync(
                        new[] { RoleNames.Rehber, RoleNames.Admin },
                        BildirisNovu.IcazeMuraciet, bashliq, metn,
                        redirectUrl: $"/User/Tesdiq/IcazeDetal/{ic.Id}?rol=Rehber",
                        icazeId: ic.Id, exceptIsciId: ic.IsciId);
                    break;

                case IcazeStatus.HrTesdiqinde:
                    var hrUrl = $"/User/Tesdiq/IcazeDetal/{ic.Id}?rol=Hr";
                    await _bildirisRouter.NotifyRolesAsync(
                        new[] { RoleNames.HR, RoleNames.Admin },
                        BildirisNovu.IcazeMuraciet, bashliq, metn,
                        redirectUrl: hrUrl, icazeId: ic.Id, exceptIsciId: ic.IsciId);
                    break;
            }
        }

        // Rəhbər/HR tesdiqinden sonra Şöbə Rəisinə məlumat bildirişi
        private async Task NotifySobeReisiAsync(Icaze ic)
        {
            var teyinat = await _unitOfWork.Repository<IsciTeyinat>()
                .GetirAsync(x => x.IsciId == ic.IsciId && x.Aktivdir && !x.Silinib);
            if (teyinat == null) return;

            var isciAd = await GetIsciAdAsync(ic.IsciId);
            var dovr = $"{ic.IcazeTarixi:dd.MM.yyyy} {ic.BaslamaSaati:hh\\:mm}–{ic.BitisSaati:hh\\:mm}";
            await _bildirisRouter.NotifyDepartmentRoleAsync(
                teyinat.DepartamentId,
                StrukturRolTipi.SobeReisi,
                BildirisNovu.IcazeTesdiq,
                "İcazə təsdiqləndi — məlumat",
                $"{isciAd} ({dovr}) icazəsi təsdiqləndi.",
                redirectUrl: $"/User/Icaze/Dovriyye",
                icazeId: ic.Id, exceptIsciId: ic.IsciId);
        }

        // İcazə təsdiqləndikdə həmin günkü Gecikme davamiyyət qeydini İcazəli et
        private async Task _GecikmeYenileAsync(Icaze ic)
        {
            var dav = await _unitOfWork.Repository<Davamiyyet>()
                .GetirAsync(x => x.IsciId == ic.IsciId && x.Tarix.Date == ic.IcazeTarixi.Date);

            if (dav == null || dav.Status != DavamiyyetStatus.Gecikme || !dav.GirisVaxti.HasValue)
                return;

            var giris = dav.GirisVaxti.Value.TimeOfDay;
            if (giris >= ic.BaslamaSaati && giris <= ic.BitisSaati)
            {
                dav.Status = DavamiyyetStatus.Icazeli;
                await _unitOfWork.Repository<Davamiyyet>().YenileAsync(dav);
                await _unitOfWork.YaddaSaxlaAsync();
            }
        }

        // Rəhbər tesdiqinden sonra HR-a məlumat bildirişi (HR fallback deyilsə)
        private async Task NotifyHrMalumatAsync(Icaze ic)
        {
            var isciAd = await GetIsciAdAsync(ic.IsciId);
            var dovr = $"{ic.IcazeTarixi:dd.MM.yyyy} {ic.BaslamaSaati:hh\\:mm}–{ic.BitisSaati:hh\\:mm}";
            await _bildirisRouter.NotifyRolesAsync(
                new[] { RoleNames.HR, RoleNames.Admin },
                BildirisNovu.IcazeTesdiq,
                "İcazə təsdiqləndi — məlumat",
                $"{isciAd} ({dovr}) icazəsi rəhbər tərəfindən təsdiqləndi.",
                redirectUrl: $"/User/Icaze/Dovriyye",
                icazeId: ic.Id, exceptIsciId: ic.IsciId);
        }

        private async Task NotifyAllRehberAsync(Icaze ic)
        {
            var isciAd = await GetIsciAdAsync(ic.IsciId);
            var dovr = $"{ic.IcazeTarixi:dd.MM.yyyy} {ic.BaslamaSaati:hh\\:mm}–{ic.BitisSaati:hh\\:mm}";
            await _bildirisRouter.NotifyRolesAsync(
                new[] { RoleNames.Rehber, RoleNames.Admin },
                BildirisNovu.IcazeMuraciet,
                "İcazə müraciəti — Rəhbər təsdiqi gözləyir",
                $"{isciAd} ({dovr}) icazəsi şöbə rəisi tərəfindən təsdiqlənib.",
                redirectUrl: $"/User/Tesdiq/IcazeDetal/{ic.Id}?rol=Rehber",
                icazeId: ic.Id, exceptIsciId: ic.IsciId);
        }

        private async Task NotifyAllHrAsync(Icaze ic)
        {
            var isciAd = await GetIsciAdAsync(ic.IsciId);
            var dovr = $"{ic.IcazeTarixi:dd.MM.yyyy} {ic.BaslamaSaati:hh\\:mm}–{ic.BitisSaati:hh\\:mm}";
            var bashliq = "İcazə müraciəti — HR təsdiqi gözləyir";
            var metn = $"{isciAd} ({dovr}) icazəsi rəhbər tərəfindən təsdiqlənib.";
            var url = $"/User/Tesdiq/IcazeDetal/{ic.Id}?rol=Hr";
            await _bildirisRouter.NotifyRolesAsync(
                new[] { RoleNames.HR, RoleNames.Admin },
                BildirisNovu.IcazeMuraciet, bashliq, metn,
                redirectUrl: url, icazeId: ic.Id, exceptIsciId: ic.IsciId);
        }

        private async Task NotifyIsciProgressAsync(Icaze ic, string mərhələ, bool tesdiq, string? qeyd)
        {
            var dovr = $"{ic.IcazeTarixi:dd.MM.yyyy} {ic.BaslamaSaati:hh\\:mm}–{ic.BitisSaati:hh\\:mm}";
            var redirectUrl = $"/User/Icaze/Detail/{ic.Id}";
            if (tesdiq)
            {
                await _bildirisRouter.NotifyIsciAsync(
                    ic.IsciId, BildirisNovu.IcazeTesdiq,
                    $"İcazə — {mərhələ} təsdiqi alındı",
                    $"{dovr} icazə müraciətiniz {mərhələ} tərəfindən təsdiqləndi.",
                    redirectUrl: redirectUrl, icazeId: ic.Id);
            }
            else
            {
                var sebeb = string.IsNullOrWhiteSpace(qeyd) ? "" : $" Səbəb: {qeyd}";
                await _bildirisRouter.NotifyIsciAsync(
                    ic.IsciId, BildirisNovu.IcazeImtina,
                    $"İcazə — {mərhələ} imtinası",
                    $"{dovr} icazə müraciətiniz {mərhələ} tərəfindən rədd edildi.{sebeb}",
                    redirectUrl: redirectUrl, icazeId: ic.Id);
            }
        }

        private async Task<string> GetIsciAdAsync(int isciId)
        {
            var isci = await _unitOfWork.Repository<Isci>()
                .GetirAsync(x => x.Id == isciId, izlemeden: true);
            return isci?.TamAd ?? $"İşçi #{isciId}";
        }

        private static IcazeListDto MapToListDto(Icaze icaze) => new()
        {
            Id = icaze.Id,
            IsciAdSoyad = icaze.Isci.TamAd,
            SobeAdi = icaze.Isci.IsciTeyinatlari
                .Where(t => t.Aktivdir)
                .Select(t => t.Departament?.Ad)
                .FirstOrDefault() ?? "-",
            EvezEdenAdSoyad = icaze.EvezEdenIsci?.TamAd ?? "—",
            IcazeTarixi = icaze.IcazeTarixi,
            BaslamaSaati = icaze.BaslamaSaati,
            BitisSaati = icaze.BitisSaati,
            IcazeSaati = icaze.IcazeSaati,
            NaharNezereAlinmasin = icaze.NaharNezereAlinmasin,
            JetonOdenenSaat = icaze.JetonOdenenSaat,
            Sebeb = icaze.Sebeb,
            Status = icaze.Status,
            Birdefelik = icaze.Birdefelik,
            SobeReisiTesdiq = icaze.SobeReisiTesdiq,
            SobeReisiTesdiqTarixi = icaze.SobeReisiTesdiqTarixi,
            RehberTesdiq = icaze.RehberTesdiq,
            RehberTesdiqTarixi = icaze.RehberTesdiqTarixi,
            HrTesdiq = icaze.HrTesdiq,
            HrTesdiqTarixi = icaze.HrTesdiqTarixi,
            CixisVaxt = icaze.CixisGiris?.CixisVaxt,
            QayidisVaxt = icaze.CixisGiris?.QayidisVaxt,
            CixisGirisStatus = icaze.CixisGiris?.Status,
            FaktikiSaat = IcazeFaktikiSaat(icaze.CixisGiris?.CixisVaxt, icaze.CixisGiris?.QayidisVaxt,
                                           icaze.Birdefelik, icaze.IcazeTarixi, icaze.BitisSaati),
        };

        // ── Cihaz oxuma bərpası üçün batch xəritələr ─────────
        // Xam cihaz oxumaları (varsa — dəqiq, qayıdışı da verir) + Davamiyyət son
        // çıxışı (xam yoxdursa fallback) bir dəfəyə yüklənir (N+1 olmasın).
        private async Task<(Dictionary<string, List<DateTime>> raw, Dictionary<string, DateTime> davCixis)>
            CihazBerpaXeritesiAsync(List<int> isciIdler, DateTime minT, DateTime maxT)
        {
            var raw = new Dictionary<string, List<DateTime>>();
            var davCixis = new Dictionary<string, DateTime>();
            if (isciIdler.Count == 0) return (raw, davCixis);

            var oxumalar = await _unitOfWork.Repository<CihazOxuma>()
                .Query().AsNoTracking()
                .Where(o => !o.Silinib && isciIdler.Contains(o.IsciId)
                         && o.Vaxt.Date >= minT && o.Vaxt.Date <= maxT)
                .Select(o => new { o.IsciId, o.Vaxt })
                .ToListAsync();
            raw = oxumalar
                .GroupBy(o => $"{o.IsciId}|{o.Vaxt:yyyy-MM-dd}")
                .ToDictionary(g => g.Key, g => g.Select(x => x.Vaxt).OrderBy(v => v).ToList());

            var davlar = await _unitOfWork.Repository<Davamiyyet>()
                .Query().AsNoTracking()
                .Where(d => !d.Silinib && isciIdler.Contains(d.IsciId)
                         && d.Tarix.Date >= minT && d.Tarix.Date <= maxT
                         && d.CixisVaxti != null)
                .Select(d => new { d.IsciId, Tarix = d.Tarix.Date, d.CixisVaxti })
                .ToListAsync();
            davCixis = davlar
                .GroupBy(d => $"{d.IsciId}|{d.Tarix:yyyy-MM-dd}")
                .ToDictionary(g => g.Key, g => g.Max(d => d.CixisVaxti!.Value));

            return (raw, davCixis);
        }

        // İcazə pəncərəsi üçün faktiki çıxış/qayıdışı bərpa edir:
        //  1) Xam oxumalar varsa — pəncərə içindəki İLK = çıxış, SON = qayıdış
        //     (işçi çıxıb-qayıdıbsa da düzgün tutulur).
        //  2) Yoxdursa — Davamiyyət son çıxışı (yalnız çıxış, pəncərə içindədirsə).
        private static (DateTime? cixis, DateTime? qayidis) IcazeFaktikiBerpa(
            int isciId, DateTime tarix, TimeSpan baslama, TimeSpan bitis,
            Dictionary<string, List<DateTime>> raw, Dictionary<string, DateTime> davCixis)
        {
            var key = $"{isciId}|{tarix:yyyy-MM-dd}";
            var alt = baslama - TimeSpan.FromMinutes(15);
            var ust = bitis + TimeSpan.FromMinutes(15);

            if (raw.TryGetValue(key, out var punchlar))
            {
                var pencere = punchlar.Where(v => v.TimeOfDay >= alt && v.TimeOfDay <= ust).ToList();
                if (pencere.Count > 0)
                {
                    var cx = pencere[0];
                    DateTime? qy = (pencere.Count > 1 && pencere[^1] > cx.AddMinutes(5))
                        ? pencere[^1] : (DateTime?)null;
                    return (cx, qy);
                }
            }

            if (davCixis.TryGetValue(key, out var dcx) &&
                dcx.TimeOfDay >= alt && dcx.TimeOfDay <= ust)
                return (dcx, null);

            return (null, null);
        }

        // ── Cihaz çıxış/qayıdışını xam punch-lardan bərpa edib SAXLAYIR ──────────
        // Boşluq idi: əvvəlki backfill yalnız "heç nə yazılmayıb" (CixisVaxt null, Gözlənir)
        // halını EKRANA göstərirdi; "çıxış var, qayıdış yox" (Status=Cixdi) halını buraxırdı
        // və heç vaxt saxlamırdı. Nəticədə canlı yol qayıdışı buraxanda (punch CihazOxuma-da
        // olsa belə) qayıdış əbədi boş qalırdı. Bu metod hər iki halı tutub bazaya yazır.
        private async Task IcazeCihazVaxtBerpaEtAsync()
        {
            var bugun = DateTime.Now.Date;
            var eksikler = await _unitOfWork.Repository<IcazeCixisGiris>()
                .Query()
                .Include(cg => cg.Icaze)
                .Where(cg => !cg.Silinib
                          && !cg.Birdefelik
                          && cg.QayidisVaxt == null
                          && (cg.Status == IcazeCixisGirisStatus.Gozlenir
                              || cg.Status == IcazeCixisGirisStatus.Cixdi)
                          && cg.Icaze.Status == IcazeStatus.Tesdiqlenib
                          && cg.Icaze.IcazeTarixi.Date <= bugun
                          && cg.Icaze.IcazeTarixi.Date >= bugun.AddDays(-92))
                .ToListAsync();
            if (eksikler.Count == 0) return;

            var isciIdler = eksikler.Select(cg => cg.Icaze.IsciId).Distinct().ToList();
            var minT = eksikler.Min(cg => cg.Icaze.IcazeTarixi.Date);
            var maxT = eksikler.Max(cg => cg.Icaze.IcazeTarixi.Date);
            var (raw, davCixis) = await CihazBerpaXeritesiAsync(isciIdler, minT, maxT);

            bool deyisdi = false;
            foreach (var cg in eksikler)
            {
                var (fcx, fqy) = IcazeFaktikiBerpa(cg.Icaze.IsciId, cg.Icaze.IcazeTarixi,
                    cg.Icaze.BaslamaSaati, cg.Icaze.BitisSaati, raw, davCixis);

                bool dey = false;
                if (cg.CixisVaxt == null && fcx != null)
                {
                    cg.CixisVaxt = fcx;
                    if (cg.Status == IcazeCixisGirisStatus.Gozlenir)
                        cg.Status = IcazeCixisGirisStatus.Cixdi;
                    dey = true;
                }
                if (cg.QayidisVaxt == null && fqy != null)
                {
                    cg.QayidisVaxt = fqy;                              // ← əsas düzəliş: qayıdış saxlanılır
                    cg.Status = IcazeCixisGirisStatus.Tamamlandi;
                    dey = true;
                }
                if (dey)
                {
                    cg.YenilenmeTarixi = DateTime.Now;
                    await _unitOfWork.Repository<IcazeCixisGiris>().YenileAsync(cg);
                    deyisdi = true;
                }
            }
            if (deyisdi) await _unitOfWork.YaddaSaxlaAsync();
        }

        // Bir icazənin FAKTİKİ saatı (Dövriyyə və İzləmə eyni məntiqlə işləsin deyə tək yerdə):
        //  • adi icazə  → qayıdış − çıxış (hər iki vaxt olmalıdır, yoxdursa null)
        //  • birdəfəlik VƏ YA icazə günün sonuna kimidir (qayıdış yoxdur) → işçi qayıtmır,
        //    ona görə çıxışdan planlaşdırılan icazə sonuna (BitisSaati) qədər sayılır.
        //    Çıxış yoxdursa, yaxud çıxış icazə sonundan sonradırsa null (faktiki ölçülə bilmir).
        // gunSonu = StandartCixisVaxti (verilsə, günün sonuna kimi olan icazə də qayıtmayan sayılır).
        internal static double? IcazeFaktikiSaat(
            DateTime? cixis, DateTime? qayidis, bool birdefelik,
            DateTime icazeTarixi, TimeSpan bitisSaati, TimeSpan? gunSonu = null)
        {
            if (cixis == null) return null;

            // İşçi qayıtmır: birdəfəlik, yaxud icazə günün sonuna kimidir və qayıdış qeydi yoxdur.
            bool qayitmir = birdefelik
                || (qayidis == null && gunSonu.HasValue && bitisSaati >= gunSonu.Value);

            if (qayitmir)
            {
                var icazeSonu = icazeTarixi.Date + bitisSaati;
                var saat = (icazeSonu - cixis.Value).TotalHours;
                return saat > 0 ? Math.Round(saat, 2) : (double?)null;
            }

            if (qayidis == null || qayidis.Value <= cixis.Value) return null;
            return Math.Round((qayidis.Value - cixis.Value).TotalHours, 2);
        }

        // "Nahara çıxmıram" halında icazədən çıxılan müddət (saat) — SABİT nahar fasiləsi.
        //
        // Qayda (10.06.2026): işçi nahar fasiləsini götürmür → 45 dəq kredit qazanır →
        // icazə pəncərəsi 45 dəq uzun ola bilər (max 3s45d), ƏVƏZİNDƏ sayğaca 45 dəq az
        // düşür. Güzəşt (+45) və çıxılma (−45) bir-birini tarazlayır: sayılan icazə heç
        // vaxt 3 saatı keçmir.
        //
        // DİQQƏT: burada REAL KƏSİŞMƏ (icazə ∩ nahar pəncərəsi) İSTİFADƏ EDİLMİR.
        // 24.07.2026-da kəsişməyə keçirilmişdi (ec0a695e) — nəticədə nahara toxunmayan
        // pəncərədə (məs. 14:00–17:45) çıxılma 0 olurdu: işçi naharda işləyir, amma
        // sayğacdan 3s45d tam yazılırdı, yəni güzəştin qarşılığı itirdi. Bərpa edildi.
        // Yeganə hədd: pəncərənin özündən çox çıxılmasın.
        internal static double NaharCixilmaSaat(TimeSpan bas, TimeSpan bitis, int naharDeq)
        {
            var pencere = (bitis - bas).TotalHours;
            if (pencere <= 0) return 0;
            var cixilan = naharDeq / 60.0;
            return cixilan < pencere ? cixilan : pencere;
        }

        public async Task<Result<IList<IcazeIsciIstatistikDto>>> GetIsciIzlemeAsync(IcazeIzlemeFiltrDto filtr)
        {
            try
            {
                await IcazeCihazVaxtBerpaEtAsync();   // cihaz çıxış/qayıdışını xam punch-lardan bərpa edib saxla

                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        predicate: x =>
                            (filtr.TarixFrom == null || x.IcazeTarixi >= filtr.TarixFrom) &&
                            (filtr.TarixTo == null || x.IcazeTarixi <= filtr.TarixTo) &&
                            (filtr.Status == null || (int)x.Status == filtr.Status) &&
                            (filtr.DepartamentId == null || x.Isci.IsciTeyinatlari
                                .Any(t => t.Aktivdir && t.DepartamentId == filtr.DepartamentId)) &&
                            (filtr.Axtaris == null || x.Isci.Ad.Contains(filtr.Axtaris) ||
                                x.Isci.Soyad.Contains(filtr.Axtaris)),
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Departament)
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Vezife)
                            .Include(i => i.EvezEdenIsci)
                            .Include(i => i.CixisGiris),
                        izlemeden: true);

                // Nahar parametrləri — effektiv saat DTO-da sabit fasilə ilə hesablanır.
                var izParam = await _unitOfWork.Repository<IsParametri>()
                    .Query().AsNoTracking().Where(x => !x.Silinib).FirstOrDefaultAsync();
                var izNaharDeq = izParam?.NaharMuddetDeqiqe ?? 45;

                // Planlaşdırılan effektiv = DTO.EffektivSaat (nahar çıxılıb) − jeton (bonus, sayılmır).
                double PlanEfektiv(IcazeListDto d) => Math.Max(0, d.EffektivSaat - (double)d.JetonOdenenSaat);

                // ── Bağlanmamış (Gözlənir) icazədə faktiki çıxış/qayıdışı cihaz datasından bərpa et ──
                // Dövriyyə ilə eyni məntiq: xam oxuma (varsa — qayıdış da), yoxsa Davamiyyət çıxışı.
                var izEksikler = list
                    .Where(ic => ic.CixisGiris != null
                              && ic.CixisGiris.CixisVaxt == null
                              && ic.CixisGiris.Status == IcazeCixisGirisStatus.Gozlenir)
                    .ToList();

                var (rawXerite, davXerite) = izEksikler.Count > 0
                    ? await CihazBerpaXeritesiAsync(
                        izEksikler.Select(ic => ic.IsciId).Distinct().ToList(),
                        izEksikler.Min(ic => ic.IcazeTarixi.Date),
                        izEksikler.Max(ic => ic.IcazeTarixi.Date))
                    : (new Dictionary<string, List<DateTime>>(), new Dictionary<string, DateTime>());

                var grouped = list
                    .GroupBy(x => x.IsciId)
                    .Select(g =>
                    {
                        var ilk = g.First();
                        var aktivTeyinat = ilk.Isci?.IsciTeyinatlari?.FirstOrDefault(t => t.Aktivdir);

                        var icazeDtolar = g.OrderByDescending(x => x.IcazeTarixi).Select(x =>
                        {
                            var dto = MapToListDto(x);
                            dto.NaharDeqiqe = izNaharDeq;
                            if (x.CixisGiris != null && x.CixisGiris.CixisVaxt == null &&
                                x.CixisGiris.Status == IcazeCixisGirisStatus.Gozlenir)
                            {
                                var (fcx, fqy) = IcazeFaktikiBerpa(x.IsciId, x.IcazeTarixi,
                                    x.BaslamaSaati, x.BitisSaati, rawXerite, davXerite);
                                if (fcx != null) dto.CixisVaxt = fcx;
                                if (fqy != null) dto.QayidisVaxt = fqy;
                            }
                            return dto;
                        }).ToList();

                        return new IcazeIsciIstatistikDto
                        {
                            IsciId = g.Key,
                            IsciAdSoyad = ilk.Isci?.TamAd ?? $"İşçi #{g.Key}",
                            SobeAdi = aktivTeyinat?.Departament?.Ad ?? "-",
                            VezifeAdi = aktivTeyinat?.Vezife?.Ad,
                            CemiMuraciet = g.Count(),
                            TesdiqlenibSayi = g.Count(x => x.Status == IcazeStatus.Tesdiqlenib),
                            GozlemeSayi = g.Count(x =>
                                x.Status == IcazeStatus.Gozlemede ||
                                x.Status == IcazeStatus.SobeReisiTesdiqinde ||
                                x.Status == IcazeStatus.RehberTesdiqinde ||
                                x.Status == IcazeStatus.HrTesdiqinde),
                            ImtinaEdildiSayi = g.Count(x => x.Status == IcazeStatus.ImtinaEdildi),
                            UmumSaat = icazeDtolar.Sum(PlanEfektiv),
                            TesdiqSaat = icazeDtolar.Where(d => d.Status == IcazeStatus.Tesdiqlenib).Sum(PlanEfektiv),
                            FaktikiSaat = icazeDtolar
                                .Where(d => d.Status == IcazeStatus.Tesdiqlenib)
                                .Select(d =>
                                {
                                    // Faktiki xam (günün sonuna kimi halı üçün StandartCixisVaxti ilə) − sabit nahar.
                                    var f = IcazeFaktikiSaat(d.CixisVaxt, d.QayidisVaxt, d.Birdefelik,
                                                             d.IcazeTarixi, d.BitisSaati, izParam?.StandartCixisVaxti);
                                    if (f.HasValue && d.NaharNezereAlinmasin && d.CixisVaxt.HasValue)
                                    {
                                        var bit = d.QayidisVaxt?.TimeOfDay ?? d.BitisSaati;
                                        f = Math.Max(0, f.Value - NaharCixilmaSaat(
                                            d.CixisVaxt.Value.TimeOfDay, bit, izNaharDeq));
                                    }
                                    return f;
                                })
                                .Where(s => s.HasValue)
                                .Sum(s => s!.Value),
                            SonIcazeTarixi = g.Max(x => (DateTime?)x.IcazeTarixi),
                            Icazeler = icazeDtolar,
                        };
                    })
                    .OrderBy(x => x.SobeAdi).ThenBy(x => x.IsciAdSoyad)
                    .ToList();

                return Result<IList<IcazeIsciIstatistikDto>>.Ok(grouped);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeIsciIstatistikDto>>.Fail($"İzləmə məlumatları gətirilmədi: {ex.Message}");
            }
        }

        public async Task<Result<IList<IcazeDovriyyeDto>>> GetDovriyyeAsync(
            DateTime? tarixFrom, DateTime? tarixTo, int? departamentId, string? axtaris)
        {
            try
            {
                await IcazeCihazVaxtBerpaEtAsync();   // cihaz çıxış/qayıdışını xam punch-lardan bərpa edib saxla

                // Günün sonuna kimi olan icazələrdə (qayıdış yox) faktikini hesablamaq üçün standart çıxış vaxtı
                var dovParam = await _unitOfWork.Repository<IsParametri>()
                    .Query().AsNoTracking().Where(x => !x.Silinib).FirstOrDefaultAsync();

                var list = await _unitOfWork.Repository<IcazeCixisGiris>()
                    .HamisiniGetirAsync(
                        predicate: x =>
                            !x.Silinib &&
                            x.Status != IcazeCixisGirisStatus.LegvEdildi &&
                            (tarixFrom == null || x.Icaze.IcazeTarixi >= tarixFrom) &&
                            (tarixTo == null || x.Icaze.IcazeTarixi <= tarixTo) &&
                            (departamentId == null || x.Icaze.Isci.IsciTeyinatlari
                                .Any(t => t.Aktivdir && t.DepartamentId == departamentId)) &&
                            (axtaris == null ||
                                x.Icaze.Isci.Ad.Contains(axtaris) ||
                                x.Icaze.Isci.Soyad.Contains(axtaris)),
                        include: q => q
                            .Include(c => c.Icaze)
                                .ThenInclude(i => i.Isci)
                                    .ThenInclude(i => i.IsciTeyinatlari)
                                        .ThenInclude(t => t.Departament),
                        izlemeden: true);

                // ── CixisGiris QEYDİ OLMAYAN təsdiqlənmiş icazələr (say = siyahı) ──
                // Bəzi axınlar (məs. jeton sorğusundan yaranan icazə) bu qeyd olmadan
                // icazə yaradırdı — nəticədə icazə Dövriyyə siyahısına düşmürdü və HR
                // düzəliş edə bilmirdi. Onlar sintetik sətir kimi əlavə olunur; düzəliş
                // POST-u (CixisGirisDuzeltAsync) qeydi özü yaradır.
                var qeydsizIcazeler = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        predicate: x => !x.Silinib &&
                            x.Status == IcazeStatus.Tesdiqlenib &&
                            x.CixisGiris == null &&
                            (tarixFrom == null || x.IcazeTarixi >= tarixFrom) &&
                            (tarixTo == null || x.IcazeTarixi <= tarixTo) &&
                            (departamentId == null || x.Isci.IsciTeyinatlari
                                .Any(t => t.Aktivdir && t.DepartamentId == departamentId)) &&
                            (axtaris == null ||
                                x.Isci.Ad.Contains(axtaris) ||
                                x.Isci.Soyad.Contains(axtaris)),
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Departament),
                        izlemeden: true);

                // ── Bağlanmamış (Gözlənir) icazələrdə faktiki çıxış/qayıdışı cihaz datasından bərpa et ──
                // Canlı ADMS hook işləməyibsə (seed, yaxud oxuma təsdiqdən əvvəl gəlib), xam cihaz
                // oxumalarından (varsa — qayıdış da daxil), yoxdursa Davamiyyət çıxışından bərpa
                // olunur və GÖSTƏRİLİR (DB-yə YAZMIR). Sintetik (qeydsiz) icazələr də bərpaya daxildir.
                var eksikler = list
                    .Where(c => c.CixisVaxt == null &&
                                c.Status == IcazeCixisGirisStatus.Gozlenir)
                    .ToList();

                var berpaAcarlari = eksikler
                    .Select(c => (IsciId: c.Icaze.IsciId, Tarix: c.Icaze.IcazeTarixi.Date))
                    .Concat(qeydsizIcazeler.Select(i => (IsciId: i.IsciId, Tarix: i.IcazeTarixi.Date)))
                    .ToList();

                var (rawXerite, davXerite) = berpaAcarlari.Count > 0
                    ? await CihazBerpaXeritesiAsync(
                        berpaAcarlari.Select(k => k.IsciId).Distinct().ToList(),
                        berpaAcarlari.Min(k => k.Tarix),
                        berpaAcarlari.Max(k => k.Tarix))
                    : (new Dictionary<string, List<DateTime>>(), new Dictionary<string, DateTime>());

                var dtos = list
                    .OrderByDescending(x => x.Icaze.IcazeTarixi)
                    .Select(c =>
                    {
                        var (fcx, fqy) = (c.CixisVaxt == null && c.Status == IcazeCixisGirisStatus.Gozlenir)
                            ? IcazeFaktikiBerpa(c.Icaze.IsciId, c.Icaze.IcazeTarixi,
                                                c.Icaze.BaslamaSaati, c.Icaze.BitisSaati, rawXerite, davXerite)
                            : ((DateTime?)null, (DateTime?)null);
                        var effCixis = c.CixisVaxt ?? fcx;
                        var effQayidis = c.QayidisVaxt ?? fqy;
                        // Birdəfəlikdə qayıdış olmur — faktiki çıxışdan icazə sonuna sayılır.
                        // Adi icazədə saxlanmış (canlı) dəyər varsa ona üstünlük verilir,
                        // yoxsa çıxış/qayıdış cütünə görə hesablanır.
                        double? faktiki = (!c.Birdefelik ? c.FaktikiSaat : null)
                            ?? IcazeFaktikiSaat(effCixis, effQayidis, c.Birdefelik,
                                                c.Icaze.IcazeTarixi, c.Icaze.BitisSaati, dovParam?.StandartCixisVaxti);
                        // "Nahara çıxmıram" halında faktikidən də sabit nahar çıxılır
                        // (planlaşdırılanla eyni məntiq).
                        if (faktiki.HasValue && c.Icaze.NaharNezereAlinmasin && effCixis.HasValue)
                        {
                            var dnDeq = dovParam?.NaharMuddetDeqiqe ?? 45;
                            var dnBit = effQayidis?.TimeOfDay ?? c.Icaze.BitisSaati;
                            faktiki = Math.Max(0, faktiki.Value
                                - NaharCixilmaSaat(effCixis.Value.TimeOfDay, dnBit, dnDeq));
                        }
                        return new IcazeDovriyyeDto
                        {
                            IcazeId = c.IcazeId,
                            IsciAdSoyad = c.Icaze.Isci.TamAd,
                            SobeAdi = c.Icaze.Isci.IsciTeyinatlari
                                .Where(t => t.Aktivdir)
                                .Select(t => t.Departament?.Ad)
                                .FirstOrDefault() ?? "-",
                            IcazeTarixi = c.Icaze.IcazeTarixi,
                            BaslamaSaati = c.Icaze.BaslamaSaati,
                            BitisSaati = c.Icaze.BitisSaati,
                            PlanlananSaat = c.Icaze.IcazeSaati,
                            NaharNezereAlinmasin = c.Icaze.NaharNezereAlinmasin,
                            NaharDeqiqe = dovParam?.NaharMuddetDeqiqe ?? 45,
                            Birdefelik = c.Birdefelik,
                            CixisVaxt = effCixis,
                            QayidisVaxt = effQayidis,
                            FaktikiSaat = faktiki,
                            CixisStatus = fcx != null
                                ? (fqy != null ? IcazeCixisGirisStatus.Tamamlandi : IcazeCixisGirisStatus.Cixdi)
                                : c.Status,
                        };
                    }).ToList();

                // Sintetik sətirlər — qeydi olmayan icazələr (eyni bərpa məntiqi ilə)
                var sintetikler = qeydsizIcazeler.Select(i =>
                {
                    var (fcx, fqy) = IcazeFaktikiBerpa(i.IsciId, i.IcazeTarixi,
                                                      i.BaslamaSaati, i.BitisSaati, rawXerite, davXerite);
                    double? faktiki = IcazeFaktikiSaat(fcx, fqy, false,
                                        i.IcazeTarixi, i.BitisSaati, dovParam?.StandartCixisVaxti);
                    if (faktiki.HasValue && i.NaharNezereAlinmasin && fcx.HasValue)
                    {
                        var dnDeq = dovParam?.NaharMuddetDeqiqe ?? 45;
                        var dnBit = fqy?.TimeOfDay ?? i.BitisSaati;
                        faktiki = Math.Max(0, faktiki.Value
                            - NaharCixilmaSaat(fcx.Value.TimeOfDay, dnBit, dnDeq));
                    }
                    return new IcazeDovriyyeDto
                    {
                        IcazeId = i.Id,
                        IsciAdSoyad = i.Isci.TamAd,
                        SobeAdi = i.Isci.IsciTeyinatlari
                            .Where(t => t.Aktivdir)
                            .Select(t => t.Departament?.Ad)
                            .FirstOrDefault() ?? "-",
                        IcazeTarixi = i.IcazeTarixi,
                        BaslamaSaati = i.BaslamaSaati,
                        BitisSaati = i.BitisSaati,
                        PlanlananSaat = i.IcazeSaati,
                        NaharNezereAlinmasin = i.NaharNezereAlinmasin,
                        NaharDeqiqe = dovParam?.NaharMuddetDeqiqe ?? 45,
                        Birdefelik = false,
                        CixisVaxt = fcx,
                        QayidisVaxt = fqy,
                        FaktikiSaat = faktiki,
                        CixisStatus = fcx != null
                            ? (fqy != null ? IcazeCixisGirisStatus.Tamamlandi : IcazeCixisGirisStatus.Cixdi)
                            : IcazeCixisGirisStatus.Gozlenir,
                    };
                }).ToList();

                if (sintetikler.Count > 0)
                    dtos = dtos.Concat(sintetikler)
                               .OrderByDescending(x => x.IcazeTarixi)
                               .ToList();

                return Result<IList<IcazeDovriyyeDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeDovriyyeDto>>.Fail($"Dövriyyə məlumatları gətirilmədi: {ex.Message}");
            }
        }

        // HR əl ilə düzəliş — cihaz çıxış/qayıdış vaxtlarını yeniləyir (insan faktoru:
        // təsadüfi tanınma, səhv skan və s. hallarda HR qiymətləri düzəldə bilsin).
        public async Task<Result> CixisGirisDuzeltAsync(int icazeId, DateTime? cixisVaxt, DateTime? qayidisVaxt)
        {
            try
            {
                var icaze = await _unitOfWork.Repository<Icaze>()
                    .GetirAsync(x => x.Id == icazeId, include: q => q.Include(i => i.CixisGiris));
                if (icaze == null)
                    return Result.Fail("İcazə tapılmadı.");

                if (cixisVaxt.HasValue && qayidisVaxt.HasValue && qayidisVaxt.Value < cixisVaxt.Value)
                    return Result.Fail("Qayıdış vaxtı çıxış vaxtından əvvəl ola bilməz.");

                // Qeyd yoxdursa (məs. köhnə jeton icazələri bu qeyd olmadan yaranırdı) —
                // düzəliş zamanı YARADILIR ki, HR sintetik sətirdən də vaxt yaza bilsin.
                var cg = icaze.CixisGiris;
                bool yeniYarandi = false;
                if (cg == null)
                {
                    yeniYarandi = true;
                    cg = new IcazeCixisGiris
                    {
                        IcazeId = icaze.Id,
                        Birdefelik = icaze.Birdefelik,
                        Status = IcazeCixisGirisStatus.Gozlenir
                    };
                    await _unitOfWork.Repository<IcazeCixisGiris>().YaratAsync(cg);
                }
                cg.CixisVaxt = cixisVaxt;
                cg.QayidisVaxt = qayidisVaxt;
                cg.Status = qayidisVaxt.HasValue
                    ? IcazeCixisGirisStatus.Tamamlandi
                    : (cixisVaxt.HasValue ? IcazeCixisGirisStatus.Cixdi : IcazeCixisGirisStatus.Gozlenir);
                cg.YenilenmeTarixi = DateTime.Now;

                if (!yeniYarandi)
                    await _unitOfWork.Repository<IcazeCixisGiris>().YenileAsync(cg);
                await _unitOfWork.YaddaSaxlaAsync();
                return Result.Ok("Çıxış/qayıdış vaxtı yeniləndi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Düzəliş xətası: {ex.Message}");
            }
        }

        public async Task<Result<IList<IcazeListDto>>> GetFiltrliAsync(
            DateTime? tarixFrom, DateTime? tarixTo, int? departamentId, int? status, string? axtaris)
        {
            try
            {
                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        predicate: x =>
                            (tarixFrom == null || x.IcazeTarixi >= tarixFrom) &&
                            (tarixTo == null || x.IcazeTarixi <= tarixTo) &&
                            (status == null || (int)x.Status == status) &&
                            (departamentId == null || x.Isci.IsciTeyinatlari
                                .Any(t => t.Aktivdir && t.DepartamentId == departamentId)) &&
                            (axtaris == null || x.Isci.Ad.Contains(axtaris) ||
                                x.Isci.Soyad.Contains(axtaris)),
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Departament)
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                                    .ThenInclude(t => t.Vezife)
                            .Include(i => i.EvezEdenIsci)
                            .Include(i => i.SobeReisi)
                            .Include(i => i.Rehber)
                            .Include(i => i.HrTesdiqleyen)
                            .Include(i => i.CixisGiris),
                        izlemeden: true);

                var dtos = list
                    .OrderByDescending(x => x.IcazeTarixi)
                    .Select(ic => new IcazeListDto
                    {
                        Id = ic.Id,
                        IsciAdSoyad = ic.Isci.TamAd,
                        SobeAdi = ic.Isci.IsciTeyinatlari
                            .Where(t => t.Aktivdir)
                            .Select(t => t.Departament?.Ad)
                            .FirstOrDefault() ?? "-",
                        EvezEdenAdSoyad = ic.EvezEdenIsci?.TamAd ?? "—",
                        IcazeTarixi = ic.IcazeTarixi,
                        BaslamaSaati = ic.BaslamaSaati,
                        BitisSaati = ic.BitisSaati,
                        IcazeSaati = ic.IcazeSaati,
                        Sebeb = ic.Sebeb,
                        Status = ic.Status,
                        Birdefelik = ic.Birdefelik,
                        ImtinaSebebi = ic.ImtinaSebebi,
                        SobeReisiTesdiq = ic.SobeReisiTesdiq,
                        SobeReisiAdSoyad = ic.SobeReisi?.TamAd,
                        SobeReisiTesdiqTarixi = ic.SobeReisiTesdiqTarixi,
                        RehberTesdiq = ic.RehberTesdiq,
                        RehberAdSoyad = ic.Rehber?.TamAd,
                        RehberTesdiqTarixi = ic.RehberTesdiqTarixi,
                        HrTesdiq = ic.HrTesdiq,
                        HrAdSoyad = ic.HrTesdiqleyen?.TamAd,
                        HrTesdiqTarixi = ic.HrTesdiqTarixi,
                        CixisVaxt = ic.CixisGiris?.CixisVaxt,
                        QayidisVaxt = ic.CixisGiris?.QayidisVaxt,
                        CixisGirisStatus = ic.CixisGiris?.Status,
                    }).ToList();

                return Result<IList<IcazeListDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Xəta: {ex.Message}");
            }
        }
    }
}
