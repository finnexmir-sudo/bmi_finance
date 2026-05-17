using FinNex.Application.DTOs.AI;

namespace FinNex.Application.Interfaces.AI;

public interface ISenedAiService
{
    Task<RiskAnalizResult> AnalyzeRiskAsync(string text, string fileName);
    Task<KonstruktorResult> ConstructDocumentAsync(string senedNovu, string musteriAd,
        int gecikmeGun, decimal meble, string? elaveMelumat);
}
