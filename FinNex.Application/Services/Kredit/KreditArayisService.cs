using System.Globalization;
using FinNex.Application.DTOs.Kredit.Arayis;
using FinNex.Application.Interfaces.Kredit;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Sorgular;

namespace FinNex.Application.Services.Kredit;

/// <inheritdoc cref="IKreditArayisService"/>
public class KreditArayisService : IKreditArayisService
{
    private readonly IOracleService _oracle;
    private readonly IOracleSorguService _sorguService;

    public KreditArayisService(IOracleService oracle, IOracleSorguService sorguService)
    {
        _oracle = oracle;
        _sorguService = sorguService;
    }

    public async Task<List<BorcalanArayisSatirDto>> BorcalanAxtarAsync(string regnom, CancellationToken ct = default)
    {
        // regnom rəqəmlərdən ibarətdir — SQL-ə salınmadan öncə təmizlənir.
        var kod = Reqemler(regnom);
        if (kod.Length == 0) return new();

        var sql = (await SaxlanmisSqlAsync("Arayış Borcalan")
                   ?? throw new InvalidOperationException(
                       "«Arayış Borcalan» sorğusu Admin → Oracle Sorğular-da tapılmadı (aktiv olmalıdır). " +
                       "Quraşdırma: docs/sql/kredit/Arayis_OracleSorgular.sql"))
                  .Replace("{REGNOM}", kod);

        var rows = await _oracle.SelectAsync(sql, 200, ct);
        return rows.Select(r => new BorcalanArayisSatirDto
        {
            Adi        = Str(r, "ADI"),
            HesabNo    = Str(r, "HESABNO"),
            Ks         = Str(r, "KS"),
            Tarix      = Dat(r, "TARIX"),
            Kredit     = Dec(r, "KREDIT"),
            Qaliq      = Dec(r, "QALIQ"),
            MuqavileNo = Str(r, "MUQAVILE_NO"),
            Valyuta    = ValyutaHesabdan(Str(r, "HESABNO")),
        }).ToList();
    }

    public async Task<List<ZaminArayisSatirDto>> ZaminAxtarAsync(string pincode, CancellationToken ct = default)
    {
        // FİN kodu — hərf və rəqəm. Apostrof/boşluq və s. atılır ki, SQL-ə mətn keçməsin.
        var fin = HerfReqem(pincode);
        if (fin.Length == 0) return new();

        var sql = (await SaxlanmisSqlAsync("Arayış Zamin")
                   ?? throw new InvalidOperationException(
                       "«Arayış Zamin» sorğusu Admin → Oracle Sorğular-da tapılmadı (aktiv olmalıdır). " +
                       "Quraşdırma: docs/sql/kredit/Arayis_OracleSorgular.sql"))
                  .Replace("{PINCODE}", fin);

        var rows = await _oracle.SelectAsync(sql, 200, ct);
        return rows.Select(r => new ZaminArayisSatirDto
        {
            Adi      = Str(r, "ADI"),
            HesabNo  = Str(r, "HESABNO"),
            Ks       = Str(r, "KS"),
            Zamin    = Str(r, "ZAMIN"),
            Tarix    = Dat(r, "TARIX"),
            Kredit   = Dec(r, "KREDIT"),
            Qaliq    = Dec(r, "QALIQ"),
            Valyuta  = ValyutaHesabdan(Str(r, "HESABNO")),
        }).ToList();
    }

    /// <summary>
    /// Valyuta hesab nömrəsinin 7–8-ci simvollarından (BMI: `substr(licschkre,7,2)`).
    /// 1-əsaslı `substr` → C#-da indeks 6.
    ///
    /// ⚠️ `kod_valuti` sütunu QƏSDƏN İŞLƏDİLMİR — o, INTEGER-dir; mətn ilə
    /// müqayisə edəndə Oracle `ORA-00932` verir (CLAUDE.md). Hesab nömrəsi
    /// CHAR olduğu üçün təhlükəsizdir.
    /// </summary>
    private static string ValyutaHesabdan(string? hesab)
    {
        if (string.IsNullOrWhiteSpace(hesab) || hesab.Length < 8) return "AZN";
        return hesab.Substring(6, 2) switch
        {
            "00" => "AZN",
            "01" => "USD",
            "02" => "AVRO",
            _    => "AZN"
        };
    }

    // ── Admin → Oracle Sorğular (adına görə aktiv sorğu) ──
    // `KreditMuqavileService` ilə eyni pattern və eyni normalizasiya.
    private async Task<string?> SaxlanmisSqlAsync(string ad)
    {
        var res = await _sorguService.HamisiniGetirAsync();
        if (!res.Success || res.Data is null) return null;
        var hedef = Norm(ad);
        var q = res.Data.FirstOrDefault(x => x.Aktiv && Norm(x.SorguAdi) == hedef);
        return string.IsNullOrWhiteSpace(q?.SorguMetni) ? null : q!.SorguMetni;
    }

    private static string Norm(string? s) => (s ?? "").ToLowerInvariant()
        .Replace("ə", "e").Replace("ş", "s").Replace("ç", "c").Replace("ğ", "g")
        .Replace("ı", "i").Replace("ö", "o").Replace("ü", "u").Replace(" ", "");

    private static string Reqemler(string? s)
        => new((s ?? "").Where(char.IsDigit).ToArray());

    private static string HerfReqem(string? s)
        => new((s ?? "").Where(char.IsLetterOrDigit).ToArray());

    private static object? Val(Dictionary<string, object?> r, string key)
    {
        if (r.TryGetValue(key, out var v)) return v;
        foreach (var kv in r)
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        return null;
    }

    private static string? Str(Dictionary<string, object?> r, string key)
        => Val(r, key)?.ToString()?.Trim();

    /// <summary>
    /// Oracle NUMBER → decimal.
    ///
    /// ⚠️ Dəyər ONSUZ DA `decimal` obyektidir — əvvəlcə tipi yoxlanır. Stringə
    /// çevirib parse etmək DƏQİQ 100× SƏHV verir (az-AZ vergülü + `NumberStyles.Any`
    /// min ayırıcısı — CLAUDE.md, 19.08.2026 real hadisə). Ehtiyat yolunda
    /// `NumberStyles.Float` işlədilir — o, min ayırıcısına İCAZƏ VERMİR.
    /// </summary>
    private static decimal? Dec(Dictionary<string, object?> r, string key)
    {
        var v = Val(r, key);
        if (v is null) return null;
        if (v is decimal d) return d;
        if (v is double db) return (decimal)db;
        if (v is int i) return i;
        if (v is long l) return l;

        var s = v.ToString();
        if (decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var r1)) return r1;
        return decimal.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out var r2) ? r2 : null;
    }

    private static DateTime? Dat(Dictionary<string, object?> r, string key)
    {
        var v = Val(r, key);
        if (v is null) return null;
        if (v is DateTime dt) return dt;
        return DateTime.TryParse(v.ToString(), out var res) ? res : null;
    }
}
