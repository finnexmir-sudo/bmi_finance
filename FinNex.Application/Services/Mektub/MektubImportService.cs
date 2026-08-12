using System.Globalization;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Mektub;
using FinNex.Application.Interfaces.Mektub;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Domain.Entities.Mektub;
using FinNex.Domain.Entities.Sorgular;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Mektub;

public class MektubImportService : IMektubImportService
{
    private readonly IUnitOfWork _uow;
    private readonly IOracleService _oracle;

    // Bir ildəki ən böyük say ~4 800-dür (daxil 2020). Limit ondan xeyli yuxarı
    // saxlanılır; buna baxmayaraq nəticədə LimiteCatdi bayrağı ilə yoxlanılır.
    private const int IlLimiti = 20000;

    // Oracle sorğuları Admin → Oracle Sorğular-da saxlanılır (layihə qaydası),
    // ADLAR ASCII-dir ki, SSMS-in Azərbaycan hərfi tələsi axtarışı sındırmasın.
    // Seed: docs/sql/mektub/Mektub_OracleSorgular.sql
    private const string SorguXaricSaylar    = "MEKTUB_IDXAL_XARIC_SAYLAR";
    private const string SorguXaricSetirler  = "MEKTUB_IDXAL_XARIC_SETIRLER";
    private const string SorguDaxilSaylar    = "MEKTUB_IDXAL_DAXIL_SAYLAR";
    private const string SorguDaxilSetirler  = "MEKTUB_IDXAL_DAXIL_SETIRLER";

    // Sətir sorğularındakı token — kod onu "il = 2024" / "il IS NULL" ilə əvəz edir.
    private const string IlToken = "{IL_SERTI}";

    // İdxal sətirləri sütun ADINA görə entity sahələrinə yazılır. Sorğu redaktə
    // olunub bir sütun itsə, xəta OLMUR — sahə səssizcə boş yazılır və 87 min
    // sətrin içində fərq edilmir. Ona görə yazmazdan ƏVVƏL sütunlar yoxlanılır.
    private static readonly string[] XaricSutunlar =
        { "KOD", "QEY_NOM", "GON_YER", "TARIX", "QISA_MEZ", "ICRACI", "DUBL", "MEKTUB_METN", "IL" };
    private static readonly string[] DaxilSutunlar =
        { "NOM", "NOM1", "DAX_TARIX", "IDARE_ADI", "GON_TARIX", "DAX_NOM", "MEK_UNVAN", "IL" };

    public MektubImportService(IUnitOfWork uow, IOracleService oracle)
    {
        _uow = uow;
        _oracle = oracle;
    }

    // ── Vəziyyət ───────────────────────────────────────────────────────────
    public async Task<Result<MektubImportVeziyyetDto>> VeziyyetAsync(CancellationToken ct = default)
    {
        try
        {
            var dto = new MektubImportVeziyyetDto();

            var xarOra = await OracleIlSaylariAsync(SorguXaricSaylar, ct);
            var daxOra = await OracleIlSaylariAsync(SorguDaxilSaylar, ct);

            // Silinmiş sətirlər də sayılır: bu, İDXALIN tamlığını göstərən ekrandır,
            // biznes siyahısı deyil. Aşağıdakı idxal da açar mövcuddursa keçir (silinmiş
            // qeydi diriltmir) — iki tərəf beləcə eyni cavabı verir.
            var xarFin = (await _uow.Repository<XaricMektub>()
                    .HamisiniGetirAsync(izlemeden: true))
                .GroupBy(x => x.Il).ToDictionary(g => g.Key, g => g.Count());

            var daxFin = (await _uow.Repository<DaxilMektub>()
                    .HamisiniGetirAsync(izlemeden: true))
                .GroupBy(x => x.Il).ToDictionary(g => g.Key, g => g.Count());

            dto.Xaric = Birlesdir(xarOra, xarFin);
            dto.Daxil = Birlesdir(daxOra, daxFin);

            return Result<MektubImportVeziyyetDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            return Result<MektubImportVeziyyetDto>.Fail($"Vəziyyət alınmadı: {ex.Message}");
        }
    }

    private static List<MektubImportIlDto> Birlesdir(
        Dictionary<int?, int> oracle, Dictionary<int?, int> finnex)
    {
        return oracle.Keys.Concat(finnex.Keys).Distinct()
            .Select(il => new MektubImportIlDto
            {
                Il        = il,
                OracleSay = oracle.TryGetValue(il, out var o) ? o : 0,
                FinNexSay = finnex.TryGetValue(il, out var f) ? f : 0
            })
            .OrderBy(x => x.Il == null).ThenBy(x => x.Il)   // boş il sonda
            .ToList();
    }

