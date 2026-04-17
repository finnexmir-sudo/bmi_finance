using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Application.Services;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class MezuniyyetService : ServiceAsync<Mezuniyyet, MezuniyyetDto, MezuniyyetCreateDto, MezuniyyetUpdateDto>, IMezuniyyetService
{
    private readonly IEvezediciTesdiqService _evezediciTesdiqService;
    private readonly IMaasHesablamaService _maasHesablamaService;
    private readonly IBildirisRouter _bildirisRouter;

    public MezuniyyetService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IEvezediciTesdiqService evezediciTesdiqService,
        IMaasHesablamaService maasHesablamaService,
        IBildirisRouter bildirisRouter)
        : base(unitOfWork, mapper)
    {
        _evezediciTesdiqService = evezediciTesdiqService;
        _maasHesablamaService = maasHesablamaService;
        _bildirisRouter = bildirisRouter;
    }

    
    public async Task<Result<IList<MezuniyyetListDto>>> GetListAsync()
    {
        var entities = await _unitOfWork.Repository<Mezuniyyet>()
            .HamisiniGetirAsync(include: x => x.Include(i => i.Isci), izlemeden: true);

        var data = _mapper.Map<IList<MezuniyyetListDto>>(entities);
        return Result<IList<MezuniyyetListDto>>.Ok(data);
    }

    public override async Task<Result<MezuniyyetDto>> YaratAsync(MezuniyyetCreateDto dto)
    {
        using (var transaction = await _unitOfWork.BeginTransactionAsync())
        {
            try
            {
                // 1. İş günlərini hesabla
                int isGunu = await HesablaIsGunuAsync(dto.BaslamaTarixi, dto.BitmeTarixi);

                // 2. Balansı növə görə yoxla (Xəstəlik və Ezamiyyət limitsizdir)
                if (dto.Nov == MezuniyyetNovu.Illik)
                {
                    var balans = await _unitOfWork.Repository<MezuniyyetBalans>()
                        .GetirAsync(x => x.IsciId == dto.IsciId && x.Il == dto.BaslamaTarixi.Year && x.Nov == dto.Nov);

                    if (balans == null || balans.QaliqGun < isGunu)
                        return Result<MezuniyyetDto>.Fail($"Kifayət qədər {dto.Nov} məzuniyyət balansınız yoxdur.");
                }

                // 3. İşçinin aktiv departamentini tap
                var teyinat = await _unitOfWork.Repository<IsciTeyinat>()
                    .GetirAsync(x => x.IsciId == dto.IsciId && x.Aktivdir);

                // 4. İşçinin roluna görə status müəyyən et
                MezuniyyetStatus ilkinStatus;

                if (dto.MuracietSahibiRehberdirmi)
                {
                    // Rəhbər birbaşa HR-a gedir
                    ilkinStatus = MezuniyyetStatus.HrTesdiqinde;
                }
                else if (dto.MuracietSahibiSobeReisidirmi)
                {
                    // Şöbə rəisi öz addımını keçir, rəhbərə gedir
                    ilkinStatus = MezuniyyetStatus.RehberTesdiqinde;
                }
                else
                {
                    // Adi işçi — departamentdə şöbə rəisi varsa ona, yoxsa rəhbərə
                    var sobeReisiVar = teyinat != null && await _unitOfWork.Repository<IsciStrukturRolu>()
                        .MovcuddurmuAsync(x =>
                            x.DepartamentId == teyinat.DepartamentId &&
                            x.RolTipi == StrukturRolTipi.SobeReisi &&
                            x.IsciId != dto.IsciId &&
                            x.Aktivdir);

                    ilkinStatus = sobeReisiVar
                        ? MezuniyyetStatus.SobeReisiTesdiqinde
                        : MezuniyyetStatus.RehberTesdiqinde;
                }

                // 6. Entity-ni yarat
                var entity = _mapper.Map<Mezuniyyet>(dto);
                entity.IsGunlerininSayi = isGunu;

                // Əvəzedici seçilibsə əvvəlcə onun cavabını gözlə
                entity.Status = dto.EvezEdenIsciId.HasValue
                    ? MezuniyyetStatus.Gozlemede
                    : ilkinStatus;

                await _unitOfWork.Repository<Mezuniyyet>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();
                // Əvəzedici varsa sorğu yarat
                if (dto.EvezEdenIsciId.HasValue)
                {
                    await _evezediciTesdiqService.YaratAsync(entity.Id, dto.EvezEdenIsciId.Value);
                }
                await transaction.CommitAsync();

                // Bildiriş — yalnız əvəzedici GÖZLƏMƏDƏ deyilsə göndər;
                // əks halda əvvəlcə əvəzedici cavab verməlidir.
                if (entity.Status != MezuniyyetStatus.Gozlemede)
                {
                    await NotifyApproversForCreateAsync(entity, teyinat?.DepartamentId);
                }

                // Navigation property-lər yüklənməyib, manual DTO yaradılır
                var resultDto = new MezuniyyetDto
                {
                    Id = entity.Id,
                    IsciId = entity.IsciId,
                    Nov = entity.Nov,
                    Status = entity.Status,
                    BaslamaTarixi = entity.BaslamaTarixi,
                    BitmeTarixi = entity.BitmeTarixi,
                    IsGunlerininSayi = entity.IsGunlerininSayi,
                    Qeyd = entity.Qeyd
                };
                return Result<MezuniyyetDto>.Ok(resultDto, "Müraciət uğurla göndərildi.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return Result<MezuniyyetDto>.Fail("Xəta baş verdi.");
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // YENİLƏ — Mövcud entity-ni fetch edib yalnız redaktə olunan sahələri yeniləyir
    // ══════════════════════════════════════════════════════

    public new async Task<Result> YenileAsync(MezuniyyetUpdateDto dto)
    {
        try
        {
            var entity = await _unitOfWork.Repository<Mezuniyyet>()
                .GetirAsync(x => x.Id == dto.Id);

            if (entity == null)
                return Result.Fail("Müraciət tapılmadı.");

            // Tarixlər dəyişibsə iş günlərini yenidən hesabla
            bool tarixDeyisib = entity.BaslamaTarixi != dto.BaslamaTarixi || entity.BitmeTarixi != dto.BitmeTarixi;

            entity.BaslamaTarixi = dto.BaslamaTarixi;
            entity.BitmeTarixi = dto.BitmeTarixi;
            entity.Nov = dto.Nov;
            entity.EvezEdenIsciId = dto.EvezEdenIsciId;
            entity.Qeyd = dto.Qeyd;
            entity.Status = dto.Status;
            entity.ImtinaSebebi = dto.ImtinaSebebi;

            if (tarixDeyisib)
                entity.IsGunlerininSayi = await HesablaIsGunuAsync(dto.BaslamaTarixi, dto.BitmeTarixi);

            await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();

            return Result.Ok("Məzuniyyət uğurla yeniləndi.");
        }
        catch
        {
            return Result.Fail("Yenilənmə zamanı xəta baş verdi.");
        }
    }

    // Təsdiq Metodları (Workflow məntiqi)
    public async Task<Result> SobeReisiTesdiqAsync(int id, bool status, string? qeyd, int sobeReisiId)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>()
            .GetirAsync(x => x.Id == id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        m.SobeReisiTesdiq = status;
        m.SobeReisiId = sobeReisiId;
        m.SobeReisiTesdiqTarixi = DateTime.Now;
        m.Status = status
            ? MezuniyyetStatus.RehberTesdiqinde
            : MezuniyyetStatus.ImtinaEdildi;

        if (!status) m.ImtinaSebebi = qeyd;

        await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m);
        await _unitOfWork.YaddaSaxlaAsync();

        if (status)
        {
            // İşçiyə progress, sonrakı mərhələ təsdiqçilərinə (Rəhbər) sorğu
            await NotifyIsciProgressAsync(m, "Şöbə rəisi", true, qeyd);
            await NotifyAllRehberAsync(m);
        }
        else
        {
            await NotifyIsciProgressAsync(m, "Şöbə rəisi", false, qeyd);
        }

        return Result.Ok("Şöbə rəisi qərarı qeydə alındı.");
    }

    // --- Yardımçı Alqoritm: İş günlərini bayramlara görə hesablayır ---
    private async Task<int> HesablaIsGunuAsync(DateTime baslangic, DateTime bitis)
    {
        var bayramlar = await _unitOfWork.Repository<BayramGunu>()
            .HamisiniGetirAsync(x => x.Tarix >= baslangic && x.Tarix <= bitis);

        int count = 0;
        for (var date = baslangic; date <= bitis; date = date.AddDays(1))
        {
            // Şənbə (6) və Bazar (0) günləri deyilsə VƏ bayram günləri siyahısında yoxdursa
            if (date.DayOfWeek != DayOfWeek.Saturday &&
                date.DayOfWeek != DayOfWeek.Sunday &&
                !bayramlar.Any(b => b.Tarix.Date == date.Date))
            {
                count++;
            }
        }
        return count;
    }

    public async Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd, int rehberId)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>()
            .GetirAsync(x => x.Id == id); // ← izlemeden olmadan, tracking ilə

        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        m.RehberTesdiq = status;
        m.RehberId = rehberId;
        m.RehberTesdiqTarixi = DateTime.Now;
        m.Status = status
            ? MezuniyyetStatus.HrTesdiqinde
            : MezuniyyetStatus.ImtinaEdildi;

        if (!status) m.ImtinaSebebi = qeyd;

        await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m); // ← explicit update
        await _unitOfWork.YaddaSaxlaAsync();

        if (status)
        {
            await NotifyIsciProgressAsync(m, "Rəhbər", true, qeyd);
            await NotifyAllHrAsync(m);
        }
        else
        {
            await NotifyIsciProgressAsync(m, "Rəhbər", false, qeyd);
        }

        return Result.Ok("Rəhbər qərarı qeydə alındı.");
    }
    // HrTesdiqAsync metodu
    public async Task<Result> HrTesdiqAsync(int id, bool status, string? qeyd, int hrId)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>()
            .GetirAsync(x => x.Id == id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        m.HrTesdiq = status;
        m.HrId = hrId;
        m.HrTesdiqTarixi = DateTime.Now;
        m.Status = status
            ? MezuniyyetStatus.Tesdiqlenib
            : MezuniyyetStatus.ImtinaEdildi;

        if (!status) m.ImtinaSebebi = qeyd;

        // Qabaqcadan ödəniş seçilibsə: HR təsdiq anında məbləğ hesablanır, status
        // “Gozleyir” olur. Mühasib ayrıca səhifədə yoxlayıb “Ödənildi” vurur.
        if (status && m.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis)
        {
            var hesab = await _maasHesablamaService
                .MezuniyyetOdenisiDetalliHesablaAsync(m.IsciId, m.BaslamaTarixi, m.BitmeTarixi);
            m.OdenenMebleg = hesab.CemiOdenis;
            m.OdenisStatus = MezuniyyetOdenisStatus.Gozleyir;
        }

        await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m);

        // Təsdiqlənibsə: balansı yenilə + davamiyyətdə icazəli qeydlər yarat
        if (status)
        {
            // Balansı yenilə
            var balans = await _unitOfWork.Repository<MezuniyyetBalans>()
                .GetirAsync(x => x.IsciId == m.IsciId && x.Il == m.BaslamaTarixi.Year && x.Nov == m.Nov);

            if (balans != null)
            {
                balans.IstifadeOlunanGun += m.IsGunlerininSayi;
                await _unitOfWork.Repository<MezuniyyetBalans>().YenileAsync(balans);
            }

            // Davamiyyətdə növə görə qeydlər yarat (hər iş günü üçün)
            var davamiyyetStatusu = m.Nov switch
            {
                MezuniyyetNovu.Xestelik => DavamiyyetStatus.Xestelik,
                MezuniyyetNovu.Ezamiyyet => DavamiyyetStatus.Ezamiyyet,
                _ => DavamiyyetStatus.Icazeli
            };

            var bayramlar = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x => x.Tarix >= m.BaslamaTarixi && x.Tarix <= m.BitmeTarixi);

            for (var gun = m.BaslamaTarixi; gun <= m.BitmeTarixi; gun = gun.AddDays(1))
            {
                if (gun.DayOfWeek == DayOfWeek.Saturday || gun.DayOfWeek == DayOfWeek.Sunday)
                    continue;
                if (bayramlar.Any(b => b.Tarix.Date == gun.Date))
                    continue;

                // Artıq qeyd varsa yaratma
                var movcud = await _unitOfWork.Repository<Davamiyyet>()
                    .MovcuddurmuAsync(x => x.IsciId == m.IsciId && x.Tarix.Date == gun.Date);
                if (movcud) continue;

                var dav = new Davamiyyet
                {
                    IsciId = m.IsciId,
                    Tarix = gun,
                    Status = davamiyyetStatusu
                };
                await _unitOfWork.Repository<Davamiyyet>().YaratAsync(dav);
            }
        }

        await _unitOfWork.YaddaSaxlaAsync();

        // Bildirişlər — HR mərhələsi son təsdiq/imtina nöqtəsidir
        if (status)
        {
            await NotifyIsciFinalApproveAsync(m);

            // Ödəniş tipinə görə Mühasibi məlumatlandır.
            if (m.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis)
            {
                await NotifyMuhasibForAdvancePaymentAsync(m);
            }
            else if (m.OdenisTipi == MezuniyyetOdenisTipi.AySonuOdenis)
            {
                await NotifyMuhasibForMonthEndPaymentAsync(m);
            }
        }
        else
        {
            await NotifyIsciProgressAsync(m, "HR", false, qeyd);
        }

        return Result.Ok("HR qərarı qeydə alındı.");
    }

    // ════════════════════════════════════════════════════════════
    // Bildiriş köməkçiləri — bütün məzuniyyət iş axını üçün
    // ════════════════════════════════════════════════════════════

    private async Task NotifyApproversForCreateAsync(Mezuniyyet m, int? departamentId)
    {
        var isciAd = await GetIsciAdAsync(m.IsciId);
        var dovr = $"{m.BaslamaTarixi:dd.MM.yyyy} – {m.BitmeTarixi:dd.MM.yyyy}";
        var bashliq = "Yeni məzuniyyət müraciəti";
        var metn = $"{isciAd} ({dovr}, {m.IsGunlerininSayi} iş günü, {m.Nov}) məzuniyyət müraciəti göndərdi.";

        switch (m.Status)
        {
            case MezuniyyetStatus.SobeReisiTesdiqinde:
                if (departamentId.HasValue)
                {
                    await _bildirisRouter.NotifyDepartmentRoleAsync(
                        departamentId.Value, StrukturRolTipi.SobeReisi,
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

    private async Task NotifyAllRehberAsync(Mezuniyyet m)
    {
        var isciAd = await GetIsciAdAsync(m.IsciId);
        var dovr = $"{m.BaslamaTarixi:dd.MM.yyyy} – {m.BitmeTarixi:dd.MM.yyyy}";
        await _bildirisRouter.NotifyRolesAsync(
            new[] { RoleNames.Rehber, RoleNames.Admin },
            BildirisNovu.MezuniyyetMuraciet,
            "Məzuniyyət müraciəti — Rəhbər təsdiqi gözləyir",
            $"{isciAd} ({dovr}) müraciəti şöbə rəisi tərəfindən təsdiqlənib, sizin təsdiqinizi gözləyir.",
            redirectUrl: $"/User/Tesdiq/MezuniyyetDetal/{m.Id}?rol=Rehber",
            mezuniyyetId: m.Id, exceptIsciId: m.IsciId);
    }

    private async Task NotifyAllHrAsync(Mezuniyyet m)
    {
        var isciAd = await GetIsciAdAsync(m.IsciId);
        var dovr = $"{m.BaslamaTarixi:dd.MM.yyyy} – {m.BitmeTarixi:dd.MM.yyyy}";
        await _bildirisRouter.NotifyRolesAsync(
            new[] { RoleNames.HR, RoleNames.Admin },
            BildirisNovu.MezuniyyetMuraciet,
            "Məzuniyyət müraciəti — HR təsdiqi gözləyir",
            $"{isciAd} ({dovr}) müraciəti rəhbər tərəfindən təsdiqlənib, son təsdiqi gözləyir.",
            redirectUrl: $"/User/Tesdiq/MezuniyyetDetal/{m.Id}?rol=Hr",
            mezuniyyetId: m.Id, exceptIsciId: m.IsciId);
    }

    private async Task NotifyIsciProgressAsync(Mezuniyyet m, string mərhələ, bool tesdiq, string? qeyd)
    {
        var dovr = $"{m.BaslamaTarixi:dd.MM.yyyy} – {m.BitmeTarixi:dd.MM.yyyy}";
        var redirectUrl = $"/User/Mezuniyyet/Detail/{m.Id}";
        if (tesdiq)
        {
            await _bildirisRouter.NotifyIsciAsync(
                m.IsciId,
                BildirisNovu.MezuniyyetTesdiq,
                $"Məzuniyyət — {mərhələ} təsdiqi alındı",
                $"{dovr} məzuniyyət müraciətiniz {mərhələ} tərəfindən təsdiqləndi.",
                redirectUrl: redirectUrl,
                mezuniyyetId: m.Id);
        }
        else
        {
            var sebeb = string.IsNullOrWhiteSpace(qeyd) ? "" : $" Səbəb: {qeyd}";
            await _bildirisRouter.NotifyIsciAsync(
                m.IsciId,
                BildirisNovu.MezuniyyetImtina,
                $"Məzuniyyət — {mərhələ} imtinası",
                $"{dovr} məzuniyyət müraciətiniz {mərhələ} tərəfindən rədd edildi.{sebeb}",
                redirectUrl: redirectUrl,
                mezuniyyetId: m.Id);
        }
    }

    private async Task NotifyIsciFinalApproveAsync(Mezuniyyet m)
    {
        var dovr = $"{m.BaslamaTarixi:dd.MM.yyyy} – {m.BitmeTarixi:dd.MM.yyyy}";
        await _bildirisRouter.NotifyIsciAsync(
            m.IsciId,
            BildirisNovu.MezuniyyetTesdiq,
            "Məzuniyyət — yekun təsdiq",
            $"{dovr} məzuniyyət müraciətiniz HR tərəfindən rəsmiləşdirildi.",
            redirectUrl: $"/User/Mezuniyyet/Detail/{m.Id}",
            mezuniyyetId: m.Id);
    }

    private async Task NotifyMuhasibForMonthEndPaymentAsync(Mezuniyyet m)
    {
        var isciAd = await GetIsciAdAsync(m.IsciId);
        var dovr = $"{m.BaslamaTarixi:dd.MM.yyyy} – {m.BitmeTarixi:dd.MM.yyyy}";
        await _bildirisRouter.NotifyRolesAsync(
            new[] { RoleNames.Muhasib, RoleNames.Admin },
            BildirisNovu.MezuniyyetOdenisGozleyir,
            "Məzuniyyət ödənişi — ay sonu maaşla",
            $"{isciAd} üçün {dovr} məzuniyyəti təsdiqlənib. Ödəniş həmin ayın maaşına əlavə olunmalıdır.",
            redirectUrl: $"/HR/MezuniyyetOdenis/Detail/{m.Id}",
            mezuniyyetId: m.Id, exceptIsciId: m.IsciId);
    }

    private async Task NotifyMuhasibForAdvancePaymentAsync(Mezuniyyet m)
    {
        var isciAd = await GetIsciAdAsync(m.IsciId);
        var dovr = $"{m.BaslamaTarixi:dd.MM.yyyy} – {m.BitmeTarixi:dd.MM.yyyy}";
        var mebleg = m.OdenenMebleg.HasValue ? $" (ilkin hesablama: {m.OdenenMebleg:N2} ₼)" : "";
        await _bildirisRouter.NotifyRolesAsync(
            new[] { RoleNames.Muhasib, RoleNames.Admin },
            BildirisNovu.TesdiqSorgusu,
            "Məzuniyyət ödənişi — qabaqcadan",
            $"{isciAd} üçün {dovr} məzuniyyət ödənişi gözləyir{mebleg}.",
            redirectUrl: $"/HR/MezuniyyetOdenis/Detail/{m.Id}",
            mezuniyyetId: m.Id, exceptIsciId: m.IsciId);
    }

    private async Task<string> GetIsciAdAsync(int isciId)
    {
        var isci = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.Id == isciId, izlemeden: true);
        return isci?.TamAd ?? $"İşçi #{isciId}";
    }

    // ============================================================
    // Bu faylı MezuniyyetService.cs-ə əlavə edin:
    // HrTesdiqAsync metodundan SONRA bu iki metodu əlavə edin.
    // ============================================================

    public async Task<Result<IList<MezuniyyetListDto>>> GetIsciMezuniyyetleriAsync(int isciId)
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
            .HamisiniGetirAsync(
                predicate: x => x.IsciId == isciId,
                include: q => q
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Departament)
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Vezife)
    .Include(m => m.EvezEdenIsci),
                izlemeden: true);

            var dtos = entities
                .OrderByDescending(x => x.BaslamaTarixi)
                .Select(m => new MezuniyyetListDto
                {
                    Id = m.Id,
                    IsciAdSoyad = m.Isci.TamAd,
                    SobeAdi = m.Isci.IsciTeyinatlari
                    .Where(t => t.Aktivdir)
                    .Select(t => t.Departament.Ad)
                    .FirstOrDefault() ?? "-",
                    VezifeAdi = m.Isci.IsciTeyinatlari
                    .Where(t => t.Aktivdir)
                    .Select(t => t.Vezife.Ad)
                    .FirstOrDefault() ?? "-",
                    EvezEdenIsciAdSoyad = m.EvezEdenIsci?.TamAd,
                    Nov = m.Nov,
                    Status = m.Status,
                    BaslamaTarixi = m.BaslamaTarixi,
                    BitmeTarixi = m.BitmeTarixi,
                    IsGunlerininSayi = m.IsGunlerininSayi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Məzuniyyətlər gətirilmədi: {ex.Message}");
        }
    }

    public async Task<Result<IList<MezuniyyetListDto>>> GetGozlemededeAsync()
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    predicate: x => x.Status == MezuniyyetStatus.Gozlemede,
                    include: q => q
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Departament)
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Vezife)
    .Include(m => m.EvezEdenIsci),
                    izlemeden: true);

            var dtos = entities
                .OrderByDescending(x => x.BaslamaTarixi)
                .Select(m => new MezuniyyetListDto
                {
                    Id = m.Id,
                    IsciAdSoyad = m.Isci.TamAd,
                    SobeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault() ?? "-",
                    VezifeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault() ?? "-",
                    EvezEdenIsciAdSoyad = m.EvezEdenIsci?.TamAd,
                    Nov = m.Nov,
                    Status = m.Status,
                    BaslamaTarixi = m.BaslamaTarixi,
                    BitmeTarixi = m.BitmeTarixi,
                    IsGunlerininSayi = m.IsGunlerininSayi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Gözləmədə məzuniyyətlər gətirilmədi: {ex.Message}");
        }
    }

    public async Task<Result<IList<MezuniyyetListDto>>> GetRehberTesdiqindeAsync()
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    predicate: x => x.Status == MezuniyyetStatus.RehberTesdiqinde,
                    include: q => q
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Departament)   // ← əlavə et
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Vezife)         // ← əlavə et
    .Include(m => m.EvezEdenIsci),
                    izlemeden: true);

            var dtos = entities
                .OrderByDescending(x => x.BaslamaTarixi)
                .Select(m => new MezuniyyetListDto
                {
                    Id = m.Id,
                    IsciAdSoyad = m.Isci.TamAd,
                    SobeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault() ?? "-",
                    VezifeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault() ?? "-",
                    EvezEdenIsciAdSoyad = m.EvezEdenIsci?.TamAd,
                    Nov = m.Nov,
                    Status = m.Status,
                    BaslamaTarixi = m.BaslamaTarixi,
                    BitmeTarixi = m.BitmeTarixi,
                    IsGunlerininSayi = m.IsGunlerininSayi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Rəhbər təsdiqindəki məzuniyyətlər gətirilmədi: {ex.Message}");
        }
    }

    public async Task<Result<IList<MezuniyyetListDto>>> GetHrTesdiqindeAsync()
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    predicate: x => x.Status == MezuniyyetStatus.HrTesdiqinde,
                    include: q => q
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Departament)
    .Include(m => m.Isci)
        .ThenInclude(i => i.IsciTeyinatlari)
            .ThenInclude(t => t.Vezife)
    .Include(m => m.EvezEdenIsci),
                    izlemeden: true);

            var dtos = entities
                .OrderByDescending(x => x.BaslamaTarixi)
                .Select(m => new MezuniyyetListDto
                {
                    Id = m.Id,
                    IsciAdSoyad = m.Isci.TamAd,
                    SobeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault() ?? "-",
                    VezifeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault() ?? "-",
                    EvezEdenIsciAdSoyad = m.EvezEdenIsci?.TamAd,
                    Nov = m.Nov,
                    Status = m.Status,
                    BaslamaTarixi = m.BaslamaTarixi,
                    BitmeTarixi = m.BitmeTarixi,
                    IsGunlerininSayi = m.IsGunlerininSayi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"HR təsdiqindəki məzuniyyətlər gətirilmədi: {ex.Message}");
        }
    }

    public async Task<Result> LegvEtAsync(int id, int isciId)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>().IdIleGetirAsync(id);

        if (m == null)
            return Result.Fail("Müraciət tapılmadı.");

        if (m.IsciId != isciId)
            return Result.Fail("Bu müraciət sizə aid deyil.");

        if (m.Status != MezuniyyetStatus.Gozlemede &&
            m.Status != MezuniyyetStatus.SobeReisiTesdiqinde &&
            m.Status != MezuniyyetStatus.RehberTesdiqinde &&
            m.Status != MezuniyyetStatus.HrTesdiqinde &&
            m.Status != MezuniyyetStatus.Tesdiqlenib)
            return Result.Fail("Bu statusda ləğv etmək mümkün deyil.");

        // Təsdiqlənmiş məzuniyyəti ləğv edəndə balansı geri qaytar
        if (m.Status == MezuniyyetStatus.Tesdiqlenib)
        {
            var balans = await _unitOfWork.Repository<MezuniyyetBalans>()
                .GetirAsync(x => x.IsciId == m.IsciId && x.Il == m.BaslamaTarixi.Year && x.Nov == m.Nov);

            if (balans != null)
            {
                balans.IstifadeOlunanGun = Math.Max(0, balans.IstifadeOlunanGun - m.IsGunlerininSayi);
                await _unitOfWork.Repository<MezuniyyetBalans>().YenileAsync(balans);
            }

            // Davamiyyətdəki icazəli/xəstəlik/ezamiyyət qeydlərini sil
            var davQeydleri = await _unitOfWork.Repository<Davamiyyet>()
                .HamisiniGetirAsync(x =>
                    x.IsciId == m.IsciId &&
                    (x.Status == DavamiyyetStatus.Icazeli ||
                     x.Status == DavamiyyetStatus.Xestelik ||
                     x.Status == DavamiyyetStatus.Ezamiyyet) &&
                    x.Tarix >= m.BaslamaTarixi &&
                    x.Tarix <= m.BitmeTarixi);

            foreach (var dav in davQeydleri)
                await _unitOfWork.Repository<Davamiyyet>().YumshakSilAsync(dav.Id);
        }

        await _unitOfWork.Repository<Mezuniyyet>().YumshakSilAsync(id);
        await _unitOfWork.YaddaSaxlaAsync();

        return Result.Ok("Müraciət ləğv edildi.");
    }
    // İşçinin departament ID-sinə görə SobeReisiTesdiqinde statuslu müraciətlər
    public async Task<Result<IList<MezuniyyetListDto>>> GetSobeyeGoreMezuniyyetlerAsync(int departamentId, int sobeReisiIsciId)
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    predicate: x => x.Status == MezuniyyetStatus.SobeReisiTesdiqinde &&
                    x.IsciId != sobeReisiIsciId &&  // ← özünü görməsin
                    x.Isci.IsciTeyinatlari
                        .Any(t => t.Aktivdir && t.DepartamentId == departamentId),
                    include: q => q
                        .Include(m => m.Isci)
                            .ThenInclude(i => i.IsciTeyinatlari)
                                .ThenInclude(t => t.Departament)
                        .Include(m => m.Isci)
                            .ThenInclude(i => i.IsciTeyinatlari)
                                .ThenInclude(t => t.Vezife)
                        .Include(m => m.EvezEdenIsci),
                    izlemeden: true);

            var dtos = entities
                .OrderByDescending(x => x.BaslamaTarixi)
                .Select(m => new MezuniyyetListDto
                {
                    Id = m.Id,
                    IsciAdSoyad = m.Isci.TamAd,
                    SobeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault() ?? "-",
                    VezifeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault() ?? "-",
                    EvezEdenIsciAdSoyad = m.EvezEdenIsci?.TamAd,
                    Nov = m.Nov,
                    Status = m.Status,
                    BaslamaTarixi = m.BaslamaTarixi,
                    BitmeTarixi = m.BitmeTarixi,
                    IsGunlerininSayi = m.IsGunlerininSayi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Xəta: {ex.Message}");
        }
    }
    public override async Task<Result<MezuniyyetDto>> IdIleGetirAsync(int id)
    {
        var entity = await _unitOfWork.Repository<Mezuniyyet>()
            .GetirAsync(
                predicate: x => x.Id == id,
                include: q => q
                    .Include(m => m.Isci)
                        .ThenInclude(i => i.IsciTeyinatlari)
                            .ThenInclude(t => t.Departament)
                    .Include(m => m.Isci)
                        .ThenInclude(i => i.IsciTeyinatlari)
                            .ThenInclude(t => t.Vezife)
                    .Include(m => m.EvezEdenIsci),
                izlemeden: true);

        if (entity == null)
            return Result<MezuniyyetDto>.Fail("Tapılmadı.");

        var dto = new MezuniyyetDto
        {
            Id = entity.Id,
            IsciId = entity.IsciId,
            IsciAdSoyad = entity.Isci.TamAd,
            SobeAdi = entity.Isci.IsciTeyinatlari
                .Where(t => t.Aktivdir)
                .Select(t => t.Departament.Ad)
                .FirstOrDefault() ?? "-",
            VezifeAdi = entity.Isci.IsciTeyinatlari
                .Where(t => t.Aktivdir)
                .Select(t => t.Vezife.Ad)
                .FirstOrDefault() ?? "-",
            EvezEdenIsciId = entity.EvezEdenIsciId,
            EvezEdenIsciAdSoyad = entity.EvezEdenIsci?.TamAd,
            Nov = entity.Nov,
            Status = entity.Status,
            BaslamaTarixi = entity.BaslamaTarixi,
            BitmeTarixi = entity.BitmeTarixi,
            IsGunlerininSayi = entity.IsGunlerininSayi,
            Qeyd = entity.Qeyd,
            ImtinaSebebi = entity.ImtinaSebebi,
            SobeReisiTesdiq = entity.SobeReisiTesdiq,
            SobeReisiTesdiqTarixi = entity.SobeReisiTesdiqTarixi,
            RehberTesdiq = entity.RehberTesdiq,
            RehberTesdiqTarixi = entity.RehberTesdiqTarixi,
            HrTesdiq = entity.HrTesdiq,
            HrTesdiqTarixi = entity.HrTesdiqTarixi,
        };

        return Result<MezuniyyetDto>.Ok(dto);
    }
    public async Task<Result<IList<MezuniyyetListDto>>> GetFiltrliAsync(
    DateTime? baslaTarixFrom,
    DateTime? baslaTarixTo,
    int? departamentId,
    int? status,
    string? axtaris)
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    predicate: x =>
                        (baslaTarixFrom == null || x.BaslamaTarixi >= baslaTarixFrom) &&
                        (baslaTarixTo == null || x.BaslamaTarixi <= baslaTarixTo) &&
                        (status == null || (int)x.Status == status) &&
                        (departamentId == null || x.Isci.IsciTeyinatlari
                            .Any(t => t.Aktivdir && t.DepartamentId == departamentId)) &&
                        (axtaris == null || x.Isci.Ad.Contains(axtaris) ||
                            x.Isci.Soyad.Contains(axtaris)),
                    include: q => q
                        .Include(m => m.Isci)
                            .ThenInclude(i => i.IsciTeyinatlari)
                                .ThenInclude(t => t.Departament)
                        .Include(m => m.Isci)
                            .ThenInclude(i => i.IsciTeyinatlari)
                                .ThenInclude(t => t.Vezife)
                        .Include(m => m.EvezEdenIsci)
                        .Include(m => m.SobeReisiIsci)   // ← navigation property
                        .Include(m => m.RehberIsci)       // ← navigation property
                        .Include(m => m.HrIsci),          // ← navigation property
                    izlemeden: true);

            var dtos = entities
                .OrderByDescending(x => x.BaslamaTarixi)
                .Select(m => new MezuniyyetListDto
                {
                    Id = m.Id,
                    IsciAdSoyad = m.Isci.TamAd,
                    SobeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault() ?? "-",
                    VezifeAdi = m.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault() ?? "-",
                    EvezEdenIsciAdSoyad = m.EvezEdenIsci?.TamAd,
                    Nov = m.Nov,
                    Status = m.Status,
                    BaslamaTarixi = m.BaslamaTarixi,
                    BitmeTarixi = m.BitmeTarixi,
                    IsGunlerininSayi = m.IsGunlerininSayi,
                    SobeReisiAdSoyad = m.SobeReisiIsci?.TamAd,
                    SobeReisiTesdiq = m.SobeReisiTesdiq,
                    SobeReisiTesdiqTarixi = m.SobeReisiTesdiqTarixi,
                    RehberAdSoyad = m.RehberIsci?.TamAd,
                    RehberTesdiq = m.RehberTesdiq,
                    RehberTesdiqTarixi = m.RehberTesdiqTarixi,
                    HrAdSoyad = m.HrIsci?.TamAd,
                    HrTesdiq = m.HrTesdiq,
                    HrTesdiqTarixi = m.HrTesdiqTarixi,
                    ImtinaSebebi = m.ImtinaSebebi,
                    YaradilmaTarixi = m.YaradilmaTarixi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Xəta: {ex.Message}");
        }
    }

    // ════════════════════════════════════════════════════════════
    // TƏSDİQ EKRANI MƏLUMATLANDIRICILARI
    // ════════════════════════════════════════════════════════════

    // Verilən müraciətin tarix aralığında həmin vaxt məzuniyyətdə olan
    // və ya olmağı planlaşdırılmış (təsdiq gözləyən) digər işçiləri gətirir.
    // Ləğv edilmiş / imtina olunmuş qeydlər nəzərə alınmır.
    // viewerRol = SobeReisi olduqda yalnız eyni şöbənin qeydləri qayıdır
    // (Şöbə Rəisi başqa şöbənin iş yükünə cavabdeh deyil).
    public async Task<Result<IList<MezuniyyetOverlapDto>>> GetOverlapMezuniyyetlerAsync(int mezuniyyetId, StrukturRolTipi? viewerRol = null)
    {
        try
        {
            var hedef = await _unitOfWork.Repository<Mezuniyyet>()
                .GetirAsync(x => x.Id == mezuniyyetId, izlemeden: true);
            if (hedef == null)
                return Result<IList<MezuniyyetOverlapDto>>.Fail("Müraciət tapılmadı.");

            // Müraciət edənin aktiv şöbəsini öyrən (EyniSobe işarəsi + SobeReisi filtri üçün)
            var hedefTeyinat = await _unitOfWork.Repository<IsciTeyinat>()
                .GetirAsync(x => x.IsciId == hedef.IsciId && x.Aktivdir, izlemeden: true);
            int? hedefDepId = hedefTeyinat?.DepartamentId;
            bool sobeReisiFiltr = viewerRol == StrukturRolTipi.SobeReisi && hedefDepId.HasValue;

            var aktivStatuslar = new[]
            {
                MezuniyyetStatus.SobeReisiTesdiqinde,
                MezuniyyetStatus.RehberTesdiqinde,
                MezuniyyetStatus.HrTesdiqinde,
                MezuniyyetStatus.Tesdiqlenib
            };

            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    predicate: x =>
                        x.Id != mezuniyyetId &&
                        x.IsciId != hedef.IsciId &&
                        !x.Silinib &&
                        aktivStatuslar.Contains(x.Status) &&
                        x.BaslamaTarixi <= hedef.BitmeTarixi &&
                        x.BitmeTarixi >= hedef.BaslamaTarixi &&
                        (!sobeReisiFiltr || x.Isci.IsciTeyinatlari
                            .Any(t => t.Aktivdir && t.DepartamentId == hedefDepId)),
                    include: q => q
                        .Include(m => m.Isci)
                            .ThenInclude(i => i.IsciTeyinatlari)
                                .ThenInclude(t => t.Departament)
                        .Include(m => m.Isci)
                            .ThenInclude(i => i.IsciTeyinatlari)
                                .ThenInclude(t => t.Vezife),
                    izlemeden: true);

            var dtos = entities
                .OrderBy(x => x.BaslamaTarixi)
                .Select(m =>
                {
                    var teyinat = m.Isci.IsciTeyinatlari.FirstOrDefault(t => t.Aktivdir);
                    return new MezuniyyetOverlapDto
                    {
                        Id = m.Id,
                        IsciId = m.IsciId,
                        IsciAdSoyad = m.Isci.TamAd,
                        SobeAdi = teyinat?.Departament?.Ad ?? "-",
                        VezifeAdi = teyinat?.Vezife?.Ad ?? "-",
                        Nov = m.Nov,
                        Status = m.Status,
                        BaslamaTarixi = m.BaslamaTarixi,
                        BitmeTarixi = m.BitmeTarixi,
                        IsGunlerininSayi = m.IsGunlerininSayi,
                        EyniSobe = hedefDepId.HasValue
                                   && teyinat != null
                                   && teyinat.DepartamentId == hedefDepId.Value
                    };
                })
                .ToList();

            return Result<IList<MezuniyyetOverlapDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetOverlapDto>>.Fail($"Xəta: {ex.Message}");
        }
    }

    // Müraciət edən işçinin eyni tarix aralığında başqa işçi üçün qəbul
    // edilmiş əvəzedici olub-olmadığını yoxlayır. Soft-warn — yalnız xəbərdarlıq.
    public async Task<Result<IList<EvezediciKonfliktDto>>> GetEvezediciKonfliktiAsync(int mezuniyyetId)
    {
        try
        {
            var hedef = await _unitOfWork.Repository<Mezuniyyet>()
                .GetirAsync(x => x.Id == mezuniyyetId, izlemeden: true);
            if (hedef == null)
                return Result<IList<EvezediciKonfliktDto>>.Fail("Müraciət tapılmadı.");

            // Müraciət edən işçi üçün QƏBUL edilmiş əvəzedici sorğuları
            var qebullar = await _unitOfWork.Repository<FinNex.Domain.Entities.Communication.EvezediciTesdiq>()
                .HamisiniGetirAsync(
                    predicate: x => !x.Silinib
                                 && x.EvezediciIsciId == hedef.IsciId
                                 && x.Status == FinNex.Domain.Entities.Communication.EvezediciTesdiqStatus.Qebul
                                 && x.Mezuniyyet != null
                                 && !x.Mezuniyyet.Silinib
                                 && x.MezuniyyetId != mezuniyyetId
                                 && x.Mezuniyyet.Status != MezuniyyetStatus.ImtinaEdildi
                                 && x.Mezuniyyet.Status != MezuniyyetStatus.LegvEdildi
                                 && x.Mezuniyyet.BaslamaTarixi <= hedef.BitmeTarixi
                                 && x.Mezuniyyet.BitmeTarixi >= hedef.BaslamaTarixi,
                    include: q => q.Include(e => e.Mezuniyyet).ThenInclude(m => m.Isci),
                    izlemeden: true);

            var dtos = qebullar
                .OrderBy(x => x.Mezuniyyet.BaslamaTarixi)
                .Select(x => new EvezediciKonfliktDto
                {
                    MezuniyyetId = x.MezuniyyetId,
                    MuracietEdenIsciId = x.Mezuniyyet.IsciId,
                    MuracietEdenIsciAdSoyad = x.Mezuniyyet.Isci?.TamAd ?? $"İşçi #{x.Mezuniyyet.IsciId}",
                    BaslamaTarixi = x.Mezuniyyet.BaslamaTarixi,
                    BitmeTarixi = x.Mezuniyyet.BitmeTarixi
                })
                .ToList();

            return Result<IList<EvezediciKonfliktDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<EvezediciKonfliktDto>>.Fail($"Xəta: {ex.Message}");
        }
    }
}