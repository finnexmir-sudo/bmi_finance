namespace FinNex.Application.DTOs.Countrycode;

/// <summary>
/// BMI `countrycode` cədvəlindən bir ölkə.
///
/// Namespace mənbə cədvəlin adını daşıyır (`Countrycode`) — `Kurval` ilə eyni
/// qayda. Səbəb: alt namespace-ə entity/DTO adı vermək valideyn namespace-də
/// həmin tipi kölgələyir və layihəni build olunmaz edir (CS0118 — 13.08.2026
/// hadisəsi, CLAUDE.md).
/// </summary>
public class BmiOlkeDto
{
    /// <summary>countrycode.CODE — ISO-3 kodu (AZE, TUR, IRN…).</summary>
    public string Kod { get; set; } = "";

    /// <summary>
    /// countrycode.NAME — Azərbaycan dilində tam ad («Azərbaycan Respublikası»).
    /// MÜQAVİLƏYƏ DÜŞƏN DƏYƏR BUDUR: şablon mətni «{k_olke}nın vətəndaşı» kimi
    /// qurulub, yəni ora kod yox, ad yazılmalıdır. Kod yalnız Oracle-dan gələn
    /// dəyəri (məs. zaminin COUNTRYCODE-u) ada çevirmək üçün işlədilir.
    /// </summary>
    public string Ad { get; set; } = "";
}
