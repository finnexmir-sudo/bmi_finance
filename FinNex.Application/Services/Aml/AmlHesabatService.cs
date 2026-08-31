using FinNex.Application.DTOs.Aml;
using FinNex.Application.DTOs.Sorgular;
using FinNex.Application.Interfaces.Aml;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Sorgular;

namespace FinNex.Application.Services.Aml;

/// <summary>
/// AML → «Hesab üzrə sorğu» (BMI: <c>frmhesabsorgu.cs</c>).
///
/// ── BMI-DƏN NƏ DƏYİŞDİ ───────────────────────────────────────────────────
/// BMI-də SQL nəticəsi bir <c>dataGridView</c>-ə düşür, sonra <c>exceleat2()</c>
/// içində xanaların ÜZƏRİNƏ yazılırdı (çatdırılma kanalı, hesab adları,
/// VÖEN/FİN axtarışı, hesab növü, valyuta kodu). Burada həmin çevirmələrin
/// HAMISI SQL-in üst qatındadır — servis yalnız sorğunu icra edib nəticəni
/// olduğu kimi ötürür. Bir məntiq iki yerdə saxlanılmır.
///
/// ── SQL HARADA SAXLANILIR ────────────────────────────────────────────────
/// <c>OracleSorgular</c> cədvəlində (layihə qaydası — CLAUDE.md). Admin
/// oradan redaktə edə bilir. Sorğu adları aşağıdakı sabitlərdədir; əlavə
/// script: <c>docs/sql/aml/90_AML_OracleSorgular.sql</c>.
/// </summary>
public class AmlHesabatService : IAmlHesabatService
{
    private readonly IOracleService _oracle;
    private readonly IOracleSorguService _sorgu;

    // Sorğu adları QƏSDƏN ASCII-dir — SSMS-də Azərbaycan hərfləri bəzən pozulur
    // və `=` müqayisəsi sükutla sınır (Hevale_OracleSorgular.sql-də eyni qayda).
    public const string AdFiziki = "AML_HESAB_SORGU_FIZIKI";
    public const string AdHuquqi = "AML_HESAB_SORGU_HUQUQI";
    public const string AdQaliq  = "AML_HESAB_SORGU_QALIQ";

    /// <summary>Bir çıxarışda gözlənilən maksimum sətir sayı.</summary>
    private const int MaxSetir = 20000;

    public AmlHesabatService(IOracleService oracle, IOracleSorguService sorgu)
    {
        _oracle = oracle;
        _sorgu = sorgu;
    }

    private List<OracleSorguDto>? _cache;

    private async Task<string> SqlAl(string ad)
    {
        _cache ??= (await _sorgu.HamisiniGetirAsync())?.Data?.ToList() ?? new List<OracleSorguDto>();
        var q = _cache.FirstOrDefault(x => x.Aktiv
            && !string.IsNullOrWhiteSpace(x.SorguMetni)
            && string.Equals((x.SorguAdi ?? "").Trim(), ad, StringComparison.OrdinalIgnoreCase));
        if (q == null)
            throw new InvalidOperationException(
                $"OracleSorgular-da sorğu tapılmadı: '{ad}'. " +
                "docs/sql/aml/90_AML_OracleSorgular.sql işlədilməlidir.");
        return q.SorguMetni;
    }

    /// <summary>Hesab nömrəsinin 6–7-ci simvolundan valyuta qısaltması.</summary>
    public static string? ValyutaKodu(string? hesab)
    {
        var h = (hesab ?? "").Trim();
        if (h.Length < 7) return null;
        return h.Substring(5, 2) switch
        {
            "00" => "AZN",
            "01" => "USD",
            "02" => "EUR",
            "03" => "RUB",
            "04" => "IRR",
            "05" => "AED",
            _ => null
        };
    }

