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
    .Include(m => m.EvezEdenIsci),
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
                // Saat yoxlaması
                if (dto.BitisSaati <= dto.BaslamaSaati)
                    return Result<IcazeListDto>.Fail("Bitme saati baslama saatindan sonra olmalidir.");

                // İşçinin roluna görə status müəyyən et
                IcazeStatus ilkinStatus;
                int? departamentId = null;

                if (dto.MuracietSahibiRehberdirmi)
                {
                    // Rəhbər birbaşa HR-a gedir
                    ilkinStatus = IcazeStatus.HrTesdiqinde;
                }
                else if (dto.MuracietSahibiSobeReisidirmi)
                {
                    // Şöbə rəisi öz addımını keçir, rəhbərə gedir
                    ilkinStatus = IcazeStatus.RehberTesdiqinde;
                }
                else
                {
                    // Adi işçi — şöbə rəisinə gedir
                    var teyinat = await _unitOfWork.Repository<IsciTeyinat>()
                        .GetirAsync(x => x.IsciId == dto.IsciId && x.Aktivdir);

                    departamentId = teyinat?.DepartamentId;

                    var sobeReisiVar = teyinat != null && await _unitOfWork.Repository<IsciStrukturRolu>()
                        .MovcuddurmuAsync(x =>
                            x.DepartamentId == teyinat.DepartamentId &&
                            x.RolTipi == StrukturRolTipi.SobeReisi &&
                            x.IsciId != dto.IsciId &&
                            x.Aktivdir);

                    ilkinStatus = sobeReisiVar
                        ? IcazeStatus.SobeReisiTesdiqinde
                        : IcazeStatus.RehberTesdiqinde;
                }

                var entity = new Icaze
                {
                    IsciId = dto.IsciId,
                    EvezEdenIsciId = dto.EvezEdenIsciId.HasValue ? dto.EvezEdenIsciId.Value : null,
                    IcazeTarixi = dto.IcazeTarixi,
                    BaslamaSaati = dto.BaslamaSaati,
                    BitisSaati = dto.BitisSaati,
                    Sebeb = dto.Sebeb,
                    Status = ilkinStatus
                };

                await _unitOfWork.Repository<Icaze>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // Navigation property-ləri yüklə
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
                            .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                // Bildiriş — sonrakı mərhələ təsdiqçilərinə
                await NotifyApproversForCreateAsync(entity, departamentId);

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

                if (icaze.Status != IcazeStatus.Gozlemede &&
                    icaze.Status != IcazeStatus.SobeReisiTesdiqinde &&
                    icaze.Status != IcazeStatus.RehberTesdiqinde &&
                    icaze.Status != IcazeStatus.HrTesdiqinde)
                    return Result.Fail("Hələ təsdiq olunmamış icazə ləğv edilə bilər.");

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
                            .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                if (icaze == null)
                    return Result<IcazeDetailDto>.Fail("Tapilmadi.");

                var dto = new IcazeDetailDto
                {
                    Id = icaze.Id,
                    IsciAdSoyad = icaze.Isci.TamAd,
                    SobeAdi = icaze.Isci.IsciTeyinatlari
                    .Where(t => t.Aktivdir)
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
                    SobeReisiTesdiq = icaze.SobeReisiTesdiq,
                    SobeReisiTesdiqTarixi = icaze.SobeReisiTesdiqTarixi,
                    RehberTesdiq = icaze.RehberTesdiq,
                    RehberTesdiqTarixi = icaze.RehberTesdiqTarixi,
                    HrTesdiq = icaze.HrTesdiq,
                    HrTesdiqTarixi = icaze.HrTesdiqTarixi,
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
                            .Include(i => i.EvezEdenIsci),
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

        public async Task<Result<IList<IcazeListDto>>> GetGozlemededeAsync()
        {
            try
            {
                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        predicate: x => x.Status == IcazeStatus.Gozlemede,
                        include: q => q
    .Include(i => i.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Departament)
    .Include(i => i.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Vezife)
    .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                var dtos = list
                    .OrderByDescending(x => x.IcazeTarixi)
                    .Select(MapToListDto)
                    .ToList();

                return Result<IList<IcazeListDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Gözləmədə icazələr gətirilmədi: {ex.Message}");
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

                var dtos = list
                    .OrderByDescending(x => x.IcazeTarixi)
                    .Select(MapToListDto)
                    .ToList();

                return Result<IList<IcazeListDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Rəhbər təsdiqindəki icazələr gətirilmədi: {ex.Message}");
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

                var dtos = list
                    .OrderByDescending(x => x.IcazeTarixi)
                    .Select(MapToListDto)
                    .ToList();

                return Result<IList<IcazeListDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"HR təsdiqindəki icazələr gətirilmədi: {ex.Message}");
            }
        }

        // IcazeService.cs — 3 metodu belə yeniləyin:

        public async Task<Result> SobeReisiTesdiqAsync(int id, bool status, string? qeyd, int sobeReisiId = 0)
        {
            var icaze = await _unitOfWork.Repository<Icaze>()
                .GetirAsync(x => x.Id == id);
            if (icaze == null) return Result.Fail("İcazə tapılmadı.");

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

        public async Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd, int rehberId = 0)
        {
            var icaze = await _unitOfWork.Repository<Icaze>()
                .GetirAsync(x => x.Id == id);
            if (icaze == null) return Result.Fail("İcazə tapılmadı.");

            icaze.RehberTesdiq = status;
            icaze.RehberId = rehberId > 0 ? rehberId : icaze.RehberId;
            icaze.RehberTesdiqTarixi = DateTime.Now;
            icaze.Status = status ? IcazeStatus.HrTesdiqinde : IcazeStatus.ImtinaEdildi;
            if (!status) icaze.ImtinaSebebi = qeyd;

            await _unitOfWork.Repository<Icaze>().YenileAsync(icaze);
            await _unitOfWork.YaddaSaxlaAsync();

            if (status)
            {
                await NotifyIsciProgressAsync(icaze, "Rəhbər", true, qeyd);
                await NotifyAllHrAsync(icaze);
            }
            else
            {
                await NotifyIsciProgressAsync(icaze, "Rəhbər", false, qeyd);
            }
            return Result.Ok("Rəhbər qərarı qeydə alındı.");
        }

        public async Task<Result> HrTesdiqAsync(int id, bool status, string? qeyd, int hrId = 0)
        {
            var icaze = await _unitOfWork.Repository<Icaze>()
                .GetirAsync(x => x.Id == id);
            if (icaze == null) return Result.Fail("İcazə tapılmadı.");

            icaze.HrTesdiq = status;
            icaze.HrId = hrId > 0 ? hrId : icaze.HrId;
            icaze.HrTesdiqTarixi = DateTime.Now;
            icaze.Status = status ? IcazeStatus.Tesdiqlenib : IcazeStatus.ImtinaEdildi;
            if (!status) icaze.ImtinaSebebi = qeyd;

            await _unitOfWork.Repository<Icaze>().YenileAsync(icaze);
            await _unitOfWork.YaddaSaxlaAsync();

            await NotifyIsciProgressAsync(icaze, "HR", status, qeyd);
            return Result.Ok("HR qərarı qeydə alındı.");
        }

        // ════════════════════════════════════════════════════════
        // Bildiriş köməkçiləri — bütün icazə iş axını üçün
        // ════════════════════════════════════════════════════════

        private async Task NotifyApproversForCreateAsync(Icaze ic, int? departamentId)
        {
            var isciAd = await GetIsciAdAsync(ic.IsciId);
            var dovr = $"{ic.IcazeTarixi:dd.MM.yyyy} {ic.BaslamaSaati:hh\\:mm}–{ic.BitisSaati:hh\\:mm}";
            var bashliq = "Yeni icazə müraciəti";
            var metn = $"{isciAd} ({dovr}) icazə müraciəti göndərdi.";

            switch (ic.Status)
            {
                case IcazeStatus.SobeReisiTesdiqinde:
                    if (departamentId.HasValue)
                    {
                        await _bildirisRouter.NotifyDepartmentRoleAsync(
                            departamentId.Value, StrukturRolTipi.SobeReisi,
                            BildirisNovu.IcazeMuraciet, bashliq, metn,
                            redirectUrl: $"/User/Tesdiq/IcazeDetal/{ic.Id}?rol=SobeReisi",
                            icazeId: ic.Id, exceptIsciId: ic.IsciId);
                    }
                    break;

                case IcazeStatus.RehberTesdiqinde:
                    await _bildirisRouter.NotifyRolesAsync(
                        new[] { RoleNames.Rehber, RoleNames.Admin },
                        BildirisNovu.IcazeMuraciet, bashliq, metn,
                        redirectUrl: $"/User/Tesdiq/IcazeDetal/{ic.Id}?rol=Rehber",
                        icazeId: ic.Id, exceptIsciId: ic.IsciId);
                    break;

                case IcazeStatus.HrTesdiqinde:
                    await _bildirisRouter.NotifyRolesAsync(
                        new[] { RoleNames.HR, RoleNames.Admin },
                        BildirisNovu.IcazeMuraciet, bashliq, metn,
                        redirectUrl: $"/User/Tesdiq/IcazeDetal/{ic.Id}?rol=Hr",
                        icazeId: ic.Id, exceptIsciId: ic.IsciId);
                    break;
            }
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
            await _bildirisRouter.NotifyRolesAsync(
                new[] { RoleNames.HR, RoleNames.Admin },
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
                    ic.IsciId,
                    BildirisNovu.IcazeTesdiq,
                    $"İcazə — {mərhələ} təsdiqi alındı",
                    $"{dovr} icazə müraciətiniz {mərhələ} tərəfindən təsdiqləndi.",
                    redirectUrl: redirectUrl,
                    icazeId: ic.Id);
            }
            else
            {
                var sebeb = string.IsNullOrWhiteSpace(qeyd) ? "" : $" Səbəb: {qeyd}";
                await _bildirisRouter.NotifyIsciAsync(
                    ic.IsciId,
                    BildirisNovu.IcazeImtina,
                    $"İcazə — {mərhələ} imtinası",
                    $"{dovr} icazə müraciətiniz {mərhələ} tərəfindən rədd edildi.{sebeb}",
                    redirectUrl: redirectUrl,
                    icazeId: ic.Id);
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
    .Select(t => t.Departament.Ad)
    .FirstOrDefault() ?? "-",
            EvezEdenAdSoyad = icaze.EvezEdenIsci?.TamAd ?? "—",
            IcazeTarixi = icaze.IcazeTarixi,
            BaslamaSaati = icaze.BaslamaSaati,
            BitisSaati = icaze.BitisSaati,
            IcazeSaati = icaze.IcazeSaati,
            Sebeb = icaze.Sebeb,
            Status = icaze.Status,
            SobeReisiTesdiq = icaze.SobeReisiTesdiq,
            SobeReisiTesdiqTarixi = icaze.SobeReisiTesdiqTarixi,
            RehberTesdiq = icaze.RehberTesdiq,
            RehberTesdiqTarixi = icaze.RehberTesdiqTarixi,
            HrTesdiq = icaze.HrTesdiq,
            HrTesdiqTarixi = icaze.HrTesdiqTarixi,
        };
        public async Task<Result<IList<IcazeListDto>>> GetFiltrliAsync(
    DateTime? tarixFrom,
    DateTime? tarixTo,
    int? departamentId,
    int? status,
    string? axtaris)
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
                            .Include(i => i.HrTesdiqleyen),
                        izlemeden: true);

                var dtos = list
                    .OrderByDescending(x => x.IcazeTarixi)
                    .Select(ic => new IcazeListDto
                    {
                        Id = ic.Id,
                        IsciAdSoyad = ic.Isci.TamAd,
                        SobeAdi = ic.Isci.IsciTeyinatlari
                            .Where(t => t.Aktivdir)
                            .Select(t => t.Departament.Ad)
                            .FirstOrDefault() ?? "-",
                        EvezEdenAdSoyad = ic.EvezEdenIsci?.TamAd ?? "—",
                        IcazeTarixi = ic.IcazeTarixi,
                        BaslamaSaati = ic.BaslamaSaati,
                        BitisSaati = ic.BitisSaati,
                        IcazeSaati = ic.IcazeSaati,
                        Sebeb = ic.Sebeb,
                        Status = ic.Status,
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