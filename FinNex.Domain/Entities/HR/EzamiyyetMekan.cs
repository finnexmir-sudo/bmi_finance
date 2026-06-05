namespace FinNex.Domain.Entities.HR
{
    public class EzamiyyetMekan : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public bool Aktiv { get; set; } = true;

        public ICollection<EzamiyyetMuraciet> Muracietler { get; set; } = new List<EzamiyyetMuraciet>();
    }
}
