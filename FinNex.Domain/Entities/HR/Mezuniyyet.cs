namespace FinNex.Domain.Entities.HR
{
    public class Mezuniyyet : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public MezuniyyetNovu Nov { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }

        public string? Qeyd { get; set; }
        public MezuniyyetStatus Status { get; set; }
    }

}
