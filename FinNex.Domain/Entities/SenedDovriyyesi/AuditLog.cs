namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public class AuditLog : BaseEntity
    {
        public string UserId { get; set; } = null!;
        public string Action { get; set; } = null!; // Upload/View/Download/SoftDelete/Restore
        public int? SenedId { get; set; }
        public string? Ip { get; set; }
        public string? DetailsJson { get; set; }
    }
}
