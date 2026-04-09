namespace FinNex.Domain.Entities.Communication
{
    public class PushAbonelik : BaseEntity
    {
        public int IsciId { get; set; }
        public HR.Isci Isci { get; set; } = null!;

        public string Endpoint { get; set; } = null!;
        public string P256dh { get; set; } = null!;
        public string Auth { get; set; } = null!;
    }
}
