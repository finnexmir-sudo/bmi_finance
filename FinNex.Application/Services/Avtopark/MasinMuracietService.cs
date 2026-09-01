using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Avtopark;
using FinNex.Application.Interfaces.Avtopark;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Avtopark;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinNex.Application.Services.Avtopark;

/// <summary>
/// Maşın müraciəti iş axını.
///
/// <code>
/// İŞÇİ    → müraciət (Gozlemede)
/// RƏHBƏR  → Tesdiqlenib | ImtinaEdildi
/// KASSA   → «Çıxdı» (Cixib)   → bildiriş: işçi + təsdiqləyən rəhbər
/// KASSA   → «Gəldi» (Qayidib) → bildiriş: işçi + təsdiqləyən rəhbər
/// </code>
///
/// Müraciət edən özü RƏHBƏRdirsə rəhbər addımı ATLANIR — qayda TƏK YERDƏDİR
/// (<see cref="IlkinStatus"/>), ekran həmin metoddan gələn bayrağı oxuyur.
/// CLAUDE.md-dəki «Rol Prioriteti» tələsi (servis bir addımı atlayır, ekran
/// başqasını gizlədir) məhz belə qarşısı alınır.
/// </summary>
public class MasinMuracietService : IMasinMuracietService
{
    private readonly IUnitOfWork _uow;
    private readonly IBildirisRouter _bildiris;
    private readonly ILogger<MasinMuracietService> _logger;

    public MasinMuracietService(
        IUnitOfWork uow,
        IBildirisRouter bildiris,
        ILogger<MasinMuracietService> logger)
    {
        _uow = uow;
        _bildiris = bildiris;
        _logger = logger;
    }

    // ══ MARŞRUT — TƏK MƏNBƏ ═══════════════════════════════════════════════
    /// <summary>
    /// Yeni müraciətin ilkin statusu.
    ///
    /// ⚠️ BU METOD MARŞRUTUN YEGANƏ MƏNBƏYİDİR. Başqa yerdə (controller, view,
    /// DTO) eyni şərti təkrar YAZMA — biri dəyişəndə o biri köhnə qalar və
    /// ekran «müraciət haradadır» sualına yalan cavab verər.
    /// </summary>
    public static MasinMuracietStatus IlkinStatus(bool muracietSahibiRehberdirmi)
        => muracietSahibiRehberdirmi
            ? MasinMuracietStatus.Tesdiqlenib   // rəhbər öz müraciətini özü təsdiqləmir — addım atlanır
            : MasinMuracietStatus.Gozlemede;

    /// <summary>Rəhbər təsdiqi addımı bu müraciətdə varmı (göstərmə qatı üçün).</summary>
    public static bool RehberAddimiVar(bool muracietSahibiRehberdirmi)
        => IlkinStatus(muracietSahibiRehberdirmi) == MasinMuracietStatus.Gozlemede;

    // ── Ortaq sorğu (bütün siyahılar eyni Include-dan keçsin) ─────────────
    private IQueryable<MasinMuraciet> Baza() =>
        _uow.Repository<MasinMuraciet>().Query().AsNoTracking()
            .Include(x => x.Masin)
            .Include(x => x.Isci)
            .Include(x => x.Rehber)
            .Include(x => x.CixisQeydEden)
            .Include(x => x.QayidisQeydEden);

    private static string Ad(Isci? i) => i == null ? "" : $"{i.Ad} {i.Soyad}".Trim();

