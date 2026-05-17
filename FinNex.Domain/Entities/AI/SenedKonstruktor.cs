namespace FinNex.Domain.Entities.AI;

public class SenedKonstruktor : BaseEntity
{
    public int AppUserId { get; set; }
    public string SenedNovu { get; set; } = "";
    public string ParametrlerJson { get; set; } = "{}";
    public string GeneratedContent { get; set; } = "";

    public virtual AppUser AppUser { get; set; } = null!;
}
