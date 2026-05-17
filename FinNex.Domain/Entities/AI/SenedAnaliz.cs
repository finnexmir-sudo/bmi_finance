using FinNex.Domain.Enums;

namespace FinNex.Domain.Entities.AI;

public class SenedAnaliz : BaseEntity
{
    public int AppUserId { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string OriginalText { get; set; } = "";
    public RiskLevel RiskLevel { get; set; }
    public string RiskyClausesJson { get; set; } = "[]";

    public virtual AppUser AppUser { get; set; } = null!;
}
