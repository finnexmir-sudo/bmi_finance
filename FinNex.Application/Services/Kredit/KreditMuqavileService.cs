using System.Globalization;
using FinNex.Application.DTOs.Kredit.Muqavile;
using FinNex.Application.Interfaces.Kredit;
using FinNex.Application.Interfaces.Oracle;

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

    public KreditMuqavileService(IOracleService oracle)
    {
        _oracle = oracle;
    }

    public async Task<List<KreditMuqavileSatirDto>> KreditleriGetirAsync(DateTime tarix, CancellationToken ct = default)
    {
        var rows = await _oracle.SelectAsync(BuildSql(tarix, null), 2000, ct);
        return rows.Select(Map).ToList();
    }

    public async Task<KreditMuqavileSatirDto?> KrediGetirAsync(string hesabNo, string ks, DateTime tarix, CancellationToken ct = default)
    {
        // hesabNo/ks yalnız rəqəmlərdən ibarətdir — SQL-ə salınmadan öncə təmizlənir.
        var hesab = new string((hesabNo ?? "").Where(char.IsDigit).ToArray());
        var subs = new string((ks ?? "").Where(char.IsDigit).ToArray());
        if (hesab.Length == 0) return null;

        var extra = $" AND t.licschkre = '{hesab}' AND t.subschkre = '{(subs.Length == 0 ? "0" : subs)}'";
        var rows = await _oracle.SelectAsync(BuildSql(tarix, extra), 5, ct);
        return rows.Select(Map).FirstOrDefault();
    }

    public async Task<List<ZaminDaxilDto>> ZaminleriGetirAsync(string hesabNo, string ks, CancellationToken ct = default)
    {
        var hesab = new string((hesabNo ?? "").Where(char.IsDigit).ToArray());
        var subs = new string((ks ?? "").Where(char.IsDigit).ToArray());
        if (hesab.Length == 0) return new();

        // BMI Zaminler.cs (zamingetir) — odb.creditinfoguarantee-dən zaminlər
        var sql = $@"
SELECT g.guarantee_name AS AD,
       g.guarantee_id   AS PASPORT,
       g.pincode        AS FIN,
       g.telefon        AS TELEFON,
       g.adress         AS UNVAN
  FROM odb.creditinfoguarantee g
 WHERE g.licschkre = '{hesab}'
   AND g.subschkre = '{(subs.Length == 0 ? "0" : subs)}'";

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

    private static string BuildSql(DateTime tarix, string? extraWhere)
    {
        // Tarix DateTime-dan formatlanır (istifadəçi mətni birbaşa SQL-ə düşmür).
        var tarixStr = tarix.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

        return $@"
SELECT
    r.name_regnom                              AS ADI,
    t.subschkre                                AS KS,
    substr(t.licschkre, 10, 6) || t.subschkre  AS SUBQEYD,
    t.licschkre                                AS HESABNO,
    t.date_open                                AS VERILME_TARIXI,
    t.naznackredita                            AS TEYINAT,
    t.summakre                                 AS MEBLEG,
    t.summa                                    AS MEBLEG_AZN,
    (SELECT g1.summa_pog_kre + g1.summa_pog_pro
       FROM odb.graphpogkre g1
      WHERE g1.subschkre = t.subschkre
        AND g1.licschkre = t.licschkre
        AND g1.date_pog = odb.func_tar((SELECT MAX(gr3.date_pog)
                                          FROM odb.graphpogkre gr3
                                         WHERE gr3.subschkre = t.subschkre
                                           AND gr3.licschkre = t.licschkre)))  AS AYLIQ,
    k.fifd                                     AS FIFD,
    t.procstavkre                              AS FAIZ,
    t.procstav_19                              AS VK_FAIZ,
    t.procstavrez                              AS EHTIYAT_FAIZ,
    t.srok                                     AS MUDDET,
    r.passport                                 AS SERIYA_NO,
    r.senedi_veren_orqaninin_adi               AS VEREN_ORQAN,
    r.senedin_verilme_tarixi                   AS SENED_VERILME_TARIXI,
    r.telefon                                  AS MOBIL,
    r.registrac                                AS UNVAN,
    r.grajdanstvo                              AS OLKE,
    y.pincode                                  AS FIN,
    y.anyinfotodisting                         AS GIROV_UNVAN,
    y.registryno                               AS TEMINAT_NO,
    y.registrydate                             AS CIXARIS_TARIXI,
    m.az || m.nr || m.bank || m.licsch         AS CARI_HESAB,
    t.summa_zaloga                             AS GIROV_DEYERI,
    r.yurik                                    AS HUQUQI_SEXS,
    r.inn_regnom                               AS VOEN
FROM odb.licschkre t, odb.regnom r, odb.licsch m, odb.srokpogprockre k, odb.creditinfo y
WHERE t.date_open = to_date('{tarixStr}', 'dd-MM-yyyy')
  AND substr(t.licschkre, 10, 6) = r.regnom
  AND t.licschkre = k.licschkre
  AND t.subschkre = k.subschkre
  AND t.licschkre = y.licschkre
  AND t.subschkre = y.subschkre
  AND m.licsch = k.licsch_3(+){extraWhere}";
    }

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
