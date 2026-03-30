using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.HR
{
    public class Vezife : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public string? Tesvir { get; set; }
        public bool Aktivdir { get; set; } = true;

        public int DepartamentId { get; set; }
        public Departament Departament { get; set; } = null!;

        public ICollection<IsciTeyinat> IsciTeyinatlar { get; set; }
            = new List<IsciTeyinat>();
    }

}
