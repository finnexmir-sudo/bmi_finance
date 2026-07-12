using FinNex.Application.DTOs.Risk;

namespace FinNex.Application.Interfaces.Risk;

public interface IRiskService
{
    // "Risk" departamentinə təyin olunmuş aktiv hesabatlar (OracleSorgular)
    Task<IList<RiskHesabatDto>> HesabatlarAsync();

    // Dashboard: sorğuları Mahiyyət tag-inə görə ayırır — [KPI]/[PIE]/[BAR]/[LINE]
    // widget kimi icra olunur (parametrsiz), qalanı adi hesabat kartı kimi qalır.
    Task<RiskPanelDto> PanelAsync(int maxRows = 5000);

    // Seçilmiş hesabatı Oracle-da icra edir (yalnız SELECT). Parametr token-ləri
    // ({BASTARIX}/{SONTARIX}/{TARIX}/{HEDD}/{IL}) varsa və dəyər verilməyibsə,
    // icra olunmur — yalnız hansı parametrlərin lazım olduğu qaytarılır.
    Task<RiskNeticeDto?> IcraEtAsync(int sorguId, RiskParametrDeyer? deyerler = null, int maxRows = 100000);
}
