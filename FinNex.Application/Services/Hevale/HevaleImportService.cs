using System.Globalization;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Hevale;
using FinNex.Application.Interfaces.Hevale;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Domain.Entities.Hevale;
using FinNex.Domain.Entities.Sorgular;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Hevale;

public class HevaleImportService : IHevaleImportService
{
    private readonly IUnitOfWork _uow;
    private readonly IOracleService _oracle;

    // Bir ildəki sətir sayı üçün geniş ehtiyat; nəticədə LimiteCatdi ilə yoxlanılır.
    private const int IlLimiti = 20000;

    // Oracle sorğuları Admin → Oracle Sorğular-da saxlanılır (layihə qaydası),
    // ADLAR ASCII-dir ki, SSMS-in Azərbaycan hərfi tələsi axtarışı sındırmasın.
    // Seed: docs/sql/hevale/Hevale_OracleSorgular.sql
    private const string SorguGedenSaylar   = "HEVALE_IDXAL_GEDEN_SAYLAR";
    private const string SorguGedenSetirler = "HEVALE_IDXAL_GEDEN_SETIRLER";
    private const string SorguGelenSaylar   = "HEVALE_IDXAL_GELEN_SAYLAR";
    private const string SorguGelenSetirler = "HEVALE_IDXAL_GELEN_SETIRLER";

    // Sətir sorğularındakı token — kod onu il şərti ilə əvəz edir.
    private const string IlToken = "{IL_SERTI}";

    // İdxal sətirləri sütun ADINA görə entity sahələrinə yazılır. Sorğu redaktə
    // olunub bir sütun itsə, xəta OLMUR — sahə səssizcə boş yazılır. Ona görə
    // yazmazdan ƏVVƏL sütunlar yoxlanılır.
    private static readonly string[] GedenSutunlar =
    {
        "HEV_NOM", "HES_NOM", "SAA", "TIP_RES", "MEBLEG", "VAL_TIP", "TARIX",
        "MEN_OLKE", "CONTRAC_NOM", "DECLAR_NOM", "ARAYIS", "OLKE", "HEV_TIP",
        "GON_TIP", "AL_BANK", "ICRA"
    };
    private static readonly string[] GelenSutunlar =
    {
        "HEV_NOM", "HES_NOM", "SAA", "TIP_RES", "MEBLEG", "VAL_TIP", "TARIX",
        "MEN_OLKE", "HEV_TIP", "DEC", "DEC_NOM", "GEL_OLKE", "GON_TIP",
        "AL_BANK", "ICRA"
    };

    // MEBLEG sütunlarının SQL Server dəqiqliyi (AppDbContext): gedən 14,2 — gələn 10,2.
    // Bundan böyük məbləğ SaveChanges-də bütün ili dağıdardı; belə sətir atılır və
    // `Xetali` kimi sayılır (vəziyyət ekranında "çatışmayan" olaraq görünür).
    private const decimal GedenMebleqHedd = 1_000_000_000_000m;   // 10^(14-2)
    private const decimal GelenMebleqHedd = 100_000_000m;         // 10^(10-2)

    public HevaleImportService(IUnitOfWork uow, IOracleService oracle)
    {
        _uow = uow;
        _oracle = oracle;
    }

    // ── Vəziyyət ───────────────────────────────────────────────────────────
    public async Task<Result<HevaleImportVeziyyetDto>> VeziyyetAsync(CancellationToken ct = default)
    {
        try
        {
            var dto = new HevaleImportVeziyyetDto();

            var gedOra = await OracleIlSaylariAsync(SorguGedenSaylar, ct);
            var gelOra = await OracleIlSaylariAsync(SorguGelenSaylar, ct);

            // Silinmiş sətirlər də sayılır: bu, İDXALIN tamlığını göstərən ekrandır,
            // biznes siyahısı deyil. İdxal da açar mövcuddursa keçir (silinmiş qeydi
            // diriltmir) — iki tərəf beləcə eyni cavabı verir.
            var gedFin = (await _uow.Repository<GedenHevale>().HamisiniGetirAsync(izlemeden: true))
                .GroupBy(x => x.Tarix.HasValue ? x.Tarix.Value.Year : (int?)null)
                .Select(g => (Il: g.Key, Say: g.Count())).ToList();

            var gelFin = (await _uow.Repository<GelenHevale>().HamisiniGetirAsync(izlemeden: true))
                .GroupBy(x => x.Tarix.HasValue ? x.Tarix.Value.Year : (int?)null)
                .Select(g => (Il: g.Key, Say: g.Count())).ToList();

            dto.Geden = Birlesdir(gedOra, gedFin);
            dto.Gelen = Birlesdir(gelOra, gelFin);

            return Result<HevaleImportVeziyyetDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            return Result<HevaleImportVeziyyetDto>.Fail($"Vəziyyət alınmadı: {ex.Message}");
        }
    }