    /// <summary>
    /// «SS:DD» mətnini gecə yarısından keçən dəqiqəyə çevirir; format səhvdirsə
    /// `null`. İcazə modulundakı `parseTime` ilə eyni qayda — ora da rəqəm
    /// yazılır, «:» JS ilə avtomatik qoyulur.
    /// </summary>
    private static int? SaatiDeqiqeyeCevir(string? saat)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            (saat ?? "").Trim(), @"^(\d{1,2}):(\d{2})$");
        if (!m.Success) return null;

        var h = int.Parse(m.Groups[1].Value);
        var d = int.Parse(m.Groups[2].Value);
        if (h > 23 || d > 59) return null;

        return h * 60 + d;
    }

    private static MasinMuracietListDto Map(MasinMuraciet x, string? sobeAdi = null) => new()
    {
        Id = x.Id,
        MasinId = x.MasinId,
        MasinAdi = x.Masin == null
            ? ""
            : ($"{x.Masin.Marka} {x.Masin.Model}".Trim().Length > 0
                ? $"{x.Masin.Marka} {x.Masin.Model}".Trim()
                : x.Masin.DovletNomresi),
        MasinNomresi = x.Masin?.DovletNomresi ?? "",
        IsciId = x.IsciId,
        IsciAdSoyad = Ad(x.Isci),
        SobeAdi = sobeAdi,
        PlanBaslama = x.PlanBaslama,
        PlanBitme = x.PlanBitme,
        Meqsed = x.Meqsed,
        Marsrut = x.Marsrut,
        Status = x.Status,
        RehberAdi = Ad(x.Rehber),
        RehberTesdiqTarixi = x.RehberTesdiqTarixi,
        ImtinaSebebi = x.ImtinaSebebi,
        CixisTarixi = x.CixisTarixi,
        CixisQeydEdenAdi = Ad(x.CixisQeydEden),
        QayidisTarixi = x.QayidisTarixi,
        QayidisQeydEdenAdi = Ad(x.QayidisQeydEden)
    };

    /// <summary>
    /// Müraciətçilərin şöbə adları — AYRI sorğu ilə.
    ///
    /// Filtered `Include` işlətmirik: eyni kontekstdə ikinci tracking sorğusu
    /// naviqasiyaya bütün təyinatları yapışdıra bilir və filtr effektsiz qalır
    /// (CLAUDE.md — EF Core fixup tələsi). Aktiv təyinat şərtinin DÖRD hissəsi
    /// də burada var — `BitmeTarixi == null` DEYİL, `Aktivdir`.
    /// </summary>
    private async Task<Dictionary<int, string>> SobeAdlariAsync(IEnumerable<int> isciIdler)
    {
        var idler = isciIdler.Distinct().ToList();
        if (idler.Count == 0) return new Dictionary<int, string>();

        var teyinatlar = await _uow.Repository<IsciTeyinat>().Query().AsNoTracking()
            .Include(t => t.Departament)
            .Where(t => idler.Contains(t.IsciId) && t.Aktivdir && !t.Silinib)
            .ToListAsync();

        return teyinatlar
            .GroupBy(t => t.IsciId)
            .ToDictionary(
                g => g.Key,
                g => (g.FirstOrDefault(t => t.Esasdir) ?? g.First()).Departament?.Ad ?? "");
    }

    private async Task<IList<MasinMuracietListDto>> MapListAsync(List<MasinMuraciet> list)
    {
        var sobeler = await SobeAdlariAsync(list.Select(x => x.IsciId));
        return list
            .Select(x => Map(x, sobeler.TryGetValue(x.IsciId, out var s) ? s : null))
            .ToList();
    }

    // ══ OXUMA ═════════════════════════════════════════════════════════════

    public async Task<IList<MasinMuracietListDto>> GetIsciMuracietleriAsync(int isciId)
    {
        var list = await Baza()
            .Where(x => x.IsciId == isciId)
            .OrderByDescending(x => x.PlanBaslama)
            .ToListAsync();

        return await MapListAsync(list);
    }

    public async Task<IList<MasinMuracietListDto>> GetTesdiqGozleyenlerAsync()
    {
        var list = await Baza()
            .Where(x => x.Status == MasinMuracietStatus.Gozlemede)
            .OrderBy(x => x.PlanBaslama)
            .ToListAsync();

        return await MapListAsync(list);
    }

    public async Task<IList<MasinMuracietListDto>> GetKassaSiyahisiAsync()
    {
        var list = await Baza()
            .Where(x => x.Status == MasinMuracietStatus.Tesdiqlenib
                     || x.Status == MasinMuracietStatus.Cixib)
            // Çöldə olanlar əvvəl (təcili olan onlardır), sonra açar gözləyənlər.
            .OrderByDescending(x => x.Status == MasinMuracietStatus.Cixib)
            .ThenBy(x => x.PlanBaslama)
            .ToListAsync();

        return await MapListAsync(list);
    }

    public async Task<IList<MasinMuracietListDto>> GetAcigCixislarAsync()
    {
        var list = await Baza()
            .Where(x => x.Status == MasinMuracietStatus.Cixib)
            .OrderBy(x => x.CixisTarixi)
            .ToListAsync();

        return await MapListAsync(list);
    }

    public async Task<IList<MasinMuracietListDto>> GetJurnalAsync(DateTime? bas, DateTime? son, int? masinId)
    {
        var basTarix = (bas ?? DateTime.Today.AddDays(-30)).Date;
        // Son günün ÖZÜ də daxildir — `<= son` yazsaq həmin günün saatlı
        // qeydləri («24.08 10:00») kənarda qalardı.
        var sonTarix = (son ?? DateTime.Today).Date.AddDays(1);

        // PlanBitme 21.08.2026-dan yazılmır (nullable) — süzgəc TƏK NÖQTƏYƏ,
        // yəni planlaşdırılan çıxış anına görədir. Əvvəlki interval şərti
        // (`PlanBitme >= basTarix`) yeni qeydlərdə `null` olduğu üçün onları
        // jurnaldan səssizcə atardı.
        var sorgu = Baza().Where(x => x.PlanBaslama >= basTarix && x.PlanBaslama < sonTarix);

        if (masinId.HasValue && masinId.Value > 0)
            sorgu = sorgu.Where(x => x.MasinId == masinId.Value);

        var list = await sorgu.OrderByDescending(x => x.PlanBaslama).ToListAsync();
        return await MapListAsync(list);
    }

    public async Task<MasinMuracietListDto?> GetirAsync(int id)
    {
        var e = await Baza().FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return null;

        var sobeler = await SobeAdlariAsync(new[] { e.IsciId });
        return Map(e, sobeler.TryGetValue(e.IsciId, out var s) ? s : null);
    }

    // ══ ÜST-ÜSTƏ DÜŞMƏ — 21.08.2026-DAN YOXDUR ═══════════════════════════
    //
    // Əvvəl `TarixKonfliktiTapAsync` planlaşdırılan intervalları tutuşdururdu
    // (klassik `A1 <= B2 && A2 >= B1`). O yoxlama PLANLAŞDIRILAN BİTMƏ vaxtına
    // bağlı idi; bitmə sahəsi formadan çıxarıldığı üçün interval qalmadı və
    // yoxlama da götürüldü.
    //
    // ⚠️ MAŞIN YENƏ DƏ İKİQAT GÖTÜRÜLƏ BİLMİR — qoruyucu sadəcə BAŞQA
    // MƏRHƏLƏDƏDİR: `CixdiAsync` (Qoruyucu 4.1) bir maşının eyni anda iki açıq
    // çıxışına icazə vermir. Yəni açar verilən an maşın tutulur, «Gəldi» qeyd
    // edilənə qədər ikinci açar verilmir.
    //
    // NƏ DƏYİŞDİ: iki nəfər eyni maşına eyni günə müraciət edib hər ikisi
    // təsdiqlənə bilər; ikinci adam bunu MÜRACİƏT ANINDA yox, KASSADA öyrənir.
    // Bu, istifadəçinin açıq qərarıdır (21.08.2026).
    //
    // Bərpa lazım olsa: `TarixKonfliktiTapAsync`, `KonfliktMesaji` metodlarını
    // və `TutanStatuslar` sahəsini qaytar, sonra `YaratAsync` ilə
    // `RehberTesdiqAsync`-də çağırışları geri əlavə et — git: bu commitdən
    // əvvəlki nüsxə.

    // ══ YARATMA ═══════════════════════════════════════════════════════════

    public async Task<Result<int>> YaratAsync(MasinMuracietCreateDto dto, int userId)
    {
        if (dto.IsciId <= 0)
            return Result<int>.Fail("İşçi profili tapılmadı — müraciət yaradıla bilmədi.");

        if (dto.MasinId <= 0)
            return Result<int>.Fail("Maşın seçilməlidir.");

        if (string.IsNullOrWhiteSpace(dto.Meqsed))
            return Result<int>.Fail("Məqsəd yazılmalıdır.");

        var masin = await _uow.Repository<Masin>().GetirAsync(x => x.Id == dto.MasinId && !x.Silinib, izlemeden: true);
        if (masin == null)
            return Result<int>.Fail("Maşın tapılmadı.");

        if (masin.Status != MasinStatus.Aktiv)
            return Result<int>.Fail(
                $"«{masin.DovletNomresi}» hazırda {(masin.Status == MasinStatus.Temirde ? "təmirdədir" : "istifadədən çıxıb")} — müraciət yazıla bilməz.");

        // Tarix (təqvim) + Saat («HH:mm» mətn) → tək `PlanBaslama`.
        // Saat formatı DTO-da `RegularExpression` ilə də yoxlanılır, amma
        // forma birbaşa POST edilə bilər — ona görə burada TƏKRAR yoxlanılır.
        var saatDeq = SaatiDeqiqeyeCevir(dto.Saat);
        if (saatDeq == null)
            return Result<int>.Fail("Saat «SS:DD» formatında olmalıdır (məs. 10:00).");

        var planBaslama = dto.Tarix.Date.AddMinutes(saatDeq.Value);

        var ilkin = IlkinStatus(dto.MuracietSahibiRehberdirmi);

        var e = new MasinMuraciet
        {
            MasinId = dto.MasinId,
            IsciId = dto.IsciId,
            PlanBaslama = planBaslama,
            // PlanBitme QƏSDƏN yazılmır — bax entity sənədi (21.08.2026).
            Meqsed = dto.Meqsed.Trim(),
            Marsrut = dto.Marsrut?.Trim(),
            Status = ilkin,
            YaradanIcraciId = userId
        };

        // Rəhbər öz müraciətini yazırsa təsdiq izini DƏRHAL yazırıq — sonradan
        // «kim təsdiqlədi» sualı cavabsız qalmasın (jurnalda boş sətir olmasın).
        if (ilkin == MasinMuracietStatus.Tesdiqlenib)
        {
            e.RehberId = dto.IsciId;
            e.RehberTesdiqTarixi = DateTime.Now;
        }

        await _uow.Repository<MasinMuraciet>().YaratAsync(e);
        await _uow.YaddaSaxlaAsync();

        var isciAdi = await IsciAdiAsync(dto.IsciId);
        var metn = $"{masin.DovletNomresi} · {e.PlanBaslama:dd.MM.yyyy HH:mm} · {e.Meqsed}";

        if (ilkin == MasinMuracietStatus.Gozlemede)
        {
            await GuvenliBildirisAsync(() => _bildiris.NotifyRoleAsync(
                RoleNames.Rehber, BildirisNovu.MasinMuraciet,
                "Yeni maşın müraciəti",
                $"{isciAdi} — {metn}",
                $"/Avtopark/Tesdiq/Index"));
        }
        else
        {
            // Rəhbər özü — addım atlandı, açar üçün kassa xəbərdar edilir.
            await GuvenliBildirisAsync(() => _bildiris.NotifyRoleAsync(
                RoleNames.Kassa, BildirisNovu.MasinTesdiq,
                "Maşın açarı gözləyir",
                $"{isciAdi} — {metn}",
                $"/Avtopark/Kassa/Index"));
        }

        return Result<int>.Ok(e.Id,
            ilkin == MasinMuracietStatus.Tesdiqlenib
                ? "Müraciət yaradıldı və təsdiqləndi — açar üçün kassaya müraciət edin."
                : "Müraciət göndərildi — rəhbər təsdiqi gözlənilir.");
    }

    // ══ EZAMİYYƏT BAĞLANTISI (01.09.2026) ═════════════════════════════════

    /// <summary>Maşın mövcuddur və götürülə bilər? (ezamiyyət formasından çağırılır)</summary>
    public async Task<Result> MasinSecimiYoxlaAsync(int masinId)
    {
        if (masinId <= 0) return Result.Fail("Maşın seçilməlidir.");

        var masin = await _uow.Repository<Masin>()
            .GetirAsync(x => x.Id == masinId && !x.Silinib, izlemeden: true);
        if (masin == null) return Result.Fail("Maşın tapılmadı.");

        if (masin.Status != MasinStatus.Aktiv)
            return Result.Fail(
                $"«{masin.DovletNomresi}» hazırda " +
                $"{(masin.Status == MasinStatus.Temirde ? "təmirdədir" : "istifadədən çıxıb")} — seçilə bilməz.");

        return Result.Ok();
    }

    public async Task<Result<int>> EzamiyyetdenYaratAsync(
        int ezamiyyetId, int masinId, int isciId,
        DateTime planBaslama, string meqsed, string? marsrut,
        int rehberIsciId, int userId)
    {
        var yoxla = await MasinSecimiYoxlaAsync(masinId);
        if (!yoxla.Success) return Result<int>.Fail(yoxla.Message ?? "Maşın seçilə bilmədi.");

        if (string.IsNullOrWhiteSpace(meqsed))
            return Result<int>.Fail("Məqsəd boşdur — ezamiyyətin başlığı oxunmadı.");

        // Eyni ezamiyyət üçün ikinci müraciət yaranmasın: təsdiq düyməsi iki dəfə
        // basıla bilər, yaxud ləğv → yenidən təsdiq axını olar. Ləğv edilmiş
        // sətir saymır — o, qəsdən bağlanıb və yenisi yazıla bilər.
        var movcud = await _uow.Repository<MasinMuraciet>().Query().AsNoTracking()
            .FirstOrDefaultAsync(x => x.EzamiyyetMuracietId == ezamiyyetId
                                   && x.Status != MasinMuracietStatus.LegvEdildi
                                   && x.Status != MasinMuracietStatus.ImtinaEdildi
                                   && !x.Silinib);
        if (movcud != null) return Result<int>.Ok(movcud.Id, "Bu ezamiyyət üçün maşın müraciəti onsuz da var.");

        var e = new MasinMuraciet
        {
            MasinId = masinId,
            IsciId = isciId,
            PlanBaslama = planBaslama,
            // PlanBitme QƏSDƏN yazılmır — bax entity sənədi (21.08.2026).
            Meqsed = meqsed.Trim(),
            Marsrut = string.IsNullOrWhiteSpace(marsrut) ? null : marsrut.Trim(),
            EzamiyyetMuracietId = ezamiyyetId,

            // Rəhbər ezamiyyəti təsdiqlədi — maşın addımı ATLANIR.
            Status = MasinMuracietStatus.Tesdiqlenib,
            RehberId = rehberIsciId,
            RehberTesdiqTarixi = DateTime.Now,

            YaradanIcraciId = userId
        };

        await _uow.Repository<MasinMuraciet>().YaratAsync(e);
        await _uow.YaddaSaxlaAsync();

        var masinAdi = (await _uow.Repository<Masin>()
            .GetirAsync(x => x.Id == masinId, izlemeden: true))?.DovletNomresi ?? "—";
        var isciAdi = await IsciAdiAsync(isciId);

        await GuvenliBildirisAsync(() => _bildiris.NotifyRoleAsync(
            RoleNames.Kassa, BildirisNovu.MasinTesdiq,
            "Maşın açarı gözləyir (ezamiyyət)",
            $"{isciAdi} — {masinAdi} · {planBaslama:dd.MM.yyyy HH:mm} · {meqsed}",
            "/Avtopark/Kassa/Index"));

        return Result<int>.Ok(e.Id, "Ezamiyyət üçün maşın müraciəti yaradıldı.");
    }

    public async Task<bool> EzamiyyetLegvindeMasiniLegvEtAsync(int ezamiyyetId, int userId)
    {
        var e = await _uow.Repository<MasinMuraciet>()
            .GetirAsync(x => x.EzamiyyetMuracietId == ezamiyyetId
                          && x.Status != MasinMuracietStatus.LegvEdildi
                          && x.Status != MasinMuracietStatus.ImtinaEdildi
                          && !x.Silinib);
        if (e == null) return true;   // bağlı müraciət yoxdur — ediləsi bir şey yoxdur

        // Açar verilib (Cixib) və ya artıq qayıdıb — jurnal sətri toxunulmazdır.
        if (e.Status != MasinMuracietStatus.Gozlemede &&
            e.Status != MasinMuracietStatus.Tesdiqlenib)
            return false;

        e.Status = MasinMuracietStatus.LegvEdildi;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi = DateTime.Now;

        await _uow.Repository<MasinMuraciet>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();

        // Ad ƏVVƏLCƏ alınır — `GuvenliBildirisAsync` sinxron lambda qəbul edir,
        // onun içində `await` yazmaq olmaz (CS4034).
        var isciAdi = await IsciAdiAsync(e.IsciId);

        await GuvenliBildirisAsync(() => _bildiris.NotifyRoleAsync(
            RoleNames.Kassa, BildirisNovu.MasinImtina,
            "Maşın müraciəti ləğv edildi",
            $"Ezamiyyət ləğv olundu — {isciAdi} üçün maşın açarı lazım deyil.",
            "/Avtopark/Kassa/Index"));

        return true;
    }

    // ══ RƏHBƏR MƏRHƏLƏSİ ══════════════════════════════════════════════════

    public async Task<Result> TesdiqEtAsync(int id, int rehberIsciId, int userId)
    {
        var e = await _uow.Repository<MasinMuraciet>()
            .GetirAsync(x => x.Id == id && !x.Silinib, include: q => q.Include(m => m.Masin));

        if (e == null) return Result.Fail("Müraciət tapılmadı.");

        if (e.Status != MasinMuracietStatus.Gozlemede)
            return Result.Fail($"Bu müraciət artıq «{StatusMetni(e.Status)}» vəziyyətindədir.");

        if (e.Masin != null && e.Masin.Status != MasinStatus.Aktiv)
            return Result.Fail("Maşın artıq aktiv deyil (təmirdə / istifadədən çıxıb) — təsdiq edilə bilməz.");

        e.Status = MasinMuracietStatus.Tesdiqlenib;
        e.RehberId = rehberIsciId;
        e.RehberTesdiqTarixi = DateTime.Now;
        e.ImtinaSebebi = null;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi = DateTime.Now;

        await _uow.Repository<MasinMuraciet>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();

        var metn = $"{e.Masin?.DovletNomresi} · {e.PlanBaslama:dd.MM.yyyy HH:mm}";
        // Ad ƏVVƏLCƏDƏN alınır: `GuvenliBildirisAsync` sinxron lambda qəbul edir,
        // onun içində `await` yazmaq mümkün deyil.
        var isciAdi = await IsciAdiAsync(e.IsciId);

        await GuvenliBildirisAsync(() => _bildiris.NotifyIsciAsync(
            e.IsciId, BildirisNovu.MasinTesdiq,
            "Maşın müraciətiniz təsdiqləndi",
            $"{metn} — açar üçün kassaya müraciət edin.",
            "/Avtopark/Muraciet/Index"));

        await GuvenliBildirisAsync(() => _bildiris.NotifyRoleAsync(
            RoleNames.Kassa, BildirisNovu.MasinTesdiq,
            "Maşın açarı gözləyir",
            $"{isciAdi} — {metn}",
            "/Avtopark/Kassa/Index"));

        return Result.Ok("Müraciət təsdiqləndi.");
    }

    public async Task<Result> ImtinaEtAsync(int id, int rehberIsciId, string? sebeb, int userId)
    {
        if (string.IsNullOrWhiteSpace(sebeb))
            return Result.Fail("İmtina səbəbi yazılmalıdır.");

        var e = await _uow.Repository<MasinMuraciet>()
            .GetirAsync(x => x.Id == id && !x.Silinib, include: q => q.Include(m => m.Masin));

        if (e == null) return Result.Fail("Müraciət tapılmadı.");

        // Təsdiqlənmiş müraciətdən də imtina etmək olar (açar hələ verilməyibsə) —
        // rəhbər fikrini dəyişə bilər. Açar verildikdən sonra YOX: maşın çöldədir,
        // «imtina» jurnalı yalan göstərərdi.
        if (e.Status is not (MasinMuracietStatus.Gozlemede or MasinMuracietStatus.Tesdiqlenib))
            return Result.Fail($"Bu müraciət artıq «{StatusMetni(e.Status)}» vəziyyətindədir — imtina edilə bilməz.");

        e.Status = MasinMuracietStatus.ImtinaEdildi;
        e.RehberId = rehberIsciId;
        e.RehberTesdiqTarixi = DateTime.Now;
        e.ImtinaSebebi = sebeb.Trim();
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi = DateTime.Now;

        await _uow.Repository<MasinMuraciet>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();

        await GuvenliBildirisAsync(() => _bildiris.NotifyIsciAsync(
            e.IsciId, BildirisNovu.MasinImtina,
            "Maşın müraciətinizdən imtina edildi",
            $"{e.Masin?.DovletNomresi} · {e.PlanBaslama:dd.MM.yyyy HH:mm} — səbəb: {e.ImtinaSebebi}",
            "/Avtopark/Muraciet/Index"));

        return Result.Ok("İmtina qeyd edildi.");
    }

    public async Task<Result> LegvEtAsync(int id, int isciId, int userId)
    {
        var e = await _uow.Repository<MasinMuraciet>()
            .GetirAsync(x => x.Id == id && !x.Silinib, include: q => q.Include(m => m.Masin));

        if (e == null) return Result.Fail("Müraciət tapılmadı.");

        if (e.IsciId != isciId)
            return Result.Fail("Yalnız öz müraciətinizi ləğv edə bilərsiniz.");

        if (e.Status is not (MasinMuracietStatus.Gozlemede or MasinMuracietStatus.Tesdiqlenib))
            return Result.Fail("Açar verildikdən sonra müraciət ləğv edilə bilməz.");

        e.Status = MasinMuracietStatus.LegvEdildi;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi = DateTime.Now;

        await _uow.Repository<MasinMuraciet>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();

        return Result.Ok("Müraciət ləğv edildi.");
    }

    // ══ KASSA MƏRHƏLƏSİ ═══════════════════════════════════════════════════

    public async Task<Result> CixdiAsync(int id, int qeydEdenIsciId, int userId)
    {
        var e = await _uow.Repository<MasinMuraciet>()
            .GetirAsync(x => x.Id == id && !x.Silinib, include: q => q.Include(m => m.Masin));

        if (e == null) return Result.Fail("Müraciət tapılmadı.");

        if (e.Status != MasinMuracietStatus.Tesdiqlenib)
            return Result.Fail(
                e.Status == MasinMuracietStatus.Cixib
                    ? "Bu müraciət üzrə açar artıq verilib."
                    : $"Açar yalnız təsdiqlənmiş müraciətə verilə bilər (hazırkı vəziyyət: «{StatusMetni(e.Status)}»).");

        // QOruyucu 4.1 — bir maşının EYNİ ANDA iki açıq çıxışı ola bilməz.
        // Bu yoxlama olmasa jurnal iki paralel çıxış göstərər və heç bir xəta
        // çıxmaz; «Gəldi» düyməsi də hansı sətri bağlayacağını bilməz.
        var acig = await _uow.Repository<MasinMuraciet>().Query().AsNoTracking()
            .Include(x => x.Isci)
            .FirstOrDefaultAsync(x => x.MasinId == e.MasinId
                                   && x.Id != e.Id
                                   && x.Status == MasinMuracietStatus.Cixib);

        if (acig != null)
            return Result.Fail(
                $"Bu maşın hazırda çöldədir — {Ad(acig.Isci)} " +
                $"({acig.CixisTarixi:dd.MM.yyyy HH:mm}-dan). Əvvəlcə «Gəldi» qeyd edilməlidir.");

        e.Status = MasinMuracietStatus.Cixib;
        e.CixisTarixi = DateTime.Now;
        e.CixisQeydEdenId = qeydEdenIsciId;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi = DateTime.Now;

        await _uow.Repository<MasinMuraciet>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();

        await BildirIsciVeRehberAsync(e, BildirisNovu.MasinCixis,
            "Maşın çıxdı",
            $"{e.Masin?.DovletNomresi} — açar verildi ({e.CixisTarixi:dd.MM.yyyy HH:mm}).");

        return Result.Ok($"«Çıxdı» qeyd edildi — {e.CixisTarixi:HH:mm}.");
    }

    public async Task<Result> GeldiAsync(int id, int qeydEdenIsciId, int userId)
    {
        var e = await _uow.Repository<MasinMuraciet>()
            .GetirAsync(x => x.Id == id && !x.Silinib, include: q => q.Include(m => m.Masin));

        if (e == null) return Result.Fail("Müraciət tapılmadı.");

        if (e.Status != MasinMuracietStatus.Cixib)
            return Result.Fail($"Bu müraciət «Çıxıb» vəziyyətində deyil (hazırkı vəziyyət: «{StatusMetni(e.Status)}»).");

        var indi = DateTime.Now;

        // Qoruyucu 4.3 — qayıdış çıxışdan əvvəl ola bilməz. Hər iki tarix
        // `DateTime.Now`-dan gəldiyi üçün normalda mümkün deyil, amma server
        // saatı geri çəkilərsə jurnalda mənfi müddət yaranardı.
        if (e.CixisTarixi.HasValue && indi < e.CixisTarixi.Value)
            return Result.Fail(
                $"Qayıdış vaxtı çıxış vaxtından ({e.CixisTarixi:dd.MM.yyyy HH:mm}) əvvəl ola bilməz — server saatını yoxlayın.");

        e.Status = MasinMuracietStatus.Qayidib;
        e.QayidisTarixi = indi;
        e.QayidisQeydEdenId = qeydEdenIsciId;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi = indi;

        await _uow.Repository<MasinMuraciet>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();

        var muddet = e.CixisTarixi.HasValue ? indi - e.CixisTarixi.Value : (TimeSpan?)null;
        var muddetMetni = muddet.HasValue
            ? $" · {(int)muddet.Value.TotalHours} saat {muddet.Value.Minutes} dəqiqə"
            : "";

        await BildirIsciVeRehberAsync(e, BildirisNovu.MasinQayidis,
            "Maşın qayıtdı",
            $"{e.Masin?.DovletNomresi} — açar geri alındı ({indi:dd.MM.yyyy HH:mm}){muddetMetni}.");

        return Result.Ok($"«Gəldi» qeyd edildi — {indi:HH:mm}{muddetMetni}.");
    }

    // ══ KÖMƏKÇİLƏR ════════════════════════════════════════════════════════

    /// <summary>
    /// İşçiyə və (fərqlidirsə) təsdiqləyən rəhbərə bildiriş.
    ///
    /// Rəhbər öz müraciətini yazıbsa `RehberId == IsciId` olur — şərt olmasa
    /// eyni adama EYNİ bildiriş iki dəfə gedərdi.
    /// Bildirişlər ARDICIL yazılır (`Task.WhenAll` YOX) — `BildirisService`
    /// sorğunun ortaq `DbContext`-ini işlədir, o isə thread-safe deyil
    /// (CLAUDE.md — paralel yazı və ölü bildiriş).
    /// </summary>
    private async Task BildirIsciVeRehberAsync(MasinMuraciet e, BildirisNovu nov, string bashliq, string metn)
    {
        await GuvenliBildirisAsync(() => _bildiris.NotifyIsciAsync(
            e.IsciId, nov, bashliq, metn, "/Avtopark/Muraciet/Index"));

        if (e.RehberId.HasValue && e.RehberId.Value != e.IsciId)
        {
            var isciAdi = await IsciAdiAsync(e.IsciId);
            await GuvenliBildirisAsync(() => _bildiris.NotifyIsciAsync(
                e.RehberId.Value, nov, bashliq, $"{isciAdi} — {metn}", "/Avtopark/Tesdiq/Index"));
        }
    }

    /// <summary>
    /// Bildiriş xətası əsas əməliyyatı POZMUR, amma İZSİZ də qalmır.
    /// Boş `catch` bir dəfə 17 gün gizli qalmış səhvə səbəb olub (CLAUDE.md).
    /// </summary>
    private async Task GuvenliBildirisAsync(Func<Task> gonder)
    {
        try { await gonder(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Avtopark bildirişi göndərilmədi.");
        }
    }

    private async Task<string> IsciAdiAsync(int isciId)
    {
        var i = await _uow.Repository<Isci>().GetirAsync(x => x.Id == isciId, izlemeden: true);
        return Ad(i);
    }

    private static string StatusMetni(MasinMuracietStatus s) => s switch
    {
        MasinMuracietStatus.Gozlemede => "Gözləmədə",
        MasinMuracietStatus.Tesdiqlenib => "Təsdiqlənib",
        MasinMuracietStatus.Cixib => "Çıxıb",
        MasinMuracietStatus.Qayidib => "Qayıdıb",
        MasinMuracietStatus.ImtinaEdildi => "İmtina edildi",
        MasinMuracietStatus.LegvEdildi => "Ləğv edildi",
        _ => s.ToString()
    };
}