    public async Task<AmlHesabNeticeDto> IcraEtAsync(AmlHesabSorguDto sorgu, CancellationToken ct = default)
    {
        var hesab = (sorgu.Hesab ?? "").Trim();
        var dto = new AmlHesabNeticeDto
        {
            Hesab = hesab,
            BasTarix = sorgu.BasTarix?.Date ?? DateTime.Today,
            SonTarix = sorgu.SonTarix?.Date ?? DateTime.Today,
            Huquqi = sorgu.Huquqi,
            Valyuta = ValyutaKodu(hesab)
        };

        // ── Validasiya — Oracle-a getməmişdən ƏVVƏL ──────────────────────
        if (hesab.Length == 0)
        {
            dto.Xeta = "Hesab nömrəsi boşdur.";
            return dto;
        }
        // Hesab nömrəsi sorğuya MƏTN kimi yerləşdirilir (bind dəyişəni yoxdur),
        // ona görə yalnız rəqəm qəbul edilir — apostrof/boşluq keçə bilməz.
        if (!hesab.All(char.IsDigit))
        {
            dto.Xeta = "Hesab nömrəsi yalnız rəqəmlərdən ibarət olmalıdır.";
            return dto;
        }
        if (dto.BasTarix > dto.SonTarix)
        {
            dto.Xeta = "Başlama tarixi bitmə tarixindən sonra ola bilməz.";
            return dto;
        }

        var t1 = dto.BasTarix.ToString("dd/MM/yyyy");
        var t2 = dto.SonTarix.ToString("dd/MM/yyyy");

        string Doldur(string sql) => sql
            .Replace("{HESAB}", hesab)
            .Replace("{TARIX1}", t1)
            .Replace("{TARIX2}", t2);

        try
        {
            // ── 1) Şapka: hesabın adı + giriş/son qalıq ──────────────────
            // BMI bunu ayrıca `hesabad_qaliq()` ilə çəkir.
            //
            // 31.08.2026 — sorğu `from dual` + üç müstəqil skalyar alt-sorğuya
            // keçdi, yəni HƏMİŞƏ 1 sətir qaytarır (əvvəl daxili join idi və biri
            // boş olanda üçü də itirirdi — hesabın adı da). Ona görə «tapılmadı»
            // şərti artıq SƏTİR SAYINA yox, MƏZMUNA baxır.
            var qaliqSql = Doldur(await SqlAl(AdQaliq));
            var qaliq = await _oracle.SelectAsync(qaliqSql, maxRows: 1, ct);
            if (qaliq.Count > 0)
            {
                dto.HesabAdi   = Val(qaliq[0], "NAME_LATIN")?.ToString()?.Trim();
                dto.GirisQaliq = Dec(Val(qaliq[0], "GIR_QALIQ"));
                dto.SonQaliq   = Dec(Val(qaliq[0], "SON_QALIQ"));
            }

            // Üçü də boşdursa hesab nə `accounts`-da, nə də qalıq tarixçəsində var.
            var hesabTapilmadi = string.IsNullOrWhiteSpace(dto.HesabAdi)
                              && dto.GirisQaliq == null
                              && dto.SonQaliq   == null;

            // ── 2) Əsas çıxarış ─────────────────────────────────────────
            var sql = Doldur(await SqlAl(sorgu.Huquqi ? AdHuquqi : AdFiziki));
            var netice = await _oracle.SelectXamAsync(sql, MaxSetir, ct);

            dto.Sutunlar = netice.Sutunlar;
            dto.Setirler = netice.Setirler;
            dto.Ugurlu = true;

            if (dto.Setirler.Count == 0)
                dto.Xeta = hesabTapilmadi
                    ? "Nəticə yoxdur — hesab tapılmadı və ya seçilmiş giriş tarixindən sonra açılıb."
                    : "Seçilmiş dövrdə bu hesabda əməliyyat yoxdur.";

            if (dto.Setirler.Count >= MaxSetir)
                dto.Xeta = $"Nəticə {MaxSetir} sətirdə kəsildi — dövrü daraldın. " +
                           "Excel çıxarışı da natamamdır.";
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    // ── Köməkçilər ───────────────────────────────────────────────────────

    private static object? Val(Dictionary<string, object?> row, string ad)
    {
        if (row.TryGetValue(ad, out var v)) return v;
        // Oracle sütun adını böyük hərflə qaytarır, amma sorğu dırnaqlı yazılsa fərqli ola bilər
        var key = row.Keys.FirstOrDefault(k => string.Equals(k, ad, StringComparison.OrdinalIgnoreCase));
        return key == null ? null : row[key];
    }

    // Oracle NUMBER sütunu artıq `decimal` obyektidir — stringə çevirib geri parse
    // etmək az-AZ mədəniyyətində 100× səhv verir (CLAUDE.md — «Oracle Rəqəmi»).
    private static decimal? Dec(object? v) => v switch
    {
        null => null,
        decimal d => d,
        double db => (decimal)db,
        float f => (decimal)f,
        long l => l,
        int i => i,
        short s => s,
        _ => null
    };
}
