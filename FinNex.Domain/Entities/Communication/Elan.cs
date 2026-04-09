using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication
{
    public class Elan : BaseEntity
    {
        public string Bashliq { get; set; } = null!;
        public string Metn { get; set; } = null!;

        public int GonderenIsciId { get; set; }
        public Isci GonderenIsci { get; set; } = null!;

        public bool Vacibdir { get; set; }
        public DateTime? BitirmeTarixi { get; set; } // Null = müddətsiz

        public bool Aktivdir { get; set; } = true;
    }
}
