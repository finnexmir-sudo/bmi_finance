namespace FinNex.Domain.Entities.HR
{
    public class XercKateqoriyasi : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public string? Ikon { get; set; }
        public bool Aktivdir { get; set; } = true;

        public ICollection<Xerc> Xercler { get; set; } = new List<Xerc>();
    }
}
