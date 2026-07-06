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
}