    // DİQQƏT: burada `Dictionary<int?, int>` İSTİFADƏ EDİLMİR — Dictionary NULL açar
    // qəbul etmir (ArgumentNullException), Oracle-da isə tarixi boş sətir ola bilər
    // və qruplaşdırma onu NULL açarlı sətir kimi qaytarır. Ona görə siyahı + LINQ.
    private static List<HevaleImportIlDto> Birlesdir(
        List<(int? Il, int Say)> oracle, List<(int? Il, int Say)> finnex)
    {
        return oracle.Select(x => x.Il).Concat(finnex.Select(x => x.Il)).Distinct()
            .Select(il => new HevaleImportIlDto
            {
                Il        = il,
                OracleSay = oracle.FirstOrDefault(x => x.Il == il).Say,
                FinNexSay = finnex.FirstOrDefault(x => x.Il == il).Say
            })
            .OrderBy(x => x.Il == null).ThenBy(x => x.Il)   // boş il sonda
            .ToList();
    }

    private async Task<List<(int? Il, int Say)>> OracleIlSaylariAsync(string sorguAdi, CancellationToken ct)
    {
        var sql = await SorguMetniAsync(sorguAdi);
        var setirler = await _oracle.SelectAsync(sql, 500, ct);

        var yoxla = SutunYoxla(setirler, "IL", "SAY");
        if (!yoxla.Success) throw new InvalidOperationException($"«{sorguAdi}»: {yoxla.Message}");

        return setirler
            .Select(s => (Il: Tam(s, "IL"), Say: Tam(s, "SAY") ?? 0))
            .ToList();
    }

    // ── Saxlanılan sorğular ────────────────────────────────────────────────
    // Layihə qaydası: Oracle sorğuları kodda deyil, `OracleSorgular` cədvəlində
    // saxlanılır və Admin → Oracle Sorğular səhifəsindən redaktə oluna bilir.
    // Ad tapılmasa idxal BAŞLAMIR.
    private async Task<string> SorguMetniAsync(string sorguAdi)
    {
        var sorgu = (await _uow.Repository<OracleSorgu>()
                .HamisiniGetirAsync(x => !x.Silinib && x.Aktiv, izlemeden: true))
            .FirstOrDefault(x => string.Equals((x.SorguAdi ?? "").Trim(), sorguAdi,
                StringComparison.OrdinalIgnoreCase));

        if (sorgu == null || string.IsNullOrWhiteSpace(sorgu.SorguMetni))
            throw new InvalidOperationException(
                $"Oracle sorğusu tapılmadı: «{sorguAdi}». " +
                "Admin → Oracle Sorğular-da yaradılmalıdır " +
                "(seed skripti: docs/sql/hevale/Hevale_OracleSorgular.sql).");

        return sorgu.SorguMetni;
    }

    // Sorğu nəticəsində tələb olunan sütunların hamısı varmı? Yoxdursa BAZAYA HEÇ NƏ
    // YAZILMIR — səhv redaktə səssiz data zədəsi yox, açıq xəta verməlidir.
    private static Result SutunYoxla(
        IList<Dictionary<string, object?>> setirler, params string[] telebOlunan)
    {
        if (setirler.Count == 0) return Result.Ok();

        var movcud = new HashSet<string>(setirler[0].Keys, StringComparer.OrdinalIgnoreCase);
        var catismayan = telebOlunan.Where(c => !movcud.Contains(c)).ToList();

        return catismayan.Count == 0
            ? Result.Ok()
            : Result.Fail(
                $"sorğu nəticəsində sütun çatışmır: {string.Join(", ", catismayan)}. " +
                "Sorğu redaktə olunub? Bazaya heç nə yazılmadı.");
    }

    // ── İdxal ──────────────────────────────────────────────────────────────
    public async Task<Result<HevaleImportNeticeDto>> IlIdxalAsync(
        string jurnal, int? il, CancellationToken ct = default)
    {
        jurnal = (jurnal ?? "").Trim().ToLowerInvariant();
        if (jurnal != "geden" && jurnal != "gelen")
            return Result<HevaleImportNeticeDto>.Fail("Jurnal 'geden' və ya 'gelen' olmalıdır.");

        try
        {
            return jurnal == "geden"
                ? await GedenIlAsync(il, ct)
                : await GelenIlAsync(il, ct);
        }
        catch (Exception ex)
        {
            return Result<HevaleImportNeticeDto>.Fail($"İdxal xətası: {ex.Message}");
        }
    }

