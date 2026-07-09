using System.Globalization;
using FinNex.Application.DTOs.Kredit.Muqavile;
using FinNex.Application.Interfaces.Kredit;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Sorgular;

namespace FinNex.Application.Services.Kredit;

/// <summary>
/// BMI-dəki Form2 (Anakredit) "kataloqgetir_zamin_ayliq" sorğusunun FinNex qarşılığı.
/// Verilmiş kreditləri Oracle-dan (yalnız SELECT) oxuyur.
/// Qeyd: BMI-də sorğu inner join ilə odb.creditinfo-ya bağlıdır — girov/təminat
/// qeydiyyatı olmayan kredit siyahıya düşmür. Real data ilə yoxlanılmalıdır.
/// </summary>
public class KreditMuqavileService : IKreditMuqavileService
{
    private readonly IOracleService _oracle;
    private readonly IOracleSorguService _sorguService;

    public KreditMuqavileService(IOracleService oracle, IOracleSorguService sorguService)
    {
        _oracle = oracle;
        _sorguService = sorguService;
    }

    public async Task<List<KreditMuqavileSatirDto>> KreditleriGetirAsync(DateTime tarix, CancellationToken ct = default)
    {
        var sql = await KreditSqlAsync(tarix, null);
        var rows = await _oracle.SelectAsync(sql, 2000, ct);
        return rows.Select(Map).ToList();
    }

    public async Task<KreditMuqavileSatirDto?> KrediGetirAsync(string hesabNo, string ks, DateTime tarix, CancellationToken ct = default)
    {
        // hesabNo/ks yalnız rəqəmlərdən ibarətdir — SQL-ə salınmadan öncə təmizlənir.
        var hesab = new string((hesabNo ?? "").Where(char.IsDigit).ToArray());
        var subs = new string((ks ?? "").Where(char.IsDigit).ToArray());
        if (hesab.Length == 0) return null;

        var extra = $" AND t.licschkre = '{hesab}' AND t.subschkre = '{(subs.Length == 0 ? "0" : subs)}'";
        var sql = await KreditSqlAsync(tarix, extra);
        var rows = await _oracle.SelectAsync(sql, 5, ct);
        return rows.Select(Map).FirstOrDefault();
    }

    public async Task<List<ZaminDaxilDto>> ZaminleriGetirAsync(string hesabNo, string ks, CancellationToken ct = default)
    {
        var hesab = new string((hesabNo ?? "").Where(char.IsDigit).ToArray());
        var subs = new string((ks ?? "").Where(char.IsDigit).ToArray());
        if (hesab.Length == 0) return new();

        var sql = await ZaminSqlAsync(hesab, subs.Length == 0 ? "0" : subs);
        var rows = await _oracle.SelectAsync(sql, 20, ct);
        return rows.Select(r => new ZaminDaxilDto
        {
            Ad = Str(r, "AD"),
            Pasport = Str(r, "PASPORT"),
            Fin = Str(r, "FIN"),
            Telefon = Str(r, "TELEFON"),
            Unvan = Str(r, "UNVAN"),
        }).ToList();
    }

    // ── SQL mənbəyi: Admin → Oracle Sorğular (adına görə) ──
    // PID (Cari Kataloq/Bildiriş) ilə eyni pattern. SQL yalnız Admin panelindədir —
    // kodda saxlanmır. Bu sorğular parametrlidir: stored SQL-də yer tutucular
    // ({TARIX}/{EXTRA}/{HESAB}/{SUBS}) servisdə əvəz olunur. hesab/subs/tarix dəyərləri
    // əvvəlcədən təmizlənir (yalnız rəqəm / DateTime format) — mətn birbaşa SQL-ə düşmür.
    // Sorğu adları: "Kredit Müqavilə" və "Kredit Zaminləri" (Admin-də aktiv olmalıdır).

    private async Task<string> KreditSqlAsync(DateTime tarix, string? extraWhere)
    {
        var tarixStr = tarix.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        var xam = await SaxlanmisSqlAsync("Kredit Müqavilə")
            ?? throw new InvalidOperationException(
                "\"Kredit Müqavilə\" sorğusu Admin → Oracle Sorğular-da tapılmadı (aktiv olmalıdır).");
        return xam.Replace("{TARIX}", tarixStr).Replace("{EXTRA}", extraWhere ?? "");
    }

    private async Task<string> ZaminSqlAsync(string hesab, string subs)
    {
        var xam = await SaxlanmisSqlAsync("Kredit Zaminləri")
            ?? throw new InvalidOperationException(
                "\"Kredit Zaminləri\" sorğusu Admin → Oracle Sorğular-da tapılmadı (aktiv olmalıdır).");
        return xam.Replace("{HESAB}", hesab).Replace("{SUBS}", subs);
    }

    // Admin → Oracle Sorğular-da adına görə aktiv sorğu (ad normalizasiya olunur:
    // ə→e, ş→s, boşluq atılır — böyük/kiçik hərf və boşluq fərqi əhəmiyyətsiz).
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

    private static KreditMuqavileSatirDto Map(Dictionary<string, object?> r) => new()
    {
        Adi                 = Str(r, "ADI"),
        Ks                  = Str(r, "KS"),
        SubQeyd             = Str(r, "SUBQEYD"),
        HesabNo             = Str(r, "HESABNO"),
        VerilmeTarixi       = Dat(r, "VERILME_TARIXI"),
        Teyinat             = Str(r, "TEYINAT"),
        Mebleg              = Dec(r, "MEBLEG"),
        MeblegAzn           = Dec(r, "MEBLEG_AZN"),
        Ayliq               = Dec(r, "AYLIQ"),
        Fifd                = Str(r, "FIFD"),
        Faiz                = Dec(r, "FAIZ"),
        VkFaiz              = Dec(r, "VK_FAIZ"),
        EhtiyatFaiz         = Dec(r, "EHTIYAT_FAIZ"),
        Muddet              = Str(r, "MUDDET"),
        SeriyaNo            = Str(r, "SERIYA_NO"),
        VerenOrqan          = Str(r, "VEREN_ORQAN"),
        SenedVerilmeTarixi  = Dat(r, "SENED_VERILME_TARIXI"),
        Mobil               = Str(r, "MOBIL"),
        Unvan               = Str(r, "UNVAN"),
        Olke                = Str(r, "OLKE"),
        Fin                 = Str(r, "FIN"),
        GirovUnvan          = Str(r, "GIROV_UNVAN"),
        TeminatNo           = Str(r, "TEMINAT_NO"),
        CixarisTarixi       = Dat(r, "CIXARIS_TARIXI"),
        CariHesab           = Str(r, "CARI_HESAB"),
        GirovDeyeri         = Dec(r, "GIROV_DEYERI"),
        HuquqiSexs          = (Dec(r, "HUQUQI_SEXS") ?? 0m) == 1m,
        Voen                = Str(r, "VOEN"),
    };

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

    private static decimal? Dec(Dictionary<string, object?> r, string key)
    {
        var v = Val(r, key);
        if (v is null) return null;
        if (v is decimal d) return d;
        return decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var res)
            ? res : (decimal?)null;
    }

    private static DateTime? Dat(Dictionary<string, object?> r, string key)
    {
        var v = Val(r, key);
        if (v is null) return null;
        if (v is DateTime dt) return dt;
        return DateTime.TryParse(v.ToString(), out var res) ? res : (DateTime?)null;
    }
}
