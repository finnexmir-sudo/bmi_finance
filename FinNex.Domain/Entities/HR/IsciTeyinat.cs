using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.HR
{
    public class IsciTeyinat : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int DepartamentId { get; set; }
        public Departament Departament { get; set; } = null!;

        public int VezifeId { get; set; }
        public Vezife Vezife { get; set; } = null!;

        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; }

        public bool Esasdir { get; set; } = true;
        public bool Aktivdir { get; set; } = true;
    }
}