    // Həvalədə IL sütunu yoxdur — il TARIX-dən çıxarılır.
    private static string IlSerti(int? il) =>
        il.HasValue ? $"EXTRACT(YEAR FROM tarix) = {il.Value}" : "tarix IS NULL";

    private async Task<Result<HevaleImportNeticeDto>> GedenIlAsync(int? il, CancellationToken ct)
    {
        var netice = new HevaleImportNeticeDto { Jurnal = "geden", Il = il };

        var sql = (await SorguMetniAsync(SorguGedenSetirler)).Replace(IlToken, IlSerti(il));
        var setirler = await _oracle.SelectAsync(sql, IlLimiti, ct);

        netice.Oxunan = setirler.Count;
        netice.LimiteCatdi = setirler.Count >= IlLimiti;

        var sutunYoxlama = SutunYoxla(setirler, GedenSutunlar);
        if (!sutunYoxlama.Success)
            return Result<HevaleImportNeticeDto>.Fail($"«{SorguGedenSetirler}» {sutunYoxlama.Message}");

        // Mövcud açarlar (HEV_NOM, həmin il daxilində) — təkrar idxalda dublikat olmasın.
        // `Silinib` filtri QƏSDƏN yoxdur: istifadəçi silmiş qeyd təkrar idxalda dirilməməlidir.
        var movcud = (await _uow.Repository<GedenHevale>().HamisiniGetirAsync(
                x => (il == null && x.Tarix == null)
                  || (il != null && x.Tarix != null && x.Tarix.Value.Year == il),
                izlemeden: true))
            .Where(x => !string.IsNullOrWhiteSpace(x.HevNom))
            .Select(x => x.HevNom!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var s in setirler)
        {
            ct.ThrowIfCancellationRequested();

            var hevNom = Metn(s, "HEV_NOM", 10);
            if (hevNom == null) { netice.Xetali++; continue; }
            if (movcud.Contains(hevNom)) { netice.Kecilen++; continue; }

            var mebleg = Ondalik(s, "MEBLEG");
            if (mebleg.HasValue && Math.Abs(mebleg.Value) >= GedenMebleqHedd) { netice.Xetali++; continue; }

            await _uow.Repository<GedenHevale>().YaratAsync(new GedenHevale
            {
                HevNom     = hevNom,
                HesNom     = Metn(s, "HES_NOM", 20),
                Saa        = Metn(s, "SAA", 50),
                TipRes     = Metn(s, "TIP_RES", 16),
                Mebleg     = mebleg,
                ValTip     = Metn(s, "VAL_TIP", 10),
                Tarix      = Tarix(s, "TARIX"),
                MenOlke    = Metn(s, "MEN_OLKE", 40),
                ContracNom = Metn(s, "CONTRAC_NOM", 15),
                DeclarNom  = Metn(s, "DECLAR_NOM", 15),
                Arayis     = Qisa(s, "ARAYIS"),
                Olke       = Metn(s, "OLKE", 40),
                HevTip     = Metn(s, "HEV_TIP", 254),
                GonTip     = Metn(s, "GON_TIP", 20),
                AlBank     = Metn(s, "AL_BANK", 40),
                Icra       = Qisa(s, "ICRA")
            });
            movcud.Add(hevNom);
            netice.Elave++;
        }

        if (netice.Elave > 0) await _uow.YaddaSaxlaAsync();
        return Result<HevaleImportNeticeDto>.Ok(netice, Xulase(netice));
    }

