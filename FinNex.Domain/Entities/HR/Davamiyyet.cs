namespace FinNex.Domain.Entities.HR
{
    public class Davamiyyet : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public DateTime Tarix { get; set; }
        public DateTime? GirisVaxti { get; set; }
        public DateTime? CixisVaxti { get; set; }

        public DavamiyyetStatus Status { get; set; }
    }

}
