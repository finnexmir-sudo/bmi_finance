using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public class SenedNovu : BaseEntity
    {
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        public string Kod { get; set; } = null!;   // məsələn: MUQAVILE, ERIZE
        public string Ad { get; set; } = null!;
        public bool Aktiv { get; set; } = true;
    }
}
