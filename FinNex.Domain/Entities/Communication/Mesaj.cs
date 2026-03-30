// FinNex.Domain.Entities.Communication/Mesaj.cs
using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication
{
    public class Mesaj : BaseEntity
    {
        public int GonderenIsciId { get; set; }
        public Isci GonderenIsci { get; set; } = null!;

        public int AlanIsciId { get; set; }
        public Isci AlanIsci { get; set; } = null!;

        public string Movzu { get; set; } = "";
        public string Metn { get; set; } = "";

        public bool Oxunub { get; set; } = false;
        public DateTime? OxunmaTarixi { get; set; }

        public int? CavabVerdigiMesajId { get; set; }
        public Mesaj? CavabVerdigiMesaj { get; set; }
        public ICollection<Mesaj> Cavablar { get; set; } = new List<Mesaj>();
    }
}

