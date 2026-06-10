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

                return Result<IList<IcazeListDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Icazeler getirilmedi: {ex.Message}");
            }
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

                // Maksimum müddət — adi icazə 3 saat; nahar fasiləsi icazəyə qatılırsa 3 saat 45 dəqiqə
                var icazeDeq = (int)(dto.BitisSaati - dto.BaslamaSaati).TotalMinutes;
                var maxDeq = dto.NaharNezereAlinmasin ? 225 : 180;   // 3s45d : 3s
                if (icazeDeq > maxDeq)
                    return Result<IcazeListDto>.Fail(
                        dto.NaharNezereAlinmasin
                            ? "Nahar icazəyə qatıldıqda icazə 3 saat 45 dəqiqədən çox ola bilməz."
                            : "İcazə 3 saatdan çox ola bilməz. Nahara çıxmırsınızsa müraciətdə qeyd edin (max 3 saat 45 dəqiqə).");

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

        public async Task<Result<IcazeDetailDto>> GetDetayAsync(int icazeId)
        {
            try
            {
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
        public async Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd, int rehberId = 0, decimal jetonOdenenSaat = 0, bool naharNezereAlinmasin = false)
        {
            var icaze = await _unitOfWork.Repository<Icaze>().GetirAsync(x => x.Id == id);
            if (icaze == null) return Result.Fail("İcazə tapılmadı.");

            if (icaze.Status != IcazeStatus.RehberTesdiqinde)
                return Result.Fail($"Bu müraciət artıq emal edilib (status: {icaze.Status}).");

            // Nahar müddəti IsParametri-dən oxunur (default 45 dəq)
            var isParam = await _unitOfWork.Repository<IsParametri>()
                .Query().Where(x => !x.Silinib).FirstOrDefaultAsync();
            decimal naharMuddet = (isParam?.NaharMuddetDeqiqe ?? 45) / 60m;

            var icazeSaatiRaw = (decimal)icaze.IcazeSaati;
            var efektivSaat = naharNezereAlinmasin
                ? Math.Max(0m, icazeSaatiRaw - naharMuddet)
                : icazeSaatiRaw;

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
            icaze.NaharNezereAlinmasin = status && naharNezereAlinmasin;

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
        };

        public async Task<Result<IList<IcazeIsciIstatistikDto>>> GetIsciIzlemeAsync(IcazeIzlemeFiltrDto filtr)
        {
            try
            {
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

                // Nahar müddəti (saat) — nahar icazəyə qatılıbsa icazə saatından çıxılır
                // (işçi nahar haqqını istifadə etməyib).
                var izParam = await _unitOfWork.Repository<IsParametri>()
                    .Query().AsNoTracking().Where(x => !x.Silinib).FirstOrDefaultAsync();
                double naharSaat = (izParam?.NaharMuddetDeqiqe ?? 45) / 60.0;

                // Effektiv icazə saatı = xam saat − jeton (bonus, sayılmır) − nahar (istifadə edilməyib)
                double EfektivSaat(Icaze x) => Math.Max(0,
                    x.IcazeSaati
                    - (double)x.JetonOdenenSaat
                    - (x.NaharNezereAlinmasin ? naharSaat : 0));

                // ── Fallback: bağlanmamış (Gözlənir) icazədə cihaz çıxışını Davamiyyətdən göstər ──
                // Dövriyyə ilə eyni məntiq — canlı hook işləməyibsə (seed/təsdiqdən əvvəl oxuma),
                // həmin günün Davamiyyət çıxışı icazə pəncərəsinin içindədirsə çıxış kimi göstərilir.
                var izEksikler = list
                    .Where(ic => ic.CixisGiris != null
                              && ic.CixisGiris.CixisVaxt == null
                              && ic.CixisGiris.Status == IcazeCixisGirisStatus.Gozlenir)
                    .ToList();

                var davCixisMap = new Dictionary<string, DateTime>();
                if (izEksikler.Count > 0)
                {
                    var isciIdler = izEksikler.Select(ic => ic.IsciId).Distinct().ToList();
                    var minT = izEksikler.Min(ic => ic.IcazeTarixi.Date);
                    var maxT = izEksikler.Max(ic => ic.IcazeTarixi.Date);

                    var davlar = await _unitOfWork.Repository<Davamiyyet>()
                        .Query()
                        .AsNoTracking()
                        .Where(d => !d.Silinib && isciIdler.Contains(d.IsciId)
                                 && d.Tarix.Date >= minT && d.Tarix.Date <= maxT
                                 && d.CixisVaxti != null)
                        .Select(d => new { d.IsciId, Tarix = d.Tarix.Date, d.CixisVaxti })
                        .ToListAsync();

                    davCixisMap = davlar
                        .GroupBy(d => $"{d.IsciId}|{d.Tarix:yyyy-MM-dd}")
                        .ToDictionary(g => g.Key, g => g.Max(d => d.CixisVaxti!.Value));
                }

                DateTime? IzlemeCixisGoster(Icaze ic)
                {
                    if (ic.CixisGiris == null || ic.CixisGiris.CixisVaxt != null ||
                        ic.CixisGiris.Status != IcazeCixisGirisStatus.Gozlenir)
                        return null;
                    var key = $"{ic.IsciId}|{ic.IcazeTarixi:yyyy-MM-dd}";
                    if (!davCixisMap.TryGetValue(key, out var dcx)) return null;
                    var t = dcx.TimeOfDay;
                    if (t >= ic.BaslamaSaati - TimeSpan.FromMinutes(15) &&
                        t <= ic.BitisSaati  + TimeSpan.FromMinutes(15))
                        return dcx;
                    return null;
                }

                var grouped = list
                    .GroupBy(x => x.IsciId)
                    .Select(g =>
                    {
                        var ilk = g.First();
                        var aktivTeyinat = ilk.Isci?.IsciTeyinatlari?.FirstOrDefault(t => t.Aktivdir);
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
                            UmumSaat = g.Sum(x => EfektivSaat(x)),
                            TesdiqSaat = g.Where(x => x.Status == IcazeStatus.Tesdiqlenib).Sum(x => EfektivSaat(x)),
                            SonIcazeTarixi = g.Max(x => (DateTime?)x.IcazeTarixi),
                            Icazeler = g.OrderByDescending(x => x.IcazeTarixi).Select(x =>
                            {
                                var dto = MapToListDto(x);
                                var faktikiCixis = IzlemeCixisGoster(x);
                                if (faktikiCixis != null) dto.CixisVaxt = faktikiCixis;
                                return dto;
                            }).ToList(),
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

                // ── Fallback: bağlanmamış (Gözlənir) icazələrdə cihaz çıxışını Davamiyyətdən götür ──
                // Canlı ADMS hook işləməyibsə (seed data, yaxud oxuma təsdiqdən əvvəl gəlib),
                // həmin günün Davamiyyət çıxışı icazə pəncərəsinin İÇİNDƏdirsə onu faktiki çıxış
                // kimi GÖSTƏRİR (DB-yə YAZMIR). Yalnız çıxış — qayıdış təxmin edilmir, çünki xam
                // cihaz oxumaları saxlanmır (qayıdan işçidə son çıxış günün sonu olur, icazə yox).
                var eksikler = list
                    .Where(c => c.CixisVaxt == null &&
                                c.Status == IcazeCixisGirisStatus.Gozlenir)
                    .ToList();

                var davCixisMap = new Dictionary<string, DateTime>();
                if (eksikler.Count > 0)
                {
                    var isciIdler = eksikler.Select(c => c.Icaze.IsciId).Distinct().ToList();
                    var minT = eksikler.Min(c => c.Icaze.IcazeTarixi.Date);
                    var maxT = eksikler.Max(c => c.Icaze.IcazeTarixi.Date);

                    var davlar = await _unitOfWork.Repository<Davamiyyet>()
                        .Query()
                        .AsNoTracking()
                        .Where(d => !d.Silinib && isciIdler.Contains(d.IsciId)
                                 && d.Tarix.Date >= minT && d.Tarix.Date <= maxT
                                 && d.CixisVaxti != null)
                        .Select(d => new { d.IsciId, Tarix = d.Tarix.Date, d.CixisVaxti })
                        .ToListAsync();

                    davCixisMap = davlar
                        .GroupBy(d => $"{d.IsciId}|{d.Tarix:yyyy-MM-dd}")
                        .ToDictionary(g => g.Key, g => g.Max(d => d.CixisVaxti!.Value));
                }

                // İcazə pəncərəsinin içindəki cihaz çıxışını qaytarır (yoxsa null)
                DateTime? FaktikiCixisGoster(IcazeCixisGiris c)
                {
                    if (c.CixisVaxt != null || c.Status != IcazeCixisGirisStatus.Gozlenir)
                        return null;
                    var key = $"{c.Icaze.IsciId}|{c.Icaze.IcazeTarixi:yyyy-MM-dd}";
                    if (!davCixisMap.TryGetValue(key, out var dcx)) return null;
                    var t = dcx.TimeOfDay;
                    if (t >= c.Icaze.BaslamaSaati - TimeSpan.FromMinutes(15) &&
                        t <= c.Icaze.BitisSaati  + TimeSpan.FromMinutes(15))
                        return dcx;
                    return null;
                }

                var dtos = list
                    .OrderByDescending(x => x.Icaze.IcazeTarixi)
                    .Select(c =>
                    {
                        var faktikiCixis = FaktikiCixisGoster(c);
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
                            Birdefelik = c.Birdefelik,
                            CixisVaxt = c.CixisVaxt ?? faktikiCixis,
                            QayidisVaxt = c.QayidisVaxt,
                            FaktikiSaat = c.FaktikiSaat,
                            CixisStatus = faktikiCixis != null
                                ? IcazeCixisGirisStatus.Cixdi
                                : c.Status,
                        };
                    }).ToList();

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
                if (icaze?.CixisGiris == null)
                    return Result.Fail("İcazə və ya çıxış/giriş qeydi tapılmadı.");

                if (cixisVaxt.HasValue && qayidisVaxt.HasValue && qayidisVaxt.Value < cixisVaxt.Value)
                    return Result.Fail("Qayıdış vaxtı çıxış vaxtından əvvəl ola bilməz.");

                var cg = icaze.CixisGiris;
                cg.CixisVaxt = cixisVaxt;
                cg.QayidisVaxt = qayidisVaxt;
                cg.Status = qayidisVaxt.HasValue
                    ? IcazeCixisGirisStatus.Tamamlandi
                    : (cixisVaxt.HasValue ? IcazeCixisGirisStatus.Cixdi : IcazeCixisGirisStatus.Gozlenir);
                cg.YenilenmeTarixi = DateTime.Now;

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
