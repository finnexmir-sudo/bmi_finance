using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.HR
{
    public class IsciStrukturRolu : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public StrukturRolTipi RolTipi { get; set; }

        public int? DepartamentId { get; set; }
        public Departament? Departament { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; }

        public bool Aktivdir { get; set; } = true;
    }
}