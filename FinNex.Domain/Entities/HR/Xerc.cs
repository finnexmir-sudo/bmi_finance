using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.HR
{
    public class Xerc : BaseEntity
    {
        public int? IsciId { get; set; }
        public Isci? Isci { get; set; }

        public int? DepartamentId { get; set; }
        public Departament? Departament { get; set; }

        public bool ManualGiris { get; set; } = false;

        public int KateqoriyaId { get; set; }
        public XercKateqoriyasi Kateqoriya { get; set; } = null!;

        public string Tesvir { get; set; } = null!;
        public decimal Mebleg { get; set; }
        public DateTime XercTarixi { get; set; }
        public string? QebzFaylYolu { get; set; }

        public XercStatus Status { get; set; } = XercStatus.Muraciet;

        public int? TesdiqleyenIsciId { get; set; }
        public Isci? TesdiqleyenIsci { get; set; }
        public DateTime? TesdiqTarixi { get; set; }
        public string? ImtinaSebebi { get; set; }
    }
}
