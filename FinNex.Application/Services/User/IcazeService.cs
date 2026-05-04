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

        public IcazeService(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IMapper mapper,
            IBildirisRouter bildirisRouter)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _mapper = mapper;
            _bildirisRouter = bildirisRouter;
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
                    Status = ilkinStatus
                };

                await _unitOfWork.Repository<Icaze>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // Rəhbər özü müraciət edibsə — IcazeCixisGiris dərhal yarat
                if (ilkinStatus == IcazeStatus.Tesdiqlenib)
                {
                    entity.RehberTesdiq = true;
                    entity.RehberTesdiqTarixi = DateTime.Now;
                    await _unitOfWork.Repository<Icaze>().YenileAsync(entity);
                    await _YaratCixisGirisAsync(entity.Id, false);
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
        public async Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd, int rehberId = 0)
        {
            var icaze = await _unitOfWork.Repository<Icaze>().GetirAsync(x => x.Id == id);
            if (icaze == null) return Result.Fail("İcazə tapılmadı.");

            if (icaze.Status != IcazeStatus.RehberTesdiqinde)
                return Result.Fail($"Bu müraciət artıq emal edilib (status: {icaze.Status}).");

            icaze.RehberTesdiq = status;
            icaze.RehberId = rehberId > 0 ? rehberId : icaze.RehberId;
            icaze.RehberTesdiqTarixi = DateTime.Now;

            if (!status)
            {
                icaze.Status = IcazeStatus.ImtinaEdildi;
                icaze.ImtinaSebebi = qeyd;
                await _unitOfWork.Repository<Icaze>().YenileAsync(icaze);
                await _unitOfWork.YaddaSaxlaAsync();
                await NotifyIsciProgressAsync(icaze, "Rəhbər", false, qeyd);
                return Result.Ok("Rəhbər qərarı qeydə alındı.");
            }

            // Müraciətçinin HR rolu varmı? Varsa bu addımda bitir
            var muracietciHrdir = await _unitOfWork.Repository<IsciStrukturRolu>()
                .MovcuddurmuAsync(x =>
                    x.IsciId == icaze.IsciId &&
                    x.RolTipi == StrukturRolTipi.Hr &&
                    x.Aktivdir);

            icaze.Status = muracietciHrdir ? IcazeStatus.Tesdiqlenib : IcazeStatus.HrTesdiqinde;

            if (muracietciHrdir)
            {
                icaze.Birdefelik = icaze.Birdefelik; // HR olmadığı üçün birdefelik burda false qalır
                icaze.HrTesdiq = true;
                icaze.HrTesdiqTarixi = DateTime.Now;
            }

            await _unitOfWork.Repository<Icaze>().YenileAsync(icaze);
            await _unitOfWork.YaddaSaxlaAsync();

            await NotifyIsciProgressAsync(icaze, "Rəhbər", true, qeyd);

            if (icaze.Status == IcazeStatus.Tesdiqlenib)
            {
                // HR müraciət edib, Rəhbər təsdiqlədi → birbaşa tesdiqlenib
                await _YaratCixisGirisAsync(icaze.Id, icaze.Birdefelik);
                await NotifySobeReisiAsync(icaze);
                await NotifyHrMalumatAsync(icaze);
            }
            else
            {
                // Normal işçi — HR-a göndər
                await NotifyAllHrAsync(icaze);
            }

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
                await _YaratCixisGirisAsync(icaze.Id, birdefelik);
                await NotifySobeReisiAsync(icaze);
            }

            return Result.Ok("HR qərarı qeydə alındı.");
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
                    // Directly use structural role — doesn't require AppUser.IsciId link
                    await _bildirisRouter.NotifyStrukturRoluAsync(
                        StrukturRolTipi.Rehber,
                        BildirisNovu.IcazeMuraciet, bashliq, metn,
                        redirectUrl: $"/User/Tesdiq/IcazeDetal/{ic.Id}?rol=Rehber",
                        icazeId: ic.Id, exceptIsciId: ic.IsciId);
                    // Also notify Admin via Identity role
                    await _bildirisRouter.NotifyRoleAsync(
                        RoleNames.Admin,
                        BildirisNovu.IcazeMuraciet, bashliq, metn,
                        redirectUrl: $"/User/Tesdiq/IcazeDetal/{ic.Id}?rol=Rehber",
                        icazeId: ic.Id, exceptIsciId: ic.IsciId);
                    break;

                case IcazeStatus.HrTesdiqinde:
                    await _bildirisRouter.NotifyStrukturRoluAsync(
                        StrukturRolTipi.Hr,
                        BildirisNovu.IcazeMuraciet, bashliq, metn,
                        redirectUrl: $"/User/Tesdiq/IcazeDetal/{ic.Id}?rol=Hr",
                        icazeId: ic.Id, exceptIsciId: ic.IsciId);
                    await _bildirisRouter.NotifyRoleAsync(
                        RoleNames.Admin,
                        BildirisNovu.IcazeMuraciet, bashliq, metn,
                        redirectUrl: $"/User/Tesdiq/IcazeDetal/{ic.Id}?rol=Hr",
                        icazeId: ic.Id, exceptIsciId: ic.IsciId);
                    break;
            }
        }

        // Rəhbər/HR tesdiqinden sonra Şöbə Rəisinə məlumat bildirişi
        private async Task NotifySobeReisiAsync(Icaze ic)
        {
            var isciAd = await GetIsciAdAsync(ic.IsciId);
            var dovr = $"{ic.IcazeTarixi:dd.MM.yyyy} {ic.BaslamaSaati:hh\\:mm}–{ic.BitisSaati:hh\\:mm}";
            await _bildirisRouter.NotifyStrukturRoluAsync(
                StrukturRolTipi.SobeReisi,
                BildirisNovu.IcazeTesdiq,
                "İcazə təsdiqləndi — məlumat",
                $"{isciAd} ({dovr}) icazəsi təsdiqləndi.",
                redirectUrl: $"/User/Icaze/Dovriyye",
                icazeId: ic.Id, exceptIsciId: ic.IsciId);
        }

        // Rəhbər tesdiqinden sonra HR-a məlumat bildirişi (HR fallback deyilsə)
        private async Task NotifyHrMalumatAsync(Icaze ic)
        {
            var isciAd = await GetIsciAdAsync(ic.IsciId);
            var dovr = $"{ic.IcazeTarixi:dd.MM.yyyy} {ic.BaslamaSaati:hh\\:mm}–{ic.BitisSaati:hh\\:mm}";
            await _bildirisRouter.NotifyStrukturRoluAsync(
                StrukturRolTipi.Hr,
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
            await _bildirisRouter.NotifyStrukturRoluAsync(
                StrukturRolTipi.Rehber,
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
            await _bildirisRouter.NotifyStrukturRoluAsync(
                StrukturRolTipi.Hr,
                BildirisNovu.IcazeMuraciet,
                "İcazə müraciəti — HR təsdiqi gözləyir",
                $"{isciAd} ({dovr}) icazəsi rəhbər tərəfindən təsdiqlənib.",
                redirectUrl: $"/User/Tesdiq/IcazeDetal/{ic.Id}?rol=Hr",
                icazeId: ic.Id, exceptIsciId: ic.IsciId);
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
                            UmumSaat = g.Sum(x => x.IcazeSaati),
                            TesdiqSaat = g.Where(x => x.Status == IcazeStatus.Tesdiqlenib).Sum(x => x.IcazeSaati),
                            SonIcazeTarixi = g.Max(x => (DateTime?)x.IcazeTarixi),
                            Icazeler = g.OrderByDescending(x => x.IcazeTarixi).Select(MapToListDto).ToList(),
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

                var dtos = list
                    .OrderByDescending(x => x.Icaze.IcazeTarixi)
                    .Select(c => new IcazeDovriyyeDto
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
                        CixisVaxt = c.CixisVaxt,
                        QayidisVaxt = c.QayidisVaxt,
                        FaktikiSaat = c.FaktikiSaat,
                        CixisStatus = c.Status,
                    }).ToList();

                return Result<IList<IcazeDovriyyeDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeDovriyyeDto>>.Fail($"Dövriyyə məlumatları gətirilmədi: {ex.Message}");
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
