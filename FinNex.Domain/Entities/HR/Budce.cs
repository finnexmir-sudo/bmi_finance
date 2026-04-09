using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.HR
{
    public class Budce : BaseEntity
    {
        public int DepartamentId { get; set; }
        public Departament Departament { get; set; } = null!;

        public int Il { get; set; }
        public int Ay { get; set; }

        public decimal PlanMebleg { get; set; }
        public decimal FaktikiMebleg { get; set; }
        public string? Qeyd { get; set; }
    }
}
