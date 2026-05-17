using FinNex.Domain.Enums;

namespace FinNex.Application.DTOs.AI;

public class RiskyClause
{
    public string MaddeTipi { get; set; } = "";
    public string RiskliCumle { get; set; } = "";
    public string ZararPotensiali { get; set; } = "";
    public string AlternativTeklif { get; set; } = "";
}

public class RiskAnalizResult
{
    public RiskLevel RiskLevel { get; set; }
    public List<RiskyClause> RiskyClauslar { get; set; } = new();
    public string? Xeta { get; set; }
}

public class KonstruktorResult
{
    public string GeneratedContent { get; set; } = "";
    public string? Xeta { get; set; }
}
