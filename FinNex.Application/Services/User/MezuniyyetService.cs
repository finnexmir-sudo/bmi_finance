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

                // 2. Balansı növə görə yoxla — BÜTÜN illərin cəmi qalığı
                // ilə müqayisə edirik (FIFO məntiqi ilə köhnə illərdən də
                // istifadə edilə bilir). Xəstəlik və Ezamiyyət limitsizdir.
                if (dto.Nov == MezuniyyetNovu.Illik)
                {
                    var umumiQaliq = await _unitOfWork.Repository<MezuniyyetBalans>().Query()
                        .Where(x => !x.Silinib && x.IsciId == dto.IsciId && x.Nov == dto.Nov)
                        .SumAsync(x => (int?)(x.ToplamGun - x.IstifadeOlunanGun)) ?? 0;

                    if (umumiQaliq < isGunu)
                        return Result<MezuniyyetDto>.Fail(
                            $"Kifayət qədər illik məzuniyyət balansı yoxdur. Bütün illərin cəmi qalıq: {umumiQaliq} gün, tələb olunur: {isGunu} gün.");
                }

                // 3. İşçinin aktiv departamentini tap
                var teyinat = await _unitOfWork.Repository<IsciTeyinat>()
                    .GetirAsync(x => x.IsciId == dto.IsciId && x.Aktivdir);

                // 4. İşçinin roluna görə status müəyyən et
                // QAYDA:
                //   • HR muraciet edir → yalnız Rəhbər təsdiqi (HR-self atlanır)
                //   • Rəhbər muraciet edir → birbaşa HR
                //   • Şöbə rəisi muraciet edir → Rəhbər → HR
                //   • Adi işçi:
                //       - şöbədə aktiv (məzuniyyətdə olmayan) şöbə rəisi varsa → SobeReisi → Rəhbər → HR
                //       - yoxdursa və ya məzuniyyətdədirsə → Rəhbər → HR
                MezuniyyetStatus ilkinStatus;

                if (dto.MuracietSahibiHrdirmi)
                {
                    // HR-ın muraciəti yalnız Rəhbər təsdiqindən keçir
                    ilkinStatus = MezuniyyetStatus.RehberTesdiqinde;
                }
                else if (dto.MuracietSahibiRehberdirmi)
                {
                    ilkinStatus = MezuniyyetStatus.HrTesdiqinde;
                }
                else if (dto.MuracietSahibiSobeReisidirmi)
                {
                    ilkinStatus = MezuniyyetStatus.RehberTesdiqinde;
                }
                else
                {
                    bool sobeReisiAvailable = false;
                    if (teyinat != null)
                    {
                        var sobeReisi = await _unitOfWork.Repository<IsciStrukturRolu>()
                            .GetirAsync(x =>
                                x.DepartamentId == teyinat.DepartamentId &&
                                x.RolTipi == StrukturRolTipi.SobeReisi &&
                                x.IsciId != dto.IsciId &&
                                x.Aktivdir);

                        if (sobeReisi != null)
                        {
                            // Şöbə rəisi bu gün təsdiqlənmiş məzuniyyətdədirsə step atlansın
                            var bugun = DateTime.Today;
                            var mezuniyyetdedir = await _unitOfWork.Repository<Mezuniyyet>()
                                .MovcuddurmuAsync(m =>
                                    m.IsciId == sobeReisi.IsciId &&
                                    m.Status == MezuniyyetStatus.Tesdiqlenib &&
                                    m.BaslamaTarixi <= bugun &&
                                    m.BitmeTarixi >= bugun);

                            sobeReisiAvailable = !mezuniyyetdedir;
                        }
                    }

                    ilkinStatus = sobeReisiAvailable
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

        // IDEMPOTENCY GUARD — yalnız SobeReisiTesdiqinde statusunda qərar verilə bilər
        if (m.Status != MezuniyyetStatus.SobeReisiTesdiqinde)
        {
            return Result.Fail(
                $"Bu müraciət artıq emal edilib (status: {m.Status}). Təkrar təsdiq mümkün deyil.");
        }

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

    // --- Yardımçı Alqoritm: Məzuniyyət günlərini hesablayır ---
    //
    // Qayda:
    //   - Həftəsonu (Ş/B) → SAYILIR (bank daxili qaydaya uyğun)
    //   - Bayram günləri  → yalnız MezuniyyetdeHesablanir=true olsa sayılır
    //     (default false — skip). Bu sayəsində "1 gün hesablanır, 1 yox"
    //     kimi hallar (məs. Qurban bayramı) per-day konfiqurasiya olunur.
    //   - HR təsdiq zamanı əl ilə düzəliş etmək imkanı var (override
    //     MezuniyyetService.HrTesdiqAsync-də tətbiq olunur).
    private async Task<int> HesablaIsGunuAsync(DateTime baslangic, DateTime bitis)
    {
        // Bayramlardan yalnız "hesablanmır" olanlar skip olunur
        var skipBayramlar = await _unitOfWork.Repository<BayramGunu>()
            .HamisiniGetirAsync(x => x.Tarix >= baslangic && x.Tarix <= bitis
                                  && x.Tip == GunTipi.Bayram
                                  && !x.MezuniyyetdeHesablanir);

        int count = 0;
        for (var date = baslangic; date <= bitis; date = date.AddDays(1))
        {
            if (skipBayramlar.Any(b => b.Tarix.Date == date.Date))
                continue; // hesablanmayan bayram — skip
            count++;       // qalan hər gün sayılır (həftəsonu daxil)
        }
        return count;
    }

    public async Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd, int rehberId)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>()
            .GetirAsync(x => x.Id == id); // ← izlemeden olmadan, tracking ilə

        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        // ─────────── IDEMPOTENCY GUARD ───────────
        // Yalnız RehberTesdiqinde statusunda olan müraciət rəhbər tərəfindən
        // qərarlandırıla bilər. Təkrarlı klik / iki pəncərə səhvini önləyir.
        if (m.Status != MezuniyyetStatus.RehberTesdiqinde)
        {
            return Result.Fail(
                $"Bu müraciət artıq emal edilib (status: {m.Status}). Təkrar təsdiq mümkün deyil.");
        }
        // ─────────────────────────────────────────

        m.RehberTesdiq = status;
        m.RehberId = rehberId;
        m.RehberTesdiqTarixi = DateTime.Now;

        if (!status)
        {
            m.Status = MezuniyyetStatus.ImtinaEdildi;
            m.ImtinaSebebi = qeyd;
        }
        else
        {
            // Müraciəti edən işçinin HR rolu olub-olmadığını yoxla — varsa HR step atlanır
            var muracietciHrdir = await _unitOfWork.Repository<IsciStrukturRolu>()
                .MovcuddurmuAsync(x =>
                    x.IsciId == m.IsciId &&
                    x.RolTipi == StrukturRolTipi.Hr &&
                    x.Aktivdir);

            m.Status = muracietciHrdir
                ? MezuniyyetStatus.Tesdiqlenib
                : MezuniyyetStatus.HrTesdiqinde;
        }

        await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m); // ← explicit update
        await _unitOfWork.YaddaSaxlaAsync();

        if (status)
        {
            await NotifyIsciProgressAsync(m, "Rəhbər", true, qeyd);
            if (m.Status == MezuniyyetStatus.HrTesdiqinde)
                await NotifyAllHrAsync(m);
        }
        else
        {
            await NotifyIsciProgressAsync(m, "Rəhbər", false, qeyd);
        }

        return Result.Ok("Rəhbər qərarı qeydə alındı.");
    }
    // HrTesdiqAsync metodu
    public async Task<Result> HrTesdiqAsync(int id, bool status, string? qeyd, int hrId,
                                              int? gunSayiManual = null, string? duzelisSebebi = null)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>()
            .GetirAsync(x => x.Id == id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        // ─────────────── IDEMPOTENCY GUARD ───────────────
        // Vacib qoruma: eyni müraciət iki dəfə təsdiq edilə bilməz.
        // Əks halda balans iki dəfə çıxılır (bag reported by users).
        // Yalnız HrTesdiqinde statusunda olan müraciətlər qərarlandırıla bilər.
        if (m.Status != MezuniyyetStatus.HrTesdiqinde)
        {
            var statusAdi = m.Status switch
            {
                MezuniyyetStatus.Tesdiqlenib => "təsdiqlənib",
                MezuniyyetStatus.ImtinaEdildi => "imtina edilib",
                MezuniyyetStatus.LegvEdildi => "ləğv edilib",
                MezuniyyetStatus.Gozlemede => "hələ rəhbər təsdiqindən keçməyib",
                MezuniyyetStatus.RehberTesdiqinde => "rəhbər təsdiqində gözləyir",
                MezuniyyetStatus.SobeReisiTesdiqinde => "şöbə rəisində gözləyir",
                _ => m.Status.ToString()
            };
            return Result.Fail($"Bu müraciət artıq emal edilib ({statusAdi}). Təkrar təsdiq mümkün deyil.");
        }
        // ─────────────────────────────────────────────────

        m.HrTesdiq = status;
        m.HrId = hrId;
        m.HrTesdiqTarixi = DateTime.Now;
        m.Status = status
            ? MezuniyyetStatus.Tesdiqlenib
            : MezuniyyetStatus.ImtinaEdildi;

        if (!status) m.ImtinaSebebi = qeyd;

        // HR manual gün sayı düzəlişi (təsdiq halında — ivə avtomatikdən
        // fərqli dəyər verilibsə saxlanır, balans və davamiyyət buna görə)
        if (status && gunSayiManual.HasValue && gunSayiManual.Value != m.IsGunlerininSayi)
        {
            if (gunSayiManual.Value < 0)
                return Result.Fail("Gün sayı mənfi ola bilməz.");
            m.IsGunlerininSayiManual = gunSayiManual.Value;
            m.GunHesabiDuzelisiSebebi = string.IsNullOrWhiteSpace(duzelisSebebi)
                ? "(HR tərəfindən əl ilə düzəldilib)"
                : duzelisSebebi.Trim();
        }

        // Əmr nömrəsi — yalnız HR təsdiq edərkən (imtina halında verilmir).
        // Hər il 1-dən başlayır; rəqəm hissəsi ayrıca saxlanır ki, araya
        // düşmüş əmr halları üçün sonradan suffiks ("a", "b" ...) əlavə edilsin.
        if (status && !m.EmrRegem.HasValue)
        {
            var il = m.HrTesdiqTarixi.Value.Year;
            var sonRegem = await _unitOfWork.Repository<Mezuniyyet>().Query()
                .Where(x => !x.Silinib && x.EmrIl == il && x.EmrRegem != null)
                .MaxAsync(x => (int?)x.EmrRegem) ?? 0;
            m.EmrRegem = sonRegem + 1;
            m.EmrIl = il;
            // EmrSuffiks — default null; UI-dan əl ilə verilə bilər
        }

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
            // FIFO — ən köhnə illərdən istifadə et, sonra cari ilə gəl.
            // Bu məntiq illər boyu yığılmış qalıq günlərin ilk növbədə xərclənməsini
            // təmin edir (cari ilin balansı ən sonda istifadə olunur).
            // Effektiv gün sayı: HR manual dəyər varsa onu, yoxdursa avtomatiki.
            await BalansiFifoKesAsync(m.IsciId, m.Nov, m.EfektivGunSayi);

            // Davamiyyətdə növə görə qeydlər yarat (həftəsonu da sayılır,
            // yalnız MezuniyyetdeHesablanir=false olan bayramlar skip olunur)
            var davamiyyetStatusu = m.Nov switch
            {
                MezuniyyetNovu.Xestelik => DavamiyyetStatus.Xestelik,
                MezuniyyetNovu.Ezamiyyet => DavamiyyetStatus.Ezamiyyet,
                _ => DavamiyyetStatus.Icazeli
            };

            var skipBayramlar = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x => x.Tarix >= m.BaslamaTarixi && x.Tarix <= m.BitmeTarixi
                                      && x.Tip == GunTipi.Bayram
                                      && !x.MezuniyyetdeHesablanir);

            for (var gun = m.BaslamaTarixi; gun <= m.BitmeTarixi; gun = gun.AddDays(1))
            {
                // Hesablanmayan bayramlar skip — həftəsonu artıq skip EDİLMİR
                if (skipBayramlar.Any(b => b.Tarix.Date == gun.Date))
                    continue;

                // Cari günün qeydi varmı? Varsa və "Qayıb" idisə — yeni statusa çevir.
                // Başqa status (məs. İşdə) idisə toxunma. Qeyd yoxdursa — yeni yarat.
                // Bu məntiq HR-in keçmiş tarixlər üçün yazdığı məzuniyyətləri də
                // düzgün emal edir (məs: işçi bir neçə gün gəlmədi, Qayıb yazıldı,
                // sonra sənəd gətirdi — HR təsdiq edəndə Qayıb → Xəstəlik/Ezamiyyət
                // olaraq avtomatik düzəlir).
                var mevcud = await _unitOfWork.Repository<Davamiyyet>()
                    .GetirAsync(x => x.IsciId == m.IsciId && x.Tarix.Date == gun.Date);

                if (mevcud != null)
                {
                    if (mevcud.Status == DavamiyyetStatus.Qayib)
                    {
                        mevcud.Status = davamiyyetStatusu;
                        await _unitOfWork.Repository<Davamiyyet>().YenileAsync(mevcud);
                    }
                    continue;
                }

                await _unitOfWork.Repository<Davamiyyet>().YaratAsync(new Davamiyyet
                {
                    IsciId = m.IsciId,
                    Tarix = gun,
                    Status = davamiyyetStatusu
                });
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
                    EmrRegem = m.EmrRegem,
                    EmrSuffiks = m.EmrSuffiks,
                    EmrIl = m.EmrIl,
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
                    EmrRegem = m.EmrRegem,
                    EmrSuffiks = m.EmrSuffiks,
                    EmrIl = m.EmrIl,
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
                    EmrRegem = m.EmrRegem,
                    EmrSuffiks = m.EmrSuffiks,
                    EmrIl = m.EmrIl,
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
                    EmrRegem = m.EmrRegem,
                    EmrSuffiks = m.EmrSuffiks,
                    EmrIl = m.EmrIl,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"HR təsdiqindəki məzuniyyətlər gətirilmədi: {ex.Message}");
        }
    }

    // HR izləmə paneli üçün — hələ şöbə rəisi/rəhbər təsdiqində dayanan
    // müraciətlər (HR-a hələ çatmayanlar). HR bunları read-only görür ki,
    // pipeline-i izləyə və gecikən qeydləri fərq edə bilsin.
    public async Task<Result<IList<MezuniyyetListDto>>> GetProsesdeOlanlarAsync()
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    predicate: x => x.Status == MezuniyyetStatus.SobeReisiTesdiqinde
                                 || x.Status == MezuniyyetStatus.RehberTesdiqinde,
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
                .OrderBy(x => x.YaradilmaTarixi) // ən qədim əvvəl — gecikənləri tez fərq et
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
                    EmrRegem = m.EmrRegem,
                    EmrSuffiks = m.EmrSuffiks,
                    EmrIl = m.EmrIl,
                    YaradilmaTarixi = m.YaradilmaTarixi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Prosesdə olan müraciətlər gətirilmədi: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════
    // HR Tarixçə — təsdiqlənmiş/imtina edilmiş müraciətlər
    // Axtarış: işçi adı, soyadı və ya FIN ilə
    // ══════════════════════════════════════════════════════
    public async Task<Result<IList<MezuniyyetListDto>>> GetTarixceAsync(string? axtaris = null)
    {
        try
        {
            var q = _unitOfWork.Repository<Mezuniyyet>().Query()
                .Where(x => !x.Silinib
                         && (x.Status == MezuniyyetStatus.Tesdiqlenib
                          || x.Status == MezuniyyetStatus.ImtinaEdildi));

            if (!string.IsNullOrWhiteSpace(axtaris))
            {
                var t = axtaris.Trim();
                q = q.Where(x => x.Isci.Ad.Contains(t)
                              || x.Isci.Soyad.Contains(t)
                              || (x.Isci.FIN != null && x.Isci.FIN.Contains(t)));
            }

            var entities = await q
                .Include(m => m.Isci).ThenInclude(i => i.IsciTeyinatlari).ThenInclude(t => t.Departament)
                .Include(m => m.Isci).ThenInclude(i => i.IsciTeyinatlari).ThenInclude(t => t.Vezife)
                .Include(m => m.EvezEdenIsci)
                .OrderByDescending(m => m.HrTesdiqTarixi ?? m.YaradilmaTarixi)
                .ToListAsync();

            var dtos = entities.Select(m => new MezuniyyetListDto
            {
                Id = m.Id,
                IsciAdSoyad = m.Isci.TamAd,
                SobeAdi = m.Isci.IsciTeyinatlari.Where(t => t.Aktivdir).Select(t => t.Departament.Ad).FirstOrDefault() ?? "-",
                VezifeAdi = m.Isci.IsciTeyinatlari.Where(t => t.Aktivdir).Select(t => t.Vezife.Ad).FirstOrDefault() ?? "-",
                EvezEdenIsciAdSoyad = m.EvezEdenIsci?.TamAd,
                Nov = m.Nov,
                Status = m.Status,
                BaslamaTarixi = m.BaslamaTarixi,
                BitmeTarixi = m.BitmeTarixi,
                IsGunlerininSayi = m.IsGunlerininSayi,
                EmrRegem = m.EmrRegem,
                EmrSuffiks = m.EmrSuffiks,
                EmrIl = m.EmrIl,
                HrTesdiq = m.HrTesdiq,
                HrTesdiqTarixi = m.HrTesdiqTarixi,
                ImtinaSebebi = m.ImtinaSebebi,
                YaradilmaTarixi = m.YaradilmaTarixi,
            }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Tarixçə gətirilmədi: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════
    // Aktiv və yaxınlaşan məzuniyyətlər (HR izləmə səhifəsi)
    // ══════════════════════════════════════════════════════
    public async Task<Result<IList<MezuniyyetListDto>>> GetAktivVeYaxinlardakilarAsync(int qabaqcaGun = 30)
    {
        try
        {
            var bugun = DateTime.Today;
            var sonTarix = bugun.AddDays(qabaqcaGun);

            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    // Yalnız təsdiqlənmişlər + (məzuniyyətdə olan VƏ YA yaxınlarda başlayacaq)
                    predicate: x => x.Status == MezuniyyetStatus.Tesdiqlenib
                                 && x.BaslamaTarixi <= sonTarix
                                 && x.BitmeTarixi >= bugun,
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
                .OrderBy(x => x.BaslamaTarixi)
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
                    EmrRegem = m.EmrRegem,
                    EmrSuffiks = m.EmrSuffiks,
                    EmrIl = m.EmrIl,
                    YaradilmaTarixi = m.YaradilmaTarixi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Aktiv məzuniyyətlər gətirilmədi: {ex.Message}");
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
                    EmrRegem = m.EmrRegem,
                    EmrSuffiks = m.EmrSuffiks,
                    EmrIl = m.EmrIl,
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
            EmrRegem = entity.EmrRegem,
            EmrSuffiks = entity.EmrSuffiks,
            EmrIl = entity.EmrIl,
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
                    EmrRegem = m.EmrRegem,
                    EmrSuffiks = m.EmrSuffiks,
                    EmrIl = m.EmrIl,
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

    // ════════════════════════════════════════════════════════════
    // FIFO balans kəsmə — ən köhnə illərdən sırayla istifadə et
    //
    // Məntiq: əgər işçinin 2024 (5 qalıq), 2025 (10 qalıq), 2026 (21 qalıq)
    // balansı var və 8 gün məzuniyyət yazılırsa:
    //   → 2024-dən 5 gün (hamısı istifadə olunur)
    //   → 2025-dən 3 gün (qalan 7 saxlanır)
    //   → 2026-ya toxunulmur
    //
    // Əvvəlki sadə "cari ildən kəs" məntiqi köhnə illərdən qalan günləri
    // itirməyə səbəb olurdu — köhnə illərin qalığı heç vaxt istifadə
    // edilmirdi.
    // ════════════════════════════════════════════════════════════
    private async Task BalansiFifoKesAsync(int isciId, MezuniyyetNovu nov, int gunu)
    {
        if (gunu <= 0) return;

        var repo = _unitOfWork.Repository<MezuniyyetBalans>();

        // Bütün aktiv balansları il artma sırası ilə al (ən köhnə əvvəl)
        var balanslar = await repo.Query()
            .Where(b => !b.Silinib && b.IsciId == isciId && b.Nov == nov
                     && (b.ToplamGun - b.IstifadeOlunanGun) > 0)
            .OrderBy(b => b.Il)
            .ToListAsync();

        int qalanTelebatGun = gunu;
        foreach (var b in balanslar)
        {
            if (qalanTelebatGun <= 0) break;
            int ilinQaligi = b.ToplamGun - b.IstifadeOlunanGun;
            int kesilecek = Math.Min(ilinQaligi, qalanTelebatGun);

            b.IstifadeOlunanGun += kesilecek;
            b.YenilenmeTarixi = DateTime.Now;
            await repo.YenileAsync(b);

            qalanTelebatGun -= kesilecek;
        }

        // Qalan tələbat var amma balans bitibsə — cari ilin balansına borc
        // kimi əlavə olunur (ToplamGun aşılır, İstifadə ToplamGun-dan çox olur).
        // Bu vəziyyət nadirdir, validasiya (balans.QaliqGun >= isGunu) ilk
        // yoxlanışdan keçməlidir. Amma edge case üçün cari ili hədəfləyirik.
        if (qalanTelebatGun > 0)
        {
            var cariIl = DateTime.Now.Year;
            var cariBalans = await repo.GetirAsync(x =>
                x.IsciId == isciId && x.Nov == nov && x.Il == cariIl && !x.Silinib);
            if (cariBalans != null)
            {
                cariBalans.IstifadeOlunanGun += qalanTelebatGun;
                cariBalans.YenilenmeTarixi = DateTime.Now;
                await repo.YenileAsync(cariBalans);
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    // GERİYƏ QEYD — HR tərəfindən keçmiş tarixlərə məzuniyyət
    // ════════════════════════════════════════════════════════════
    private const int GeriyeQeydMaxGun = 90;

    public async Task<Result<MezuniyyetDto>> GeriyeQeydEtAsync(GeriyeMezuniyyetCreateDto dto, int hrIsciId)
    {
        using (var transaction = await _unitOfWork.BeginTransactionAsync())
        {
            try
            {
                // ── 1. Tarix validasiyası ──────────────────────────────
                // HR birbaşa qeyd (həm keçmiş, həm gələcək tarix üçün) — təsdiq axını
                // atlanır, müraciət dərhal "Təsdiqlənib" statusunda yaranır. Keçmiş
                // tarixlər üçün Davamiyyətdə Qayıb qeydləri avtomatik çevrilir.
                if (dto.BitmeTarixi < dto.BaslamaTarixi)
                    return Result<MezuniyyetDto>.Fail("Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");

                var bugun = DateTime.Today;
                var minTarix = bugun.AddDays(-GeriyeQeydMaxGun);
                if (dto.BaslamaTarixi.Date < minTarix)
                    return Result<MezuniyyetDto>.Fail($"Keçmiş tarixlər üçün maksimum {GeriyeQeydMaxGun} gün əvvələ qədər qeyd etmək olar.");

                // ── 2. Eyni tarix aralığında mövcud məzuniyyət yoxlanışı ──
                var aktivStatuslar = new[]
                {
                    MezuniyyetStatus.SobeReisiTesdiqinde,
                    MezuniyyetStatus.RehberTesdiqinde,
                    MezuniyyetStatus.HrTesdiqinde,
                    MezuniyyetStatus.Tesdiqlenib,
                    MezuniyyetStatus.Gozlemede
                };

                var konflikt = await _unitOfWork.Repository<Mezuniyyet>()
                    .MovcuddurmuAsync(x =>
                        !x.Silinib &&
                        x.IsciId == dto.IsciId &&
                        aktivStatuslar.Contains(x.Status) &&
                        x.BaslamaTarixi <= dto.BitmeTarixi &&
                        x.BitmeTarixi >= dto.BaslamaTarixi);

                if (konflikt)
                    return Result<MezuniyyetDto>.Fail("Bu tarix aralığında işçinin artıq məzuniyyəti mövcuddur.");

                // ── 3. İş günlərinin sayı ──────────────────────────────
                int isGunu = await HesablaIsGunuAsync(dto.BaslamaTarixi, dto.BitmeTarixi);
                if (isGunu <= 0)
                    return Result<MezuniyyetDto>.Fail("Seçilən aralıqda iş günü yoxdur (yalnız həftəsonu/bayram).");

                // ── 4. Balans yoxlanışı — bütün illərin cəmi qalığı ────
                if (dto.Nov == MezuniyyetNovu.Illik)
                {
                    var umumiQaliq = await _unitOfWork.Repository<MezuniyyetBalans>().Query()
                        .Where(x => !x.Silinib && x.IsciId == dto.IsciId && x.Nov == dto.Nov)
                        .SumAsync(x => (int?)(x.ToplamGun - x.IstifadeOlunanGun)) ?? 0;

                    if (umumiQaliq < isGunu)
                        return Result<MezuniyyetDto>.Fail(
                            $"İllik məzuniyyət balansı kifayət etmir. Bütün illərin cəmi qalıq: {umumiQaliq} gün, tələb olunur: {isGunu} gün.");
                }

                // ── 5. Entity yarat — bütün addımlar HR tərəfindən təsdiqli ──
                var indi = DateTime.Now;
                var sebebTrim = string.IsNullOrWhiteSpace(dto.Sebeb) ? null : dto.Sebeb.Trim();
                var qeydMetni = sebebTrim == null
                    ? "[Geriyə qeyd — HR]"
                    : $"[Geriyə qeyd — HR] {sebebTrim}";
                var entity = new Mezuniyyet
                {
                    IsciId = dto.IsciId,
                    Nov = dto.Nov,
                    BaslamaTarixi = dto.BaslamaTarixi,
                    BitmeTarixi = dto.BitmeTarixi,
                    IsGunlerininSayi = isGunu,
                    Qeyd = qeydMetni,
                    Status = MezuniyyetStatus.Tesdiqlenib,
                    OdenisTipi = MezuniyyetOdenisTipi.AySonuOdenis,
                    OdenisStatus = MezuniyyetOdenisStatus.TetbiqEdilmir,
                    // Geriyə qeyd — təsdiq axını keçmir; yalnız HR addımı rəsmi
                    // olaraq "rəsmiləşdirən" kimi qeyd olunur (audit).
                    // SobeReisi/Rəhbər boş qalır, çünki onlar həqiqətən təsdiq
                    // etməyiblər — fake audit yaratmayaq.
                    HrTesdiq = true,
                    HrId = hrIsciId,
                    HrTesdiqTarixi = indi
                };

                await _unitOfWork.Repository<Mezuniyyet>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // ── 6. Balansı yenilə (FIFO — ən köhnə illərdən sırayla kəs) ──
                if (dto.Nov == MezuniyyetNovu.Illik)
                {
                    await BalansiFifoKesAsync(dto.IsciId, dto.Nov, isGunu);
                }

                // ── 7. Davamiyyət: Qayib → İcazəli/Xəstəlik/Ezamiyyət ──
                var yeniStatus = dto.Nov switch
                {
                    MezuniyyetNovu.Xestelik  => DavamiyyetStatus.Xestelik,
                    MezuniyyetNovu.Ezamiyyet => DavamiyyetStatus.Ezamiyyet,
                    _ => DavamiyyetStatus.Icazeli
                };

                var skipBayramlar = await _unitOfWork.Repository<BayramGunu>()
                    .HamisiniGetirAsync(x => x.Tarix >= dto.BaslamaTarixi && x.Tarix <= dto.BitmeTarixi
                                          && x.Tip == GunTipi.Bayram
                                          && !x.MezuniyyetdeHesablanir);

                int duzeldilenQaib = 0, yeniQeyd = 0;
                for (var gun = dto.BaslamaTarixi.Date; gun <= dto.BitmeTarixi.Date; gun = gun.AddDays(1))
                {
                    // Həftəsonu artıq skip edilmir — yeni qayda
                    if (skipBayramlar.Any(b => b.Tarix.Date == gun.Date)) continue;

                    var mevcud = await _unitOfWork.Repository<Davamiyyet>()
                        .GetirAsync(x => x.IsciId == dto.IsciId && x.Tarix.Date == gun.Date);

                    if (mevcud != null)
                    {
                        // Qayib idisə icazəliyə çevir — digər statuslara toxunma
                        if (mevcud.Status == DavamiyyetStatus.Qayib)
                        {
                            mevcud.Status = yeniStatus;
                            await _unitOfWork.Repository<Davamiyyet>().YenileAsync(mevcud);
                            duzeldilenQaib++;
                        }
                    }
                    else
                    {
                        await _unitOfWork.Repository<Davamiyyet>().YaratAsync(new Davamiyyet
                        {
                            IsciId = dto.IsciId,
                            Tarix = gun,
                            Status = yeniStatus
                        });
                        yeniQeyd++;
                    }
                }

                await _unitOfWork.YaddaSaxlaAsync();
                await transaction.CommitAsync();

                // ── 8. Bildirişlər ─────────────────────────────────────
                var isciAd = await GetIsciAdAsync(dto.IsciId);
                var dovr = $"{dto.BaslamaTarixi:dd.MM.yyyy} – {dto.BitmeTarixi:dd.MM.yyyy}";

                // İşçiyə
                var sebebMetn = sebebTrim == null ? "" : $" Səbəb: {sebebTrim}";
                await _bildirisRouter.NotifyIsciAsync(
                    dto.IsciId,
                    BildirisNovu.MezuniyyetTesdiq,
                    "HR geriyə məzuniyyət qeyd etdi",
                    $"HR sizin üçün {dovr} ({isGunu} iş günü, {dto.Nov}) məzuniyyəti qeyd etdi.{sebebMetn}",
                    redirectUrl: $"/User/Mezuniyyet/Detail/{entity.Id}",
                    mezuniyyetId: entity.Id);

                // Mühasibə (ay sonu maaşa təsir edə bilər)
                await _bildirisRouter.NotifyRolesAsync(
                    new[] { RoleNames.Muhasib, RoleNames.Admin },
                    BildirisNovu.MezuniyyetOdenisGozleyir,
                    "Geriyə qeyd edilmiş məzuniyyət",
                    $"HR {isciAd} üçün {dovr} ({isGunu} iş günü) məzuniyyəti geriyə qeyd etdi. Ay-sonu maaş hesablamasında nəzərə alın.",
                    redirectUrl: $"/HR/Mezuniyyet/Detal/{entity.Id}",
                    mezuniyyetId: entity.Id, exceptIsciId: dto.IsciId);

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

                var mesaj = $"Geriyə qeyd uğurla rəsmiləşdirildi ({isGunu} iş günü). " +
                           $"Davamiyyət: {duzeldilenQaib} qayib → icazəli, {yeniQeyd} yeni qeyd yaradıldı.";
                return Result<MezuniyyetDto>.Ok(resultDto, mesaj);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<MezuniyyetDto>.Fail($"Xəta baş verdi: {ex.Message}");
            }
        }
    }
}