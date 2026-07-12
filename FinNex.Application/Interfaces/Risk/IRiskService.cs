using FinNex.Application.DTOs.Risk;

namespace FinNex.Application.Interfaces.Risk;

public interface IRiskService
{
    // "Risk" departamentinə təyin olunmuş aktiv hesabatlar (OracleSorgular)
    Task<IList<RiskHesabatDto>> HesabatlarAsync();

    // Seçilmiş hesabatı Oracle-da icra edir (yalnız SELECT) — dinamik sütun+sətir
    Task<RiskNeticeDto?> IcraEtAsync(int sorguId, int maxRows = 100000);
}