    private async Task<Result<HevaleImportNeticeDto>> GelenIlAsync(int? il, CancellationToken ct)
    {
        var netice = new HevaleImportNeticeDto { Jurnal = "gelen", Il = il };

        var sql = (await SorguMetniAsync(SorguGelenSetirler)).Replace(IlToken, IlSerti(il));
        var setirler = await _oracle.SelectAsync(sql, IlLimiti, ct);

        netice.Oxunan = setirler.Count;
        netice.LimiteCatdi = setirler.Count >= IlLimiti;

        var sutunYoxlama = SutunYoxla(setirler, GelenSutunlar);
        if (!sutunYoxlama.Success)
            return Result<HevaleImportNeticeDto>.Fail($"«{SorguGelenSetirler}» {sutunYoxlama.Message}");

        var movcud = (await _uow.Repository<GelenHevale>().HamisiniGetirAsync(
                x => (il == null && x.Tarix == null)
                  || (il != null && x.Tarix != null && x.Tarix.Value.Year == il),
                izlemeden: true))
            .Where(x => !string.IsNullOrWhiteSpace(x.HevNom))
            .Select(x => x.HevNom!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var s in setirler)
        {
            ct.ThrowIfCancellationRequested();

            var hevNom = Metn(s, "HEV_NOM", 10);
            if (hevNom == null) { netice.Xetali++; continue; }
            if (movcud.Contains(hevNom)) { netice.Kecilen++; continue; }

            var mebleg = Ondalik(s, "MEBLEG");
            if (mebleg.HasValue && Math.Abs(mebleg.Value) >= GelenMebleqHedd) { netice.Xetali++; continue; }

            await _uow.Repository<GelenHevale>().YaratAsync(new GelenHevale
            {
                HevNom  = hevNom,
                HesNom  = Metn(s, "HES_NOM", 20),
                Saa     = Metn(s, "SAA", 50),
                TipRes  = Metn(s, "TIP_RES", 16),
                Mebleg  = mebleg,
                ValTip  = Metn(s, "VAL_TIP", 10),
                Tarix   = Tarix(s, "TARIX"),
                MenOlke = Metn(s, "MEN_OLKE", 40),
                HevTip  = Metn(s, "HEV_TIP", 254),
                Dec     = Uzun(s, "DEC"),
                DecNom  = Metn(s, "DEC_NOM", 30),
                GelOlke = Metn(s, "GEL_OLKE", 40),
                GonTip  = Metn(s, "GON_TIP", 20),
                AlBank  = Metn(s, "AL_BANK", 250),
                Icra    = Qisa(s, "ICRA")
            });
            movcud.Add(hevNom);
            netice.Elave++;
        }

        if (netice.Elave > 0) await _uow.YaddaSaxlaAsync();
        return Result<HevaleImportNeticeDto>.Ok(netice, Xulase(netice));
    }

    private static string Xulase(HevaleImportNeticeDto n)
    {
        var ilAd = n.Il?.ToString(CultureInfo.InvariantCulture) ?? "tarixsiz";
        var metn = $"{n.Jurnal} {ilAd}: {n.Oxunan} oxundu, {n.Elave} əlavə, {n.Kecilen} keçildi";
        if (n.Xetali > 0) metn += $", {n.Xetali} xətalı (№ boş və ya məbləğ hədddən böyük)";
        if (n.LimiteCatdi) metn += " — DİQQƏT: sətir limitinə çatdı, təkrar işlədin";
        return metn + ".";
    }

    // ── Oracle dəyər çeviriciləri ──────────────────────────────────────────
    // ODP.NET NUMBER → decimal, DATE → DateTime, VARCHAR2 → string qaytarır.
    private static int? Tam(IDictionary<string, object?> s, string sutun)
    {
        if (!s.TryGetValue(sutun, out var v) || v == null) return null;
        try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
        catch { return null; }
    }

    private static long? Uzun(IDictionary<string, object?> s, string sutun)
    {
        if (!s.TryGetValue(sutun, out var v) || v == null) return null;
        try { return Convert.ToInt64(v, CultureInfo.InvariantCulture); }
        catch { return null; }
    }

    private static short? Qisa(IDictionary<string, object?> s, string sutun)
    {
        if (!s.TryGetValue(sutun, out var v) || v == null) return null;
        try { return Convert.ToInt16(v, CultureInfo.InvariantCulture); }
        catch { return null; }   // smallint-ə sığmayan dəyər — sahə boş qalır, sətir atılmır
    }

    private static decimal? Ondalik(IDictionary<string, object?> s, string sutun)
    {
        if (!s.TryGetValue(sutun, out var v) || v == null) return null;
        try { return Convert.ToDecimal(v, CultureInfo.InvariantCulture); }
        catch { return null; }
    }

    private static DateTime? Tarix(IDictionary<string, object?> s, string sutun)
    {
        if (!s.TryGetValue(sutun, out var v) || v == null) return null;
        if (v is DateTime dt) return dt;
        return DateTime.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var p) ? p : null;
    }

    // maxUzunluq: SQL Server sütun limiti (AppDbContext-dəki HasMaxLength dəyərləri).
    // Gözlənilməz uzun dəyər bütün ili dağıtmasın deyə kəsilir.
    private static string? Metn(IDictionary<string, object?> s, string sutun, int? maxUzunluq)
    {
        if (!s.TryGetValue(sutun, out var v) || v == null) return null;
        var t = Convert.ToString(v, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(t)) return null;
        t = t.Trim();
        return (maxUzunluq.HasValue && t.Length > maxUzunluq.Value)
            ? t[..maxUzunluq.Value]
            : t;
    }
}
