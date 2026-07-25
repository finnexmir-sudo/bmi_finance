using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.HR;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Application.Services;
using FinNex.Application.Services.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class MezuniyyetService : ServiceAsync<Mezuniyyet, MezuniyyetDto, MezuniyyetCreateDto, MezuniyyetUpdateDto>, IMezuniyyetService
{
    private readonly IEvezediciTesdiqService _evezediciTesdiqService;
    private readonly IMaasHesablamaService _maasHesablamaService;
    private readonly IBildirisRouter _bildirisRouter;
    private readonly IJetonService _jetonService;
    private readonly IEmrService _emrService;
    private readonly UserManager<AppUser> _userManager;

    public MezuniyyetService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IEvezediciTesdiqService evezediciTesdiqService,
        IMaasHesablamaService maasHesablamaService,
        IBildirisRouter bildirisRouter,
        IJetonService jetonService,
        IEmrService emrService,
        UserManager<AppUser> userManager)
        : base(unitOfWork, mapper)
    {
        _evezediciTesdiqService = evezediciTesdiqService;
        _maasHesablamaService = maasHesablamaService;
        _bildirisRouter = bildirisRouter;
        _jetonService = jetonService;
        _emrService = emrService;
        _userManager = userManager;
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
                            $"Kifayət qədər əmək məzuniyyəti balansı yoxdur. Bütün illərin cəmi qalıq: {umumiQaliq} gün, tələb olunur: {isGunu} gün.");
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
                            // Şöbə rəisi bu gün təsdiqlənmiş məzuniyyətdədirsə step atlansın.
                            // .Date — tarixlər saat komponenti daşıya bilir (kod bazasının konvensiyası);
                            // .Date-siz müqayisə məzuniyyətin başladığı gündə uğursuz olur.
                            var bugun = DateTime.Today;
                            var mezuniyyetdedir = await _unitOfWork.Repository<Mezuniyyet>()
                                .MovcuddurmuAsync(m =>
                                    m.IsciId == sobeReisi.IsciId &&
                                    !m.Silinib &&
                                    m.Status == MezuniyyetStatus.Tesdiqlenib &&
                                    m.BaslamaTarixi.Date <= bugun &&
                                    m.BitmeTarixi.Date >= bugun);

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

        // ── İMTİNA ──────────────────────────────────────────────
        if (!status)
        {
            m.Status = MezuniyyetStatus.ImtinaEdildi;
            m.ImtinaSebebi = qeyd;
            await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m);
            await _unitOfWork.YaddaSaxlaAsync();
            await NotifyIsciProgressAsync(m, "Rəhbər", false, qeyd);
            return Result.Ok("Rəhbər qərarı qeydə alındı.");
        }

        // ── TƏSDİQ ──────────────────────────────────────────────
        // Növbəti mərhələ HR-dır. LAKİN müraciəti edən özü HR-dırsa HR mərhələsi
        // atlanır (öz müraciətini təsdiqləməsin). HR təyini bütün sistemlə EYNİ
        // mənbədən — Identity rolu (RoleNames.HR) — götürülür. (Əvvəl IsciStrukturRolu
        // yoxlanırdı; Identity-HR olan, amma struktur qeydi olmayan işçidə atlama
        // işləmir, müraciət HR mərhələsində ilişir və HR-a bildiriş getmirdi.)
        var muracietciAppUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.IsciId == m.IsciId);
        var muracietciHrdir = muracietciAppUser != null
            && await _userManager.IsInRoleAsync(muracietciAppUser, RoleNames.HR);

        m.Status = MezuniyyetStatus.HrTesdiqinde;
        await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m);
        await _unitOfWork.YaddaSaxlaAsync();

        await NotifyIsciProgressAsync(m, "Rəhbər", true, qeyd);

        if (muracietciHrdir)
        {
            // HR müraciətçidir → HR mərhələsi atlanır və dərhal YEKUN rəsmiləşdirilir.
            // Finalizasiya (əmr, balans, davamiyyət, ödəniş bildirişi) TAM getsin deyə
            // HrTesdiqAsync-ə deleqasiya olunur — inline "Təsdiqlənib" qoymaq bu addımları
            // buraxardı və yarımçıq təsdiq yaradardı (əmrsiz, davamiyyətsiz).
            // verenId = müraciətçinin özü (o, HR-dır; məzuniyyət əmrini HR verir).
            return await HrTesdiqAsync(m.Id, true, qeyd, m.IsciId);
        }

        // Adi işçi — HR mərhələsində qalır, HR-lara bildiriş gedir.
        await NotifyAllHrAsync(m);
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
        // Mərkəzi əmr reyestrindən (IEmrService) alınır: Emr cədvəlinə sətir yazılır,
        // nömrə isə (Nov=Mezuniyyet, il) sayğacından gəlir. HR son nömrəni əl ilə
        // təyin edibsə, generasiya oradan davam edir. Göstərmək üçün EmrRegem/EmrIl
        // məzuniyyətdə də saxlanır (köhnə format/UI toxunulmaz qalır).
        // JETON ilə məzuniyyət əmr nömrəsi ALMIR (istək 2026-07) — hazırda jeton
        // məzuniyyəti yalnız HR "Geriyə qeyd" axını ilə yaranır, amma bu yol da
        // jeton müraciəti alsa nömrə verməsin deyə burada da qoruyucu şərt.
        if (status && !m.EmrRegem.HasValue && !m.JetonIleOdendi)
        {
            var emr = await _emrService.YeniEmrAsync(
                EmrNovu.Mezuniyyet,
                elaqeliEntityId: m.Id,
                tarix: m.HrTesdiqTarixi,
                metn: $"Məzuniyyət əmri — İşçi #{m.IsciId} ({m.BaslamaTarixi:dd.MM.yyyy}–{m.BitmeTarixi:dd.MM.yyyy})",
                verenIsciId: hrId);
            m.EmrRegem = emr.Nomre;
            m.EmrIl    = emr.Il;
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
                MezuniyyetNovu.OzHesabina => DavamiyyetStatus.OdenissizMezuniyyet,
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

            // Üst-üstə düşən təsdiqlənmiş İcazələri ləğv et (gün məzuniyyətdir → İcazə artıqdır)
            await _OrtulenIcazeleriLegvEtAsync(m.IsciId, m.BaslamaTarixi, m.BitmeTarixi);
        }

        await _unitOfWork.YaddaSaxlaAsync();

        // Bildirişlər — HR mərhələsi son təsdiq/imtina nöqtəsidir
        if (status)
        {
            await NotifyIsciFinalApproveAsync(m);

            // Ödəniş tipinə görə Mühasibi məlumatlandır — YALNIZ ödənişli məzuniyyətdə.
            // Öz hesabına (ödənişsiz, Ə.M. 129) məzuniyyət haqqı YOXDUR → Mühasibə
            // ödəniş bildirişi getməməlidir (real ödəniş 0-dır, yanıldıcı olardı).
            if (m.Nov != MezuniyyetNovu.OzHesabina)
            {
                if (m.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis)
                {
                    await NotifyMuhasibForAdvancePaymentAsync(m);
                }
                else if (m.OdenisTipi == MezuniyyetOdenisTipi.AySonuOdenis)
                {
                    await NotifyMuhasibForMonthEndPaymentAsync(m);
                }
            }
        }
        else
        {
            await NotifyIsciProgressAsync(m, "HR", false, qeyd);
        }

        return Result.Ok("HR qərarı qeydə alındı.");
    }

    // ════════════════════════════════════════════════════════════
    // HR DÜZƏLİŞ (təsdiqdən sonra, başlanğıc keçməyib)
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// HR təsdiqlənmiş məzuniyyətin ödəniş tipini dəyişir (ay-sonu ↔ qabaqcadan).
    /// Şərtlər: Status=Təsdiqlənib, başlanğıc keçməyib, avans hələ icra olunmayıb
    /// (Odenilib/PlanliOdenis → bloklanır). Qabaqcadana keçəndə məbləğ hesablanır,
    /// status Gözləyir olur, Mühasibə bildiriş gedir. Maaş hesablaması data-əsaslı
    /// olduğu üçün (OdenisTipi-yə görə) avtomatik uyğunlaşır.
    /// </summary>
    public async Task<Result> HrOdenisTipiDeyisAsync(int id, MezuniyyetOdenisTipi yeniTipi, int hrId)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>().GetirAsync(x => x.Id == id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        if (m.Status != MezuniyyetStatus.Tesdiqlenib)
            return Result.Fail("Yalnız təsdiqlənmiş məzuniyyətdə ödəniş tipi dəyişdirilə bilər.");

        if (m.BaslamaTarixi.Date <= DateTime.Today)
            return Result.Fail("Məzuniyyət başlayıb (və ya bu gün başlayır) — ödəniş tipi dəyişdirilə bilməz.");

        if (m.OdenisStatus == MezuniyyetOdenisStatus.Odenilib ||
            m.OdenisStatus == MezuniyyetOdenisStatus.PlanliOdenis)
            return Result.Fail("Qabaqcadan ödəniş artıq icra olunub/planlanıb — dəyişiklik mümkün deyil. (Pulun geri-alınması əl ilə həll olunmalıdır.)");

        if (m.OdenisTipi == yeniTipi)
            return Result.Fail("Ödəniş tipi onsuz da bu tipdədir.");

        m.OdenisTipi = yeniTipi;

        if (yeniTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis)
        {
            // Avansa keçid — HrTesdiqAsync ilə eyni məntiq
            var hesab = await _maasHesablamaService
                .MezuniyyetOdenisiDetalliHesablaAsync(m.IsciId, m.BaslamaTarixi, m.BitmeTarixi);
            m.OdenenMebleg = hesab.CemiOdenis;
            m.OdenisStatus = MezuniyyetOdenisStatus.Gozleyir;
        }
        else // AySonuOdenis
        {
            // Ay-sonuna qayıdış — avans qeydləri sıfırlanır (ödəniş hələ edilməyib)
            m.OdenenMebleg = null;
            m.OdenenMeblegBrut = null;
            m.OdenisStatus = MezuniyyetOdenisStatus.TetbiqEdilmir;
            m.PlanliOdenisTarixi = null;
        }

        await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m);
        await _unitOfWork.YaddaSaxlaAsync();

        // Mühasibə bildiriş (yeni tipə görə)
        if (yeniTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis)
            await NotifyMuhasibForAdvancePaymentAsync(m);
        else
            await NotifyMuhasibForMonthEndPaymentAsync(m);

        return Result.Ok(yeniTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis
            ? "Ödəniş tipi 'Qabaqcadan ödəniş'-ə dəyişdirildi. Mühasibə bildiriş göndərildi."
            : "Ödəniş tipi 'Ay sonu ödəniş'-ə dəyişdirildi.");
    }

    /// <summary>
    /// HR təsdiqlənmiş məzuniyyəti ləğv edir (başlanğıc keçməyib).
    /// Şərtlər: Status=Təsdiqlənib, başlanğıc keçməyib, avans icra olunmayıb
    /// (Odenilib/PlanliOdenis → bloklanır). Balans LIFO ilə geri qaytarılır,
    /// davamiyyət qeydləri silinir, yalnız gözləyən (Gozleyir) avans sıfırlanır
    /// və Mühasibə ləğv bildirişi gedir. Ləğv səbəbi audit üçün saxlanır,
    /// qeyd soft-delete olunur (işçi-ləğvi ilə eyni davranış).
    /// </summary>
    public async Task<Result> HrLegvEtAsync(int id, string? sebeb, int hrId)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>().IdIleGetirAsync(id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        if (m.Status != MezuniyyetStatus.Tesdiqlenib)
            return Result.Fail("Yalnız təsdiqlənmiş məzuniyyət HR tərəfindən ləğv edilə bilər.");

        // HR YALNIZ işçinin ləğv müraciəti əsasında ləğv edə bilər.
        if (!m.LegvTelebEdilib)
            return Result.Fail("HR yalnız işçinin ləğv müraciəti əsasında ləğv edə bilər — əvvəlcə işçi ləğv müraciəti göndərməlidir.");

        if (m.BaslamaTarixi.Date <= DateTime.Today)
            return Result.Fail("Məzuniyyət başlayıb (və ya bu gün başlayır) — HR ləğvi mümkün deyil.");

        if (m.OdenisStatus == MezuniyyetOdenisStatus.Odenilib ||
            m.OdenisStatus == MezuniyyetOdenisStatus.PlanliOdenis)
            return Result.Fail("Qabaqcadan ödəniş artıq icra olunub/planlanıb — ləğv mümkün deyil. Əvvəlcə pulun geri-alınması əl ilə həll olunmalıdır.");

        // Yalnız hələ ödənilməmiş (Gozleyir) avans — Mühasibə ləğv bildirişi gedəcək
        var avansGozleyirdi = m.OdenisStatus == MezuniyyetOdenisStatus.Gozleyir;

        // Balansı geri qaytar — FifoKes ilə kəsilən eyni effektiv gün sayı (LIFO)
        await BalansiGeriQaytarAsync(m.IsciId, m.Nov, m.EfektivGunSayi);

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

        // Gözləyən avansı sıfırla (ödəniş hələ edilməyib)
        if (avansGozleyirdi)
        {
            m.OdenenMebleg = null;
            m.OdenenMeblegBrut = null;
            m.OdenisStatus = MezuniyyetOdenisStatus.TetbiqEdilmir;
            m.PlanliOdenisTarixi = null;
        }

        // Audit: ləğv səbəbi + status (soft-delete-dən əvvəl yazılır — eyni tracked instance)
        m.Status = MezuniyyetStatus.LegvEdildi;
        m.ImtinaSebebi = string.IsNullOrWhiteSpace(sebeb)
            ? "HR tərəfindən ləğv edildi."
            : sebeb.Trim();
        await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m);

        // Aktiv siyahılardan çıxsın
        await _unitOfWork.Repository<Mezuniyyet>().YumshakSilAsync(id);
        await _unitOfWork.YaddaSaxlaAsync();

        // Bildirişlər
        await NotifyIsciForHrCancelAsync(m, sebeb);
        if (avansGozleyirdi)
            await NotifyMuhasibForCancelAsync(m);

        return Result.Ok(avansGozleyirdi
            ? "Məzuniyyət ləğv edildi. Gözləyən ödəniş sıfırlandı, Mühasibə bildiriş göndərildi."
            : "Məzuniyyət ləğv edildi.");
    }

    // ── İşçi ləğv müraciəti — ləğv ETMİR, yalnız bayraq + səbəb + HR-a bildiriş ──
    public async Task<Result> LegvTelebiEtAsync(int id, int isciId, string sebeb)
    {
        if (string.IsNullOrWhiteSpace(sebeb))
            return Result.Fail("Ləğv səbəbi qeyd edilməlidir.");

        var m = await _unitOfWork.Repository<Mezuniyyet>().IdIleGetirAsync(id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");
        if (m.IsciId != isciId) return Result.Fail("Bu müraciət sizə aid deyil.");

        if (m.Status != MezuniyyetStatus.Tesdiqlenib)
            return Result.Fail("Yalnız təsdiqlənmiş məzuniyyət üçün ləğv müraciəti göndərilə bilər.");
        if (m.BaslamaTarixi.Date <= DateTime.Today)
            return Result.Fail("Məzuniyyət başlayıb (və ya bu gün başlayır) — ləğv müraciəti mümkün deyil.");
        if (m.OdenisStatus == MezuniyyetOdenisStatus.Odenilib ||
            m.OdenisStatus == MezuniyyetOdenisStatus.PlanliOdenis)
            return Result.Fail("Qabaqcadan ödəniş artıq icra olunub/planlanıb — ləğv müraciəti mümkün deyil. HR ilə əlaqə saxlayın.");
        if (m.LegvTelebEdilib)
            return Result.Fail("Bu məzuniyyət üçün ləğv müraciəti artıq göndərilib.");

        m.LegvTelebEdilib = true;
        m.LegvTelebSebebi = sebeb.Trim();
        m.LegvTelebTarixi = DateTime.Now;
        await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m);
        await _unitOfWork.YaddaSaxlaAsync();

        await NotifyHrForLegvTelebiAsync(m);
        return Result.Ok("Ləğv müraciəti HR-a göndərildi. Təsdiqləndikdən sonra məzuniyyət ləğv ediləcək.");
    }

    // ── HR ləğv müraciətini RƏDD edir — bayraq təmizlənir, işçiyə bildiriş ──
    public async Task<Result> LegvTelebiRedEtAsync(int id, int hrId, string? sebeb)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>().IdIleGetirAsync(id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");
        if (!m.LegvTelebEdilib)
            return Result.Fail("Bu məzuniyyət üçün ləğv müraciəti yoxdur.");

        m.LegvTelebEdilib = false;
        m.LegvTelebSebebi = null;
        m.LegvTelebTarixi = null;
        await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m);
        await _unitOfWork.YaddaSaxlaAsync();

        await NotifyIsciForLegvRedAsync(m, sebeb);
        return Result.Ok("Ləğv müraciəti rədd edildi.");
    }

    // ── HR üçün gözləyən ləğv müraciətləri ──
    public async Task<Result<IList<MezuniyyetListDto>>> GetLegvTelebleriAsync()
    {
        try
        {
            var entities = await _unitOfWork.Repository<Mezuniyyet>()
                .HamisiniGetirAsync(
                    predicate: x => x.LegvTelebEdilib && x.Status == MezuniyyetStatus.Tesdiqlenib,
                    include: q => q
                        .Include(m => m.Isci).ThenInclude(i => i.IsciTeyinatlari).ThenInclude(t => t.Departament)
                        .Include(m => m.Isci).ThenInclude(i => i.IsciTeyinatlari).ThenInclude(t => t.Vezife)
                        .Include(m => m.EvezEdenIsci),
                    izlemeden: true);

            var dtos = entities
                .OrderBy(x => x.LegvTelebTarixi)
                .Select(m => new MezuniyyetListDto
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
                    OdenisStatus = m.OdenisStatus,
                    LegvTelebEdilib = m.LegvTelebEdilib,
                    LegvTelebSebebi = m.LegvTelebSebebi,
                    LegvTelebTarixi = m.LegvTelebTarixi,
                }).ToList();

            return Result<IList<MezuniyyetListDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<IList<MezuniyyetListDto>>.Fail($"Ləğv müraciətləri gətirilmədi: {ex.Message}");
        }
    }

    private async Task NotifyHrForLegvTelebiAsync(Mezuniyyet m)
    {
        var isciAd = await GetIsciAdAsync(m.IsciId);
        var dovr = $"{m.BaslamaTarixi:dd.MM.yyyy} – {m.BitmeTarixi:dd.MM.yyyy}";
        await _bildirisRouter.NotifyRolesAsync(
            new[] { RoleNames.HR, RoleNames.Admin },
            BildirisNovu.MezuniyyetMuraciet,
            "Məzuniyyət ləğv müraciəti — HR təsdiqi gözləyir",
            $"{isciAd} ({dovr}) təsdiqlənmiş məzuniyyətinin ləğvini istəyir. Səbəb: {m.LegvTelebSebebi}",
            redirectUrl: $"/HR/Mezuniyyet/Detal/{m.Id}?returnAction=LegvTelebleri",
            mezuniyyetId: m.Id, exceptIsciId: m.IsciId);
    }

    private async Task NotifyIsciForLegvRedAsync(Mezuniyyet m, string? sebeb)
    {
        var dovr = $"{m.BaslamaTarixi:dd.MM.yyyy} – {m.BitmeTarixi:dd.MM.yyyy}";
        var sebebMetn = string.IsNullOrWhiteSpace(sebeb) ? "" : $" Səbəb: {sebeb.Trim()}";
        await _bildirisRouter.NotifyIsciAsync(
            m.IsciId,
            BildirisNovu.MezuniyyetMuraciet,
            "Məzuniyyət ləğv müraciəti rədd edildi",
            $"{dovr} məzuniyyətiniz üçün ləğv müraciətiniz HR tərəfindən rədd edildi — məzuniyyət qüvvədə qalır.{sebebMetn}",
            redirectUrl: "/User/Mezuniyyet/Index",
            mezuniyyetId: m.Id);
    }

    /// <summary>
    /// HR təsdiqlənmiş məzuniyyətin tarixlərini dəyişir (başlanğıc keçməyib).
    /// Köhnə günlər balansa qaytarılır, yeni günlər FIFO kəsilir; davamiyyət
    /// uzlaşdırılır (üst-üstə düşən günlər saxlanır — unikal index pozulmasın).
    /// Avans icra olunubsa (Odenilib/PlanliOdenis) və ya dövlət-vəzifə korreksiyası
    /// varsa bloklanır; gözləyən avans varsa məbləğ yeni tarixlərə görə yenilənir.
    /// Hamısı bir tranzaksiyada — atomar.
    /// </summary>
    public async Task<Result> HrTarixDeyisAsync(int id, DateTime yeniBaslama, DateTime yeniBitme, string? sebeb, int hrId)
    {
        yeniBaslama = yeniBaslama.Date;
        yeniBitme = yeniBitme.Date;

        if (yeniBitme < yeniBaslama)
            return Result.Fail("Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");
        if (yeniBaslama < DateTime.Today)
            return Result.Fail("Yeni başlama tarixi keçmişdə ola bilməz (bu gün və ya sonra olmalıdır).");

        var m = await _unitOfWork.Repository<Mezuniyyet>().IdIleGetirAsync(id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        if (m.Status != MezuniyyetStatus.Tesdiqlenib)
            return Result.Fail("Yalnız təsdiqlənmiş məzuniyyətin tarixi dəyişdirilə bilər.");

        if (m.BaslamaTarixi.Date <= DateTime.Today)
            return Result.Fail("Məzuniyyət başlayıb (və ya bu gün başlayır) — tarix dəyişdirilə bilməz.");

        if (m.OdenisStatus == MezuniyyetOdenisStatus.Odenilib ||
            m.OdenisStatus == MezuniyyetOdenisStatus.PlanliOdenis)
            return Result.Fail("Qabaqcadan ödəniş artıq icra olunub/planlanıb — tarix dəyişdirilə bilməz. Əvvəlcə ödənişi/məzuniyyəti ləğv edin.");

        // Dövlət-vəzifə korreksiyası tətbiq olunubsa — balans/davamiyyət təsiri mürəkkəbdir, bloklanır
        var korreksiyaVar = await _unitOfWork.Repository<Mezuniyyet>()
            .MovcuddurmuAsync(x => x.KorreksiyaOlunanMezuniyyetId == id && !x.Silinib);
        if (korreksiyaVar)
            return Result.Fail("Bu məzuniyyətə dövlət-vəzifə korreksiyası tətbiq olunub — tarix dəyişdirilə bilməz.");

        if (m.BaslamaTarixi.Date == yeniBaslama && m.BitmeTarixi.Date == yeniBitme)
            return Result.Fail("Tarixlər onsuz da eynidir.");

        int yeniGun = await HesablaIsGunuAsync(yeniBaslama, yeniBitme);
        if (yeniGun <= 0)
            return Result.Fail("Yeni tarix aralığında heç bir hesablanan gün yoxdur.");

        int kohneGun = m.EfektivGunSayi;
        var kohneBaslama = m.BaslamaTarixi.Date;
        var kohneBitme = m.BitmeTarixi.Date;

        // Balans yoxlaması (yalnız Illik) — köhnə günlər qayıdandan sonra yeni günlər yerləşməli
        if (m.Nov == MezuniyyetNovu.Illik)
        {
            var umumiQaliq = await _unitOfWork.Repository<MezuniyyetBalans>().Query()
                .Where(x => !x.Silinib && x.IsciId == m.IsciId && x.Nov == m.Nov)
                .SumAsync(x => (int?)(x.ToplamGun - x.IstifadeOlunanGun)) ?? 0;
            if (umumiQaliq + kohneGun < yeniGun)
                return Result.Fail($"Kifayət qədər əmək məzuniyyəti balansı yoxdur. Mümkün: {umumiQaliq + kohneGun} gün, yeni tələb: {yeniGun} gün.");
        }

        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            // 1) KÖHNƏNİ GERİ AL — balans (LIFO). Aralıq save ki, FIFO kəsmə
            //    yenilənmiş balansı oxusun (eyni tranzaksiya — atomarlıq pozulmur).
            await BalansiGeriQaytarAsync(m.IsciId, m.Nov, kohneGun);
            await _unitOfWork.YaddaSaxlaAsync();

            // 2) YENİ TARİXLƏR + gün sayı (köhnə manual override artıq keçərsizdir → təmizlə)
            m.BaslamaTarixi = yeniBaslama;
            m.BitmeTarixi = yeniBitme;
            m.IsGunlerininSayi = yeniGun;
            m.IsGunlerininSayiManual = null;
            m.GunHesabiDuzelisiSebebi = string.IsNullOrWhiteSpace(sebeb) ? null : sebeb.Trim();

            // 3) YENİ GÜNLƏRİ KƏS (FIFO)
            await BalansiFifoKesAsync(m.IsciId, m.Nov, m.EfektivGunSayi);

            // 4) DAVAMIYYƏT UZLAŞDIRMA
            var status = m.Nov switch
            {
                MezuniyyetNovu.Xestelik => DavamiyyetStatus.Xestelik,
                MezuniyyetNovu.Ezamiyyet => DavamiyyetStatus.Ezamiyyet,
                MezuniyyetNovu.OzHesabina => DavamiyyetStatus.OdenissizMezuniyyet,
                _ => DavamiyyetStatus.Icazeli
            };
            // 4a) Yeni aralıqdakı hesablanan günləri təmin et (upsert — unikal index təhlükəsiz)
            var skipYeni = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x => x.Tarix >= yeniBaslama && x.Tarix <= yeniBitme
                                      && x.Tip == GunTipi.Bayram && !x.MezuniyyetdeHesablanir);
            for (var gun = yeniBaslama; gun <= yeniBitme; gun = gun.AddDays(1))
            {
                if (skipYeni.Any(b => b.Tarix.Date == gun.Date)) continue;
                await DavamiyyetUpsertAsync(m.IsciId, gun, status);
            }
            // 4b) Köhnə aralıqda olub yeni aralıqda OLMAYAN leave-günlərini sil
            for (var gun = kohneBaslama; gun <= kohneBitme; gun = gun.AddDays(1))
            {
                if (gun >= yeniBaslama && gun <= yeniBitme) continue; // üst-üstə → toxunma
                var dav = await _unitOfWork.Repository<Davamiyyet>()
                    .GetirAsync(x => x.IsciId == m.IsciId && x.Tarix.Date == gun.Date &&
                                     (x.Status == DavamiyyetStatus.Icazeli ||
                                      x.Status == DavamiyyetStatus.Xestelik ||
                                      x.Status == DavamiyyetStatus.Ezamiyyet));
                if (dav != null)
                    await _unitOfWork.Repository<Davamiyyet>().YumshakSilAsync(dav.Id);
            }

            // 5) AVANS — gözləyirsə yeni tarixlərə görə məbləği yenilə
            var avansGozleyir = m.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis
                             && m.OdenisStatus == MezuniyyetOdenisStatus.Gozleyir;
            if (avansGozleyir)
            {
                var hesab = await _maasHesablamaService
                    .MezuniyyetOdenisiDetalliHesablaAsync(m.IsciId, yeniBaslama, yeniBitme);
                m.OdenenMebleg = hesab.CemiOdenis;
            }

            await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m);
            await _unitOfWork.YaddaSaxlaAsync();
            await transaction.CommitAsync();

            // Bildirişlər (tranzaksiyadan sonra)
            await NotifyIsciForDateChangeAsync(m);
            if (avansGozleyir)
                await NotifyMuhasibForAdvancePaymentAsync(m);

            return Result.Ok($"Məzuniyyət tarixi yeniləndi: {yeniBaslama:dd.MM.yyyy} – {yeniBitme:dd.MM.yyyy} ({yeniGun} gün)."
                + (avansGozleyir ? " Avans məbləği yenidən hesablandı, Mühasibə bildiriş göndərildi." : ""));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// ADMIN: səhv daxil edilmiş məzuniyyətin tarixini düzəldir — HR-dən fərqli olaraq
    /// məzuniyyət ARTIQ BAŞLAYIB/KEÇMİŞ olsa da işləyir (data-entry səhvinin düzəlişi).
    /// Balans, davamiyyət, avans HR versiyası ilə EYNİ atomar məntiqlə uzlaşdırılır.
    /// Yalnız avans ödənilibsə/planlanıbsa bloklanır (pul əl ilə həll olunmalıdır).
    /// </summary>
    public async Task<Result> AdminTarixDeyisAsync(int id, DateTime yeniBaslama, DateTime yeniBitme, string? sebeb, int adminId)
    {
        yeniBaslama = yeniBaslama.Date;
        yeniBitme = yeniBitme.Date;

        if (yeniBitme < yeniBaslama)
            return Result.Fail("Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");
        // ADMIN: keçmiş tarixə də icazə var (səhv düzəlişi) — 'keçmişdə ola bilməz' yoxlaması YOXDUR.

        var m = await _unitOfWork.Repository<Mezuniyyet>().IdIleGetirAsync(id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        if (m.Status != MezuniyyetStatus.Tesdiqlenib)
            return Result.Fail("Yalnız təsdiqlənmiş məzuniyyət düzəldilə bilər.");

        // ADMIN: 'məzuniyyət başlayıb' yoxlaması YOXDUR — məhz belə səhvləri düzəltmək üçündür.

        if (m.OdenisStatus == MezuniyyetOdenisStatus.Odenilib ||
            m.OdenisStatus == MezuniyyetOdenisStatus.PlanliOdenis)
            return Result.Fail("Qabaqcadan ödəniş artıq icra olunub/planlanıb — tarix dəyişdirilə bilməz. Əvvəlcə ödənişi/məzuniyyəti ləğv edin.");

        var korreksiyaVar = await _unitOfWork.Repository<Mezuniyyet>()
            .MovcuddurmuAsync(x => x.KorreksiyaOlunanMezuniyyetId == id && !x.Silinib);
        if (korreksiyaVar)
            return Result.Fail("Bu məzuniyyətə dövlət-vəzifə korreksiyası tətbiq olunub — tarix dəyişdirilə bilməz.");

        if (m.BaslamaTarixi.Date == yeniBaslama && m.BitmeTarixi.Date == yeniBitme)
            return Result.Fail("Tarixlər onsuz da eynidir.");

        int yeniGun = await HesablaIsGunuAsync(yeniBaslama, yeniBitme);
        if (yeniGun <= 0)
            return Result.Fail("Yeni tarix aralığında heç bir hesablanan gün yoxdur.");

        int kohneGun = m.EfektivGunSayi;
        var kohneBaslama = m.BaslamaTarixi.Date;
        var kohneBitme = m.BitmeTarixi.Date;

        // Balans yoxlaması (yalnız Illik)
        if (m.Nov == MezuniyyetNovu.Illik)
        {
            var umumiQaliq = await _unitOfWork.Repository<MezuniyyetBalans>().Query()
                .Where(x => !x.Silinib && x.IsciId == m.IsciId && x.Nov == m.Nov)
                .SumAsync(x => (int?)(x.ToplamGun - x.IstifadeOlunanGun)) ?? 0;
            if (umumiQaliq + kohneGun < yeniGun)
                return Result.Fail($"Kifayət qədər əmək məzuniyyəti balansı yoxdur. Mümkün: {umumiQaliq + kohneGun} gün, yeni tələb: {yeniGun} gün.");
        }

        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            // 1) Köhnəni geri al — balans (LIFO)
            await BalansiGeriQaytarAsync(m.IsciId, m.Nov, kohneGun);
            await _unitOfWork.YaddaSaxlaAsync();

            // 2) Yeni tarixlər + gün sayı
            m.BaslamaTarixi = yeniBaslama;
            m.BitmeTarixi = yeniBitme;
            m.IsGunlerininSayi = yeniGun;
            m.IsGunlerininSayiManual = null;
            m.GunHesabiDuzelisiSebebi = string.IsNullOrWhiteSpace(sebeb) ? "Admin tarix düzəlişi" : sebeb.Trim();

            // 3) Yeni günləri kəs (FIFO)
            await BalansiFifoKesAsync(m.IsciId, m.Nov, m.EfektivGunSayi);

            // 4) Davamiyyət uzlaşdırma
            var status = m.Nov switch
            {
                MezuniyyetNovu.Xestelik => DavamiyyetStatus.Xestelik,
                MezuniyyetNovu.Ezamiyyet => DavamiyyetStatus.Ezamiyyet,
                MezuniyyetNovu.OzHesabina => DavamiyyetStatus.OdenissizMezuniyyet,
                _ => DavamiyyetStatus.Icazeli
            };
            var skipYeni = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x => x.Tarix >= yeniBaslama && x.Tarix <= yeniBitme
                                      && x.Tip == GunTipi.Bayram && !x.MezuniyyetdeHesablanir);
            for (var gun = yeniBaslama; gun <= yeniBitme; gun = gun.AddDays(1))
            {
                if (skipYeni.Any(b => b.Tarix.Date == gun.Date)) continue;
                await DavamiyyetUpsertAsync(m.IsciId, gun, status);
            }
            for (var gun = kohneBaslama; gun <= kohneBitme; gun = gun.AddDays(1))
            {
                if (gun >= yeniBaslama && gun <= yeniBitme) continue;
                var dav = await _unitOfWork.Repository<Davamiyyet>()
                    .GetirAsync(x => x.IsciId == m.IsciId && x.Tarix.Date == gun.Date &&
                                     (x.Status == DavamiyyetStatus.Icazeli ||
                                      x.Status == DavamiyyetStatus.Xestelik ||
                                      x.Status == DavamiyyetStatus.Ezamiyyet));
                if (dav != null)
                    await _unitOfWork.Repository<Davamiyyet>().YumshakSilAsync(dav.Id);
            }

            // 5) Avans — gözləyirsə yeni tarixlərə görə məbləği yenilə
            var avansGozleyir = m.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis
                             && m.OdenisStatus == MezuniyyetOdenisStatus.Gozleyir;
            if (avansGozleyir)
            {
                var hesab = await _maasHesablamaService
                    .MezuniyyetOdenisiDetalliHesablaAsync(m.IsciId, yeniBaslama, yeniBitme);
                m.OdenenMebleg = hesab.CemiOdenis;
            }

            await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(m);
            await _unitOfWork.YaddaSaxlaAsync();
            await transaction.CommitAsync();

            await NotifyIsciForDateChangeAsync(m);
            if (avansGozleyir)
                await NotifyMuhasibForAdvancePaymentAsync(m);

            return Result.Ok($"Məzuniyyət tarixi admin tərəfindən düzəldildi: {yeniBaslama:dd.MM.yyyy} – {yeniBitme:dd.MM.yyyy} ({yeniGun} gün)."
                + (avansGozleyir ? " Avans məbləği yenidən hesablandı, Mühasibə bildiriş göndərildi." : ""));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Davamiyyət upsert (tarix-dəyiş üçün). Aktiv qeyd varsa yalnız Qayib-i çevirir
    /// (digər statuslara toxunmur — təsdiq məntiqi ilə eyni). Soft-silinmiş qeyd varsa
    /// onu dirildir — çünki unikal index (IsciId, Tarix) Silinib-i filtrləmir,
    /// üstünə YENİ insert constraint pozar. Heç biri yoxdursa yeni qeyd yaradır.
    /// </summary>
    private async Task DavamiyyetUpsertAsync(int isciId, DateTime gun, DavamiyyetStatus status)
    {
        var aktiv = await _unitOfWork.Repository<Davamiyyet>()
            .GetirAsync(x => x.IsciId == isciId && x.Tarix.Date == gun.Date);
        if (aktiv != null)
        {
            // Qayıb VƏ səhvən vurulmuş cihaz girişi (İşdə/Gecikmə) → məzuniyyət statusuna çevir
            // (bugünə/keçmişə uzadılanda işçi cihaza basmış ola bilər). Qəsdən qoyulmuş leave
            // statuslarına (İcazəli/Xəstəlik/Ezamiyyət/Dövlət vəzifəsi) toxunmuruq.
            if (aktiv.Status == DavamiyyetStatus.Qayib ||
                aktiv.Status == DavamiyyetStatus.Isde ||
                aktiv.Status == DavamiyyetStatus.Gecikme)
            {
                aktiv.Status     = status;
                aktiv.GirisVaxti = null;
                aktiv.CixisVaxti = null;
                await _unitOfWork.Repository<Davamiyyet>().YenileAsync(aktiv);
            }
            return;
        }

        var silinmis = await _unitOfWork.Repository<Davamiyyet>()
            .SilinmisGetirAsync(x => x.IsciId == isciId && x.Tarix.Date == gun.Date);
        if (silinmis != null)
        {
            silinmis.Silinib = false;
            silinmis.SilinmeTarixi = null;
            silinmis.Status = status;
            await _unitOfWork.Repository<Davamiyyet>().YenileAsync(silinmis);
            return;
        }

        await _unitOfWork.Repository<Davamiyyet>().YaratAsync(new Davamiyyet
        {
            IsciId = isciId,
            Tarix = gun,
            Status = status
        });
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

    private async Task NotifyIsciForHrCancelAsync(Mezuniyyet m, string? sebeb)
    {
        var dovr = $"{m.BaslamaTarixi:dd.MM.yyyy} – {m.BitmeTarixi:dd.MM.yyyy}";
        var sebebMetn = string.IsNullOrWhiteSpace(sebeb) ? "" : $" Səbəb: {sebeb.Trim()}";
        await _bildirisRouter.NotifyIsciAsync(
            m.IsciId,
            BildirisNovu.MezuniyyetImtina,
            "Məzuniyyət — HR tərəfindən ləğv edildi",
            $"{dovr} təsdiqlənmiş məzuniyyətiniz HR tərəfindən ləğv edildi.{sebebMetn}",
            redirectUrl: "/User/Mezuniyyet/Index",
            mezuniyyetId: m.Id);
    }

    private async Task NotifyIsciForDateChangeAsync(Mezuniyyet m)
    {
        var dovr = $"{m.BaslamaTarixi:dd.MM.yyyy} – {m.BitmeTarixi:dd.MM.yyyy}";
        await _bildirisRouter.NotifyIsciAsync(
            m.IsciId,
            BildirisNovu.MezuniyyetTesdiq,
            "Məzuniyyət — tarix yeniləndi",
            $"Təsdiqlənmiş məzuniyyətinizin tarixi HR tərəfindən yeniləndi: {dovr} ({m.EfektivGunSayi} gün).",
            redirectUrl: "/User/Mezuniyyet/Index",
            mezuniyyetId: m.Id);
    }

    private async Task NotifyMuhasibForCancelAsync(Mezuniyyet m)
    {
        var isciAd = await GetIsciAdAsync(m.IsciId);
        var dovr = $"{m.BaslamaTarixi:dd.MM.yyyy} – {m.BitmeTarixi:dd.MM.yyyy}";
        await _bildirisRouter.NotifyRolesAsync(
            new[] { RoleNames.Muhasib, RoleNames.Admin },
            BildirisNovu.MezuniyyetImtina,
            "Məzuniyyət ödənişi — ləğv edildi",
            $"{isciAd} üçün {dovr} məzuniyyəti ləğv edildi. Gözləyən qabaqcadan ödənişi İCRA ETMƏYİN.",
            redirectUrl: "/HR/MezuniyyetOdenis",
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
                    OdenisStatus = m.OdenisStatus,
                    LegvTelebEdilib = m.LegvTelebEdilib,
                    LegvTelebSebebi = m.LegvTelebSebebi,
                    LegvTelebTarixi = m.LegvTelebTarixi,
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
                JetonIleOdendi = m.JetonIleOdendi,
                IstifadeOlunanJetonSaat = m.IstifadeOlunanJetonSaat,
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

        // Qabaqcadan ödəniş artıq icra olunub/planlanıbsa — ləğv bloklanır.
        // (Pul köçürülüb; geri-alınma HR/Mühasib tərəfindən əl ilə həll olunmalıdır.)
        // Qeyd: OdenisStatus yalnız təsdiqlənmiş məzuniyyətdə qeyri-default olur,
        // ona görə bu yoxlama proses-də olan müraciətləri təsir etmir.
        if (m.OdenisStatus == MezuniyyetOdenisStatus.Odenilib ||
            m.OdenisStatus == MezuniyyetOdenisStatus.PlanliOdenis)
            return Result.Fail("Qabaqcadan ödəniş artıq icra olunub — ləğv mümkün deyil. HR ilə əlaqə saxlayın.");

        // Yalnız hələ ödənilməmiş (Gozleyir) avans varsa — Mühasibə ləğv bildirişi gedəcək
        var avansGozleyirdi = m.OdenisStatus == MezuniyyetOdenisStatus.Gozleyir;

        // Təsdiqlənmiş məzuniyyəti ləğv edəndə balansı geri qaytar
        if (m.Status == MezuniyyetStatus.Tesdiqlenib)
        {
            // BalansiFifoKesAsync EfektivGunSayi istifadə edib, çox il üzrə kəsmiş ola bilər.
            // BalansiGeriQaytarAsync (LIFO) eyni miqdarı düzgün şəkildə geri qaytarır.
            await BalansiGeriQaytarAsync(m.IsciId, m.Nov, m.EfektivGunSayi);

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

            // Gözləyən qabaqcadan ödənişi sıfırla (ödəniş hələ edilməyib).
            // m tracked-dir; aşağıdakı YumshakSilAsync(id) → Update(m) bu dəyişiklikləri də saxlayır.
            if (avansGozleyirdi)
            {
                m.OdenenMebleg = null;
                m.OdenenMeblegBrut = null;
                m.OdenisStatus = MezuniyyetOdenisStatus.TetbiqEdilmir;
                m.PlanliOdenisTarixi = null;
            }
        }

        await _unitOfWork.Repository<Mezuniyyet>().YumshakSilAsync(id);
        await _unitOfWork.YaddaSaxlaAsync();

        // Gözləyən avans ləğv olundusa Mühasibi xəbərdar et
        if (avansGozleyirdi)
            await NotifyMuhasibForCancelAsync(m);

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
            OdenisTipi = entity.OdenisTipi,
            OdenisStatus = entity.OdenisStatus,
            OdenenMebleg = entity.OdenenMebleg,
            PlanliOdenisTarixi = entity.PlanliOdenisTarixi,
            EmrRegem = entity.EmrRegem,
            EmrSuffiks = entity.EmrSuffiks,
            EmrIl = entity.EmrIl,
            SenedYolu = entity.SenedYolu,
            KorreksiyaSebebi = entity.KorreksiyaSebebi,
            LegvTelebEdilib = entity.LegvTelebEdilib,
            LegvTelebSebebi = entity.LegvTelebSebebi,
            LegvTelebTarixi = entity.LegvTelebTarixi,
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
    // Məzuniyyət bir günü tam örtürsə, həmin günkü TƏSDİQLƏNMİŞ İcazə artıqdır → ləğv olunur
    // (saatı boşa getməsin). Jetonla ödənilibsə jeton geri qaytarılır. Bayram günündəki
    // İcazəyə toxunulmur (məzuniyyət o günü onsuz da hesablamır).
    // Çağıran tranzaksiya daxilində istifadə edir — YaddaSaxla çağıranın üzərindədir.
    private async Task _OrtulenIcazeleriLegvEtAsync(int isciId, DateTime baslama, DateTime bitme)
    {
        var bayramlar = await _unitOfWork.Repository<BayramGunu>()
            .HamisiniGetirAsync(x => x.Tarix >= baslama && x.Tarix <= bitme
                && x.Tip == GunTipi.Bayram && !x.MezuniyyetdeHesablanir);

        var icazeler = (await _unitOfWork.Repository<Icaze>()
            .HamisiniGetirAsync(x => x.IsciId == isciId
                && x.Status == IcazeStatus.Tesdiqlenib && !x.Silinib
                && x.IcazeTarixi.Date >= baslama.Date && x.IcazeTarixi.Date <= bitme.Date))
            .Where(ic => !bayramlar.Any(b => b.Tarix.Date == ic.IcazeTarixi.Date))
            .ToList();

        foreach (var ic in icazeler)
        {
            if (ic.JetonOdenenSaat > 0)
                await _jetonService.IcazeJetonuGeriQaytarAsync(ic.IsciId, ic.JetonOdenenSaat);

            ic.Status = IcazeStatus.ImtinaEdildi;
            ic.ImtinaSebebi = "Məzuniyyət ilə əvəz olundu (həmin gün məzuniyyətdir).";
            await _unitOfWork.Repository<Icaze>().YenileAsync(ic);
        }
    }

    private async Task BalansiFifoKesAsync(int isciId, MezuniyyetNovu nov, int gunu)
    {
        if (gunu <= 0) return;
        // Öz hesabına (ödənişsiz) məzuniyyət illik balansa DƏYMİR (Ə.M. 129).
        if (nov == MezuniyyetNovu.OzHesabina) return;

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
    // BİRBAŞA QEYD — HR işçinin yerinə birbaşa məzuniyyət (keçmiş/gələcək)
    // ════════════════════════════════════════════════════════════
    // İllik/absent düzəlişi keçmişə baxır — 90 gün həddi məntiqlidir.
    private const int GeriyeQeydMaxGun = 90;
    // Öz hesabına (ödənişsiz) uzunmüddətli/açıq ola bilər və başlanğıcı 90 gündən
    // köhnə düşə bilər (bank işçini əvvəl göndərib). Ona görə geriyə həddi geniş —
    // 1 il. Pulu onsuz da aşağıdakı "ödənilmiş ay" bloku (1b) qoruyur.
    private const int GeriyeQeydOzHesabinaMaxGun = 365;

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
                // Geriyə həddi növə görə: öz hesabına 1 il, digərləri 90 gün.
                int maxKohneGun = dto.Nov == MezuniyyetNovu.OzHesabina
                    ? GeriyeQeydOzHesabinaMaxGun
                    : GeriyeQeydMaxGun;
                var minTarix = bugun.AddDays(-maxKohneGun);
                if (dto.BaslamaTarixi.Date < minTarix)
                    return Result<MezuniyyetDto>.Fail($"Keçmiş tarixlər üçün maksimum {maxKohneGun} gün əvvələ qədər qeyd etmək olar.");

                // ── 1b. Maaşı ödənilmiş aya geriyə məzuniyyət YAZILA BİLMƏZ ──
                // O günlərin maaşı artıq verilib; sonradan məzuniyyət yazmaq düzgün deyil.
                var yoxlananAylar = new HashSet<(int Il, int Ay)>();
                for (var g = dto.BaslamaTarixi.Date; g <= dto.BitmeTarixi.Date; g = g.AddDays(1))
                    yoxlananAylar.Add((g.Year, g.Month));
                foreach (var (il, ay) in yoxlananAylar)
                {
                    var maasOdenilib = await _unitOfWork.Repository<Maas>()
                        .MovcuddurmuAsync(m => !m.Silinib && m.IsciId == dto.IsciId
                            && m.Il == il && m.Ay == ay && m.Status == MaasStatus.Odenildi);
                    if (maasOdenilib)
                        return Result<MezuniyyetDto>.Fail($"{ay:00}.{il} ayının maaşı artıq ödənilib — həmin aya geriyə məzuniyyət yazmaq olmaz.");
                }

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

                // ── 4. Balans yoxlanışı ────────────────────────────────────
                if (dto.JetonIleEvezlesdir)
                {
                    // Jeton rejimi — illik balans tələb olunmur, jeton balansı yoxlanır
                    var lazimSaat = isGunu * 8m;
                    var jetonBalansi = await _jetonService.AktivSaatBalansiAsync(dto.IsciId);
                    if (jetonBalansi < lazimSaat)
                        return Result<MezuniyyetDto>.Fail(
                            $"Jeton balansı kifayət etmir. Mövcud: {jetonBalansi:0.##} saat, " +
                            $"tələb olunur: {lazimSaat:0.##} saat ({isGunu} iş günü × 8).");
                }
                else if (dto.Nov == MezuniyyetNovu.Illik)
                {
                    var umumiQaliq = await _unitOfWork.Repository<MezuniyyetBalans>().Query()
                        .Where(x => !x.Silinib && x.IsciId == dto.IsciId && x.Nov == dto.Nov)
                        .SumAsync(x => (int?)(x.ToplamGun - x.IstifadeOlunanGun)) ?? 0;

                    if (umumiQaliq < isGunu)
                        return Result<MezuniyyetDto>.Fail(
                            $"Əmək məzuniyyəti balansı kifayət etmir. Bütün illərin cəmi qalıq: {umumiQaliq} gün, tələb olunur: {isGunu} gün.");
                }

                // ── 5. Entity yarat — bütün addımlar HR tərəfindən təsdiqli ──
                var indi = DateTime.Now;
                var sebebTrim = string.IsNullOrWhiteSpace(dto.Sebeb) ? null : dto.Sebeb.Trim();
                var qeydMetni = sebebTrim == null
                    ? "[Geriyə qeyd — HR]"
                    : $"[Geriyə qeyd — HR] {sebebTrim}";

                // ── Əmr nömrəsi: 2 rejim ──────────────────────────────────
                // 1) AVTOMATIK (DTO.EmrRegem null): VAHID EmrSayghaci sayğacından
                //    növbəti nömrə (normal məzuniyyət əmrləri ilə eyni ardıcıllıq).
                //    Suffiks default BOŞ (suffiksiz) — adi əmr kimi. Yeni əmr olur.
                // 2) ARAYA ƏLAVƏ (DTO.EmrRegem dolu): HR mövcud nömrəni göstərir,
                //    sistem yalnız (EmrIl, EmrRegem, EmrSuffiks) üçlüyünün
                //    unikallığını yoxlayır. Əmr "K/M 5G 2026" kimi formalaşır
                //    və sıralamada K/M 5 ilə K/M 6 arasında qalır.
                // Default BOŞ — manuel "araya əlavə" rejimində aşağıda suffiks mütləq.
                // JETON ilə məzuniyyət əmr nömrəsi ALMIR (istək 2026-07): jetonla tam
                // məzuniyyətə çıxanda əmr reyestrindən nömrə/suffiks GÖTÜRÜLMÜR — nə
                // avtomatik sayğac artır, nə manual araya əlavə. EmrRegem/EmrIl null qalır,
                // entity-nin göstərmə property-si də null qaytarır (əmr görünmür).
                var suffiks = dto.JetonIleEvezlesdir || string.IsNullOrWhiteSpace(dto.EmrSuffiks)
                    ? null
                    : dto.EmrSuffiks.Trim().ToUpperInvariant();

                int? emrIl = null;
                int? emrRegem = null;

                if (!dto.JetonIleEvezlesdir)
                {
                    if (dto.EmrRegem.HasValue && dto.EmrRegem.Value > 0)
                    {
                        // Manuel rejim — araya əlavə
                        emrIl = dto.EmrIl.HasValue && dto.EmrIl.Value > 0
                            ? dto.EmrIl.Value
                            : indi.Year;
                        emrRegem = dto.EmrRegem.Value;

                        // Suffiks olmadan dublikat ola bilməz: əgər suffiks boşdursa,
                        // əsas nömrəni mövcud normal əmrlə qarışdırmağa imkan yoxdur.
                        if (string.IsNullOrEmpty(suffiks))
                        {
                            return Result<MezuniyyetDto>.Fail(
                                "Mövcud əmr nömrəsinə əlavə edərkən suffiks mütləq olmalıdır " +
                                "(məs: G, X, T) — yoxsa mövcud əmrlə dublikat olar.");
                        }

                        var emrKonflikt = await _unitOfWork.Repository<Mezuniyyet>()
                            .MovcuddurmuAsync(x =>
                                !x.Silinib &&
                                x.EmrIl == emrIl &&
                                x.EmrRegem == emrRegem &&
                                x.EmrSuffiks == suffiks);
                        if (emrKonflikt)
                        {
                            return Result<MezuniyyetDto>.Fail(
                                $"K/M {emrRegem}{suffiks} {emrIl} əmr nömrəsi artıq mövcuddur. " +
                                "Başqa suffiks seçin (məs. X, T) və ya başqa nömrə göstərin.");
                        }
                    }
                    else
                    {
                        // Avtomatik rejim — VAHID sayğacdan (normal məzuniyyət əmrləri ilə
                        // EYNİ ardıcıllıq). ƏVVƏL max(Mezuniyyet.EmrRegem)+1 idi: EmrSayghaci
                        // sayğacını artırmırdı, ona görə normal axın həmin nömrəni təkrar
                        // verirdi (məs. hər ikisi 76). İndi sayğacdan gəlir → toqquşma yoxdur.
                        var (nextNomre, nextIl) = await _emrService.NovbetiNomreAsync(EmrNovu.Mezuniyyet, indi);
                        emrRegem = nextNomre;
                        emrIl = nextIl;
                    }
                }

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
                    EmrRegem = emrRegem,
                    EmrIl = emrIl,
                    EmrSuffiks = suffiks,
                    HrTesdiq = true,
                    HrId = hrIsciId,
                    HrTesdiqTarixi = indi,
                    JetonIleOdendi = dto.JetonIleEvezlesdir,
                    IstifadeOlunanJetonSaat = dto.JetonIleEvezlesdir ? isGunu * 8m : null
                };

                await _unitOfWork.Repository<Mezuniyyet>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // ── 6. Balansı yenilə ──────────────────────────────────────
                if (dto.JetonIleEvezlesdir)
                {
                    // Jeton rejimi: illik balans toxunulmaz qalır, jeton xərclənir
                    var jetonRes = await _jetonService.IcazeUcunFifoJetonXercleAsync(
                        dto.IsciId, isGunu * 8m, icazeId: null);
                    if (!jetonRes.Success)
                        return Result<MezuniyyetDto>.Fail($"Jeton xərclənmədi: {jetonRes.Message}");

                    // Görünən qeyd: məzuniyyət jeton tutması da Xərcləmə Tarixçəsində görünsün
                    await _jetonService.JetonEvezlesdirmeQeydiAsync(
                        dto.IsciId, isGunu * 8m, dto.BaslamaTarixi, null, null,
                        "Məzuniyyətə jeton əvəzləşdirməsi");
                }
                else if (dto.Nov == MezuniyyetNovu.Illik)
                {
                    await BalansiFifoKesAsync(dto.IsciId, dto.Nov, isGunu);
                }

                // ── 7. Davamiyyət: Qayib → İcazəli/Xəstəlik/Ezamiyyət ──
                var yeniStatus = dto.Nov switch
                {
                    MezuniyyetNovu.Xestelik  => DavamiyyetStatus.Xestelik,
                    MezuniyyetNovu.Ezamiyyet => DavamiyyetStatus.Ezamiyyet,
                    MezuniyyetNovu.OzHesabina => DavamiyyetStatus.OdenissizMezuniyyet,
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
                        // Qayıb VƏ YA səhvən vurulmuş cihaz girişi (İşdə/Gecikmə) → məzuniyyət
                        // statusuna çevir. Səbəb: işçi cihaza basıbsa davamiyyət real-vaxtda
                        // "İşdə"/"Gecikmə" olur; HR sonradan geriyə məzuniyyət yazanda həmin gün
                        // də düzəlməlidir (köhnə kod yalnız "Qayıb"-ı çevirirdi, "İşdə" qalırdı).
                        // Qəsdən qoyulmuş leave-tipli statuslara (İcazəli/Xəstəlik/Ezamiyyət/
                        // Dövlət vəzifəsi) toxunmuruq — onları yuxarıdakı konflikt yoxlaması tutur.
                        if (mevcud.Status == DavamiyyetStatus.Qayib
                            || mevcud.Status == DavamiyyetStatus.Isde
                            || mevcud.Status == DavamiyyetStatus.Gecikme)
                        {
                            mevcud.Status     = yeniStatus;
                            mevcud.GirisVaxti = null;   // səhv giriş/çıxış təmizlənir — məzuniyyət günüdür
                            mevcud.CixisVaxti = null;   // (xam punch CihazOxuma cədvəlində audit üçün qalır)
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

                // 7.1 Üst-üstə düşən təsdiqlənmiş İcazələri ləğv et (gün məzuniyyətdir → İcazə artıqdır)
                await _OrtulenIcazeleriLegvEtAsync(dto.IsciId, dto.BaslamaTarixi, dto.BitmeTarixi);

                await _unitOfWork.YaddaSaxlaAsync();
                await transaction.CommitAsync();

                // ── 8. Bildirişlər ─────────────────────────────────────
                var isciAd = await GetIsciAdAsync(dto.IsciId);
                var dovr = $"{dto.BaslamaTarixi:dd.MM.yyyy} – {dto.BitmeTarixi:dd.MM.yyyy}";

                // İşçiyə
                var sebebMetn = sebebTrim == null ? "" : $" Səbəb: {sebebTrim}";
                var jetonQeyd = dto.JetonIleEvezlesdir ? " (jeton ilə əvəzləşdirildi)" : "";
                await _bildirisRouter.NotifyIsciAsync(
                    dto.IsciId,
                    BildirisNovu.MezuniyyetTesdiq,
                    "HR geriyə məzuniyyət qeyd etdi",
                    $"HR sizin üçün {dovr} ({isGunu} iş günü, {dto.Nov}) məzuniyyəti qeyd etdi.{jetonQeyd}{sebebMetn}",
                    redirectUrl: $"/User/Mezuniyyet/Detail/{entity.Id}",
                    mezuniyyetId: entity.Id);

                // Mühasibə
                var muhasibMetn = dto.JetonIleEvezlesdir
                    ? $"HR {isciAd} üçün {dovr} ({isGunu} iş günü) jeton ilə əvəzləşdirdi — maaşdan tutulma yoxdur."
                    : $"HR {isciAd} üçün {dovr} ({isGunu} iş günü) məzuniyyəti geriyə qeyd etdi. Ay-sonu maaş hesablamasında nəzərə alın.";
                await _bildirisRouter.NotifyRolesAsync(
                    new[] { RoleNames.Muhasib, RoleNames.Admin },
                    BildirisNovu.MezuniyyetOdenisGozleyir,
                    "Geriyə qeyd edilmiş məzuniyyət",
                    muhasibMetn,
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

                var mesaj = dto.JetonIleEvezlesdir
                    ? $"Jeton ilə əvəzləşdirmə uğurla tamamlandı ({isGunu} iş günü, {isGunu * 8} saat jeton xərcləndi). " +
                      $"Davamiyyət: {duzeldilenQaib} qayib → icazəli, {yeniQeyd} yeni qeyd. Maaşdan tutulma yoxdur."
                    : $"Geriyə qeyd uğurla rəsmiləşdirildi ({isGunu} iş günü). " +
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

    // ════════════════════════════════════════════════════════════════════════
    // DÖVLƏT VƏZİFƏSİ KORREKSİYASI — Əmək Məcəlləsi Maddə 173
    // ════════════════════════════════════════════════════════════════════════
    /// <param name="senedSaxlama">
    /// Controller tərəfindən saxlanmış sənədin server yolu (boş ola bilər).
    /// Service fayl saxlamır — yalnız verilmiş yolu entity-yə yazır.
    /// </param>
    public async Task<Result<MezuniyyetDto>> KorreksiyaEtAsync(
        MezuniyyetKorreksiyaDto dto, int hrIsciId, string senedSaxlama)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            // ── 1. Əsas məzuniyyət validasiyası ──────────────────────────
            var original = await _unitOfWork.Repository<Mezuniyyet>()
                .GetirAsync(x => x.Id == dto.MezuniyyetId && !x.Silinib);

            if (original == null)
                return Result<MezuniyyetDto>.Fail("Məzuniyyət tapılmadı.");

            if (original.IsciId != dto.IsciId)
                return Result<MezuniyyetDto>.Fail("İşçi uyğunsuzluğu.");

            if (original.Status != MezuniyyetStatus.Tesdiqlenib)
                return Result<MezuniyyetDto>.Fail(
                    "Korreksiya yalnız təsdiqlənmiş məzuniyyətlərə tətbiq edilə bilər.");

            if (original.Nov != MezuniyyetNovu.Illik)
                return Result<MezuniyyetDto>.Fail(
                    "Korreksiya yalnız əmək məzuniyyətlərinə tətbiq edilir. " +
                    "Xəstəlik və ezamiyyət üçün ayrı prosedur tətbiq olunur.");

            // ── 2. Tarix validasiyası ─────────────────────────────────────
            if (dto.BitmeTarixi < dto.BaslamaTarixi)
                return Result<MezuniyyetDto>.Fail("Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");

            if (dto.BaslamaTarixi.Date < original.BaslamaTarixi.Date ||
                dto.BitmeTarixi.Date > original.BitmeTarixi.Date)
                return Result<MezuniyyetDto>.Fail(
                    "Korreksiya tarixləri əsas məzuniyyət aralığı içərisində olmalıdır.");

            if (dto.KorreksiyaGunSayi <= 0)
                return Result<MezuniyyetDto>.Fail("Korreksiya gün sayı müsbət olmalıdır.");

            int isGunu = await HesablaIsGunuAsync(dto.BaslamaTarixi, dto.BitmeTarixi);
            if (isGunu <= 0)
                return Result<MezuniyyetDto>.Fail(
                    "Seçilən aralıqda iş günü yoxdur (yalnız həftəsonu/bayram).");

            // EfektivGunSayi hər korreksiyadan sonra birbaşa azaldılır,
            // ona görə sadə müqayisə kifayətdir — kumulyativ yoxlama tələb olunmur.
            if (dto.KorreksiyaGunSayi > original.EfektivGunSayi)
                return Result<MezuniyyetDto>.Fail(
                    $"Korreksiya günü ({dto.KorreksiyaGunSayi}) məzuniyyətin " +
                    $"qalan effektiv gün sayını ({original.EfektivGunSayi}) keçə bilməz.");

            // ── 3. Eyni işçi üçün üst-üstə düşən DovletVezifesi yoxlanışı ──
            var artiqVar = await _unitOfWork.Repository<Mezuniyyet>()
                .MovcuddurmuAsync(x =>
                    !x.Silinib &&
                    x.IsciId == dto.IsciId &&
                    x.Nov == MezuniyyetNovu.DovletVezifelerininIcrasi &&
                    x.Status == MezuniyyetStatus.Tesdiqlenib &&
                    x.BaslamaTarixi <= dto.BitmeTarixi &&
                    x.BitmeTarixi >= dto.BaslamaTarixi);

            if (artiqVar)
                return Result<MezuniyyetDto>.Fail(
                    "Bu tarix aralığında işçinin artıq dövlət vəzifəsi qeydi mövcuddur.");

            // ── 4. Balansı geri qaytar (LIFO — ən son ilin balansına) ─────
            await BalansiGeriQaytarAsync(dto.IsciId, MezuniyyetNovu.Illik, dto.KorreksiyaGunSayi);

            // ── 4b. Orijinal Illik qeydin effektiv gün sayını azalt ───────
            // Korreksiya edilmiş günlər Illik məzuniyyət kimi deyil, dövlət
            // vəzifəsi kimi qeyd olunur. Maaş hesablaması original qeydi
            // yalnız qalan günlər üçün sayır (sıfıra çatarsa maaşa daxil olmur).
            // IsGunlerininSayiManual varsa o sahəni azaldırıq — EfektivGunSayi
            // həmişə MIN(Manual ?? Avtomatik) olduğundan hər iki halda düzgün işləyir.
            if (original.IsGunlerininSayiManual.HasValue)
                original.IsGunlerininSayiManual =
                    Math.Max(0, original.IsGunlerininSayiManual.Value - dto.KorreksiyaGunSayi);
            else
                original.IsGunlerininSayi =
                    Math.Max(0, original.IsGunlerininSayi - dto.KorreksiyaGunSayi);

            await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(original);

            // ── 5. Yeni DovletVezifelerininIcrasi məzuniyyəti yarat ───────
            var indi = DateTime.Now;
            var yeniMezuniyyet = new Mezuniyyet
            {
                IsciId                      = dto.IsciId,
                Nov                         = MezuniyyetNovu.DovletVezifelerininIcrasi,
                Status                      = MezuniyyetStatus.Tesdiqlenib,
                BaslamaTarixi               = dto.BaslamaTarixi.Date,
                BitmeTarixi                 = dto.BitmeTarixi.Date,
                IsGunlerininSayi            = dto.KorreksiyaGunSayi,
                Qeyd                        = dto.Qeyd?.Trim(),
                HrTesdiq                    = true,
                HrId                        = hrIsciId,
                HrTesdiqTarixi              = indi,
                KorreksiyaOlunanMezuniyyetId = original.Id,
                SenedYolu                   = string.IsNullOrWhiteSpace(senedSaxlama)
                                                  ? null : senedSaxlama,
                KorreksiyaSebebi            = dto.Qeyd?.Trim(),
                OdenisTipi                  = MezuniyyetOdenisTipi.AySonuOdenis,
                OdenisStatus                = MezuniyyetOdenisStatus.TetbiqEdilmir,
                YaradilmaTarixi             = indi
            };

            await _unitOfWork.Repository<Mezuniyyet>().YaratAsync(yeniMezuniyyet);

            // ── 6. Davamiyyət qeydlərini yenilə ──────────────────────────
            // Həmin günlər üçün Icazeli → OdenisDovletVezifesi, MaasdanKes=false
            var skipBayramlar = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x =>
                    x.Tarix >= dto.BaslamaTarixi &&
                    x.Tarix <= dto.BitmeTarixi   &&
                    x.Tip == GunTipi.Bayram      &&
                    !x.MezuniyyetdeHesablanir);

            int yenilenDavamiyyet = 0;
            for (var gun = dto.BaslamaTarixi.Date;
                 gun <= dto.BitmeTarixi.Date;
                 gun = gun.AddDays(1))
            {
                if (skipBayramlar.Any(b => b.Tarix.Date == gun)) continue;

                var dav = await _unitOfWork.Repository<Davamiyyet>()
                    .GetirAsync(x => x.IsciId == dto.IsciId && x.Tarix.Date == gun);

                if (dav != null)
                {
                    // Icazeli idi → dövlət vəzifəsinə çevir
                    if (dav.Status == DavamiyyetStatus.Icazeli ||
                        dav.Status == DavamiyyetStatus.Qayib)
                    {
                        dav.Status      = DavamiyyetStatus.OdenisDovletVezifesi;
                        dav.MaasdanKes  = false;
                        dav.QayibSebebi = "Dövlət vəzifəsinin icrası (Maddə 173)";
                        await _unitOfWork.Repository<Davamiyyet>().YenileAsync(dav);
                        yenilenDavamiyyet++;
                    }
                }
                else
                {
                    await _unitOfWork.Repository<Davamiyyet>().YaratAsync(new Davamiyyet
                    {
                        IsciId      = dto.IsciId,
                        Tarix       = gun,
                        Status      = DavamiyyetStatus.OdenisDovletVezifesi,
                        MaasdanKes  = false,
                        QayibSebebi = "Dövlət vəzifəsinin icrası (Maddə 173)"
                    });
                    yenilenDavamiyyet++;
                }
            }

            await _unitOfWork.YaddaSaxlaAsync();
            await transaction.CommitAsync();

            var resultDto = new MezuniyyetDto
            {
                Id             = yeniMezuniyyet.Id,
                IsciId         = yeniMezuniyyet.IsciId,
                Nov            = yeniMezuniyyet.Nov,
                Status         = yeniMezuniyyet.Status,
                BaslamaTarixi  = yeniMezuniyyet.BaslamaTarixi,
                BitmeTarixi    = yeniMezuniyyet.BitmeTarixi,
                IsGunlerininSayi = yeniMezuniyyet.IsGunlerininSayi,
                Qeyd           = yeniMezuniyyet.Qeyd
            };

            return Result<MezuniyyetDto>.Ok(
                resultDto,
                $"Dövlət vəzifəsi korreksiyası tamamlandı. " +
                $"{dto.KorreksiyaGunSayi} gün illik balansa geri qaytarıldı. " +
                $"Davamiyyət: {yenilenDavamiyyet} qeyd yeniləndi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result<MezuniyyetDto>.Fail($"Korreksiya xətası: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════
    // DÖVLƏT VƏZİFƏSİ — sonradan sənəd əlavəsi
    // ══════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Result> SenedElavetEtAsync(int mezuniyyetId, string senedYollari)
    {
        if (string.IsNullOrWhiteSpace(senedYollari))
            return Result.Fail("Sənəd yolu boş ola bilməz.");

        var entity = await _unitOfWork.Repository<Mezuniyyet>()
            .GetirAsync(m => m.Id == mezuniyyetId && !m.Silinib);

        if (entity == null)
            return Result.Fail("Qeyd tapılmadı.");

        if (entity.Nov != MezuniyyetNovu.DovletVezifelerininIcrasi)
            return Result.Fail("Sənəd yalnız Dövlət Vəzifəsi korreksiya qeydlərinə əlavə edilə bilər.");

        // Mövcud sənədlər saxlanılır, yenilər üstünə əlavə olunur
        entity.SenedYolu = string.IsNullOrWhiteSpace(entity.SenedYolu)
            ? senedYollari
            : entity.SenedYolu.TrimEnd('|') + "|" + senedYollari.TrimStart('|');

        await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(entity);
        await _unitOfWork.YaddaSaxlaAsync();

        return Result.Ok("Sənəd(lər) uğurla əlavə edildi.");
    }

    // ── Balansı geri qaytarma köməkçisi (LIFO — ən son illərdən əvvəl) ───
    // Balansdan kesilmə FIFO ilə gedib (köhnə il → yeni il), geri qaytarma
    // LIFO ilə gedir (yeni il → köhnə il) — eyni məntiqlə geri alınır.
    private async Task BalansiGeriQaytarAsync(int isciId, MezuniyyetNovu nov, int gun)
    {
        if (gun <= 0) return;

        var repo = _unitOfWork.Repository<MezuniyyetBalans>();

        // IstifadeOlunanGun > 0 olan balansları ən son il əvvəl
        var balanslar = await repo.Query()
            .Where(b => !b.Silinib && b.IsciId == isciId &&
                        b.Nov == nov && b.IstifadeOlunanGun > 0)
            .OrderByDescending(b => b.Il)
            .ToListAsync();

        int qalanGeri = gun;
        foreach (var b in balanslar)
        {
            if (qalanGeri <= 0) break;
            int qaytar = Math.Min(b.IstifadeOlunanGun, qalanGeri);
            b.IstifadeOlunanGun -= qaytar;
            b.YenilenmeTarixi    = DateTime.Now;
            await repo.YenileAsync(b);
            qalanGeri -= qaytar;
        }
    }
}