    private async Task<Dictionary<int?, int>> OracleIlSaylariAsync(string sorguAdi, CancellationToken ct)
    {
        var sql = await SorguMetniAsync(sorguAdi);
        var setirler = await _oracle.SelectAsync(sql, 500, ct);

        var yoxla = SutunYoxla(setirler, "IL", "SAY");
        if (!yoxla.Success) throw new InvalidOperationException($"«{sorguAdi}»: {yoxla.Message}");

        var xerite = new Dictionary<int?, int>();
        foreach (var s in setirler)
            xerite[Tam(s, "IL")] = Tam(s, "SAY") ?? 0;
        return xerite;
    }

    // ── Saxlanılan sorğular ────────────────────────────────────────────────
    // Layihə qaydası: Oracle sorğuları kodda deyil, `OracleSorgular` cədvəlində
    // saxlanılır və Admin → Oracle Sorğular səhifəsindən redaktə oluna bilir
    // (publish etmədən düzəliş imkanı). Ad tapılmasa idxal BAŞLAMIR.
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
                "(seed skripti: docs/sql/mektub/Mektub_OracleSorgular.sql).");

        return sorgu.SorguMetni;
    }

    // Sorğu nəticəsində tələb olunan sütunların hamısı varmı? Yoxdursa BAZAYA HEÇ NƏ
    // YAZILMIR — səhv redaktə səssiz data zədəsi yox, açıq xəta verməlidir.
    private static Result SutunYoxla(
        IList<Dictionary<string, object?>> setirler, params string[] telebOlunan)
    {
        if (setirler.Count == 0) return Result.Ok();   // sətir yoxdursa yoxlanacaq bir şey yoxdur

        var movcud = new HashSet<string>(setirler[0].Keys, StringComparer.OrdinalIgnoreCase);
        var catismayan = telebOlunan.Where(c => !movcud.Contains(c)).ToList();

        return catismayan.Count == 0
            ? Result.Ok()
            : Result.Fail(
                $"sorğu nəticəsində sütun çatışmır: {string.Join(", ", catismayan)}. " +
                "Sorğu redaktə olunub? Bazaya heç nə yazılmadı.");
    }

    // ── İdxal ──────────────────────────────────────────────────────────────
    public async Task<Result<MektubImportNeticeDto>> IlIdxalAsync(
        string jurnal, int? il, CancellationToken ct = default)
    {
        jurnal = (jurnal ?? "").Trim().ToLowerInvariant();
        if (jurnal != "xaric" && jurnal != "daxil")
            return Result<MektubImportNeticeDto>.Fail("Jurnal 'xaric' və ya 'daxil' olmalıdır.");

        try
        {
            return jurnal == "xaric"
                ? await XaricIlAsync(il, ct)
                : await DaxilIlAsync(il, ct);
        }
        catch (Exception ex)
        {
            return Result<MektubImportNeticeDto>.Fail($"İdxal xətası: {ex.Message}");
        }
    }

    private static string IlSerti(int? il) => il.HasValue ? $"il = {il.Value}" : "il IS NULL";

    private async Task<Result<MektubImportNeticeDto>> XaricIlAsync(int? il, CancellationToken ct)
    {
        var netice = new MektubImportNeticeDto { Jurnal = "xaric", Il = il };

        // Sorğu Admin → Oracle Sorğular-dan gəlir; {IL_SERTI} tokeni burada əvəz olunur.
        // MEKTUB_METN — CLOB, ODP.NET onu string kimi qaytarır (cəmi 31,9 MB).
        var sql = (await SorguMetniAsync(SorguXaricSetirler)).Replace(IlToken, IlSerti(il));
        var setirler = await _oracle.SelectAsync(sql, IlLimiti, ct);

        netice.Oxunan = setirler.Count;
        netice.LimiteCatdi = setirler.Count >= IlLimiti;

        // Sütunlar tam deyilsə heç nə yazılmır (səssiz zədə əvəzinə açıq xəta)
        var sutunYoxlama = SutunYoxla(setirler, XaricSutunlar);
        if (!sutunYoxlama.Success)
            return Result<MektubImportNeticeDto>.Fail($"«{SorguXaricSetirler}» {sutunYoxlama.Message}");

        // Mövcud açarlar — təkrar idxalda dublikat yaranmasın (KOD unikaldır).
        // `Silinib` filtri QƏSDƏN yoxdur: istifadəçi silmiş qeyd təkrar idxalda dirilməməlidir.
        var movcud = (await _uow.Repository<XaricMektub>()
                .HamisiniGetirAsync(x => x.Il == il, izlemeden: true))
            .Where(x => x.Kod.HasValue)
            .Select(x => x.Kod!.Value)
            .ToHashSet();

        foreach (var s in setirler)
        {
            ct.ThrowIfCancellationRequested();

            var kod = Uzun(s, "KOD");
            if (kod == null) { netice.Xetali++; continue; }
            if (movcud.Contains(kod.Value)) { netice.Kecilen++; continue; }

            var tarix = Tarix(s, "TARIX");
            await _uow.Repository<XaricMektub>().YaratAsync(new XaricMektub
            {
                Kod        = kod,
                QeyNom     = Metn(s, "QEY_NOM", 15),
                GonYer     = Metn(s, "GON_YER", 255),
                Tarix      = tarix,
                QisaMez    = Metn(s, "QISA_MEZ", 255),
                Icraci     = Metn(s, "ICRACI", 50),
                Dubl       = Metn(s, "DUBL", 10),
                MektubMetn = Metn(s, "MEKTUB_METN", null),
                // Oracle-da il boşdursa tarixdən götürülür (il filtrli səhifələrdə itməsin)
                Il         = Tam(s, "IL") ?? tarix?.Year
            });
            movcud.Add(kod.Value);
            netice.Elave++;
        }

        if (netice.Elave > 0) await _uow.YaddaSaxlaAsync();
        return Result<MektubImportNeticeDto>.Ok(netice, Xulase(netice));
    }

    private async Task<Result<MektubImportNeticeDto>> DaxilIlAsync(int? il, CancellationToken ct)
    {
        var netice = new MektubImportNeticeDto { Jurnal = "daxil", Il = il };

        // Sorğu Admin → Oracle Sorğular-dan gəlir; {IL_SERTI} tokeni burada əvəz olunur.
        // MEZMUN (LONG RAW) sorğuya QƏSDƏN daxil deyil — ayrı keçidlə gətiriləcək.
        var sql = (await SorguMetniAsync(SorguDaxilSetirler)).Replace(IlToken, IlSerti(il));
        var setirler = await _oracle.SelectAsync(sql, IlLimiti, ct);

        netice.Oxunan = setirler.Count;
        netice.LimiteCatdi = setirler.Count >= IlLimiti;

        var sutunYoxlama = SutunYoxla(setirler, DaxilSutunlar);
        if (!sutunYoxlama.Success)
            return Result<MektubImportNeticeDto>.Fail($"«{SorguDaxilSetirler}» {sutunYoxlama.Message}");

        var movcud = (await _uow.Repository<DaxilMektub>()
                .HamisiniGetirAsync(x => x.Il == il, izlemeden: true))
            .Where(x => x.Nom.HasValue)
            .Select(x => x.Nom!.Value)
            .ToHashSet();

        foreach (var s in setirler)
        {
            ct.ThrowIfCancellationRequested();

            var nom = Tam(s, "NOM");
            if (nom == null) { netice.Xetali++; continue; }
            if (movcud.Contains(nom.Value)) { netice.Kecilen++; continue; }

            var daxTarix = Tarix(s, "DAX_TARIX");
            await _uow.Repository<DaxilMektub>().YaratAsync(new DaxilMektub
            {
                Nom      = nom,
                Nom1     = Tam(s, "NOM1"),
                DaxTarix = daxTarix,
                IdareAdi = Metn(s, "IDARE_ADI", 255),
                GonTarix = Tarix(s, "GON_TARIX"),
                DaxNom   = Metn(s, "DAX_NOM", 45),
                MekUnvan = Tam(s, "MEK_UNVAN"),
                Il       = Tam(s, "IL") ?? daxTarix?.Year
            });
            movcud.Add(nom.Value);
            netice.Elave++;
        }

        if (netice.Elave > 0) await _uow.YaddaSaxlaAsync();
        return Result<MektubImportNeticeDto>.Ok(netice, Xulase(netice));
    }

    private static string Xulase(MektubImportNeticeDto n)
    {
        var ilAd = n.Il?.ToString(CultureInfo.InvariantCulture) ?? "ilsiz";
        var metn = $"{n.Jurnal} {ilAd}: {n.Oxunan} oxundu, {n.Elave} əlavə, {n.Kecilen} keçildi";
        if (n.Xetali > 0) metn += $", {n.Xetali} xətalı";
        if (n.LimiteCatdi) metn += " — DİQQƏT: sətir limitinə çatdı, təkrar işlədin";
        return metn + ".";
    }

    // ── Oracle dəyər çeviriciləri ──────────────────────────────────────────
    // ODP.NET NUMBER → decimal, DATE → DateTime, VARCHAR2/CLOB → string qaytarır.
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

    private static DateTime? Tarix(IDictionary<string, object?> s, string sutun)
    {
        if (!s.TryGetValue(sutun, out var v) || v == null) return null;
        if (v is DateTime dt) return dt;
        return DateTime.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var p) ? p : null;
    }

    // maxUzunluq: SQL Server sütun limiti. Oracle-dakı real maksimumlar limitlərin
    // içindədir (yoxlanılıb), amma gözlənilməz uzun dəyər bütün ili dağıtmasın deyə
    // kəsilir — məlumat itkisi riskindən çox, idxalın dayanması riski pisdir.
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
