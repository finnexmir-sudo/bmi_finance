using FinNex.Application.DTOs.Kredit.Muqavile;

namespace FinNex.Application.Interfaces.Kredit;

/// <summary>
/// Kredit müqaviləsi hazırlanması modulu — verilmiş kreditlərin Oracle-dan oxunması.
/// Oracle YALNIZ oxuma üçündür (IOracleService, SELECT-only).
/// </summary>
public interface IKreditMuqavileService
{
    /// <summary>
    /// Verilən tarixdə (date_open) verilmiş kreditlərin siyahısını Oracle-dan qaytarır.
    /// </summary>
    Task<List<KreditMuqavileSatirDto>> KreditleriGetirAsync(DateTime tarix, CancellationToken ct = default);

    /// <summary>
    /// Tək krediti hesab nömrəsi (licschkre) + KS (subschkre) + tarixə görə qaytarır.
    /// Hazırlama səhifəsi üçün. Tapılmasa null.
    /// </summary>
    Task<KreditMuqavileSatirDto?> KrediGetirAsync(string hesabNo, string ks, DateTime tarix, CancellationToken ct = default);
}
