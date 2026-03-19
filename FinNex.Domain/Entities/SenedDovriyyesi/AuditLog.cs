namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public class AuditLog : BaseEntity
    {
        public int UserId { get; set; }   // string yox!
        public AppUser User { get; set; } = null!;

        public string Action { get; set; } = null!;

        public int? SenedId { get; set; }
        public Sened? Sened { get; set; }

        public string? Ip { get; set; }
        public string? DetailsJson { get; set; }
    }


}
