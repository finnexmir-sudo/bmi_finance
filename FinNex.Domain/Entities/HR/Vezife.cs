namespace FinNex.Domain.Entities.HR
{
    public class Vezife : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public string? Təsvir { get; set; }
        public bool Aktivdir { get; set; } = true;

        public ICollection<Isci> Isciler { get; set; } = new List<Isci>();
    }

}
