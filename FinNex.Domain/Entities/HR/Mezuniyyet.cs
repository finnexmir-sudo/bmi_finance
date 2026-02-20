namespace FinNex.Domain.Entities.HR
{
    public class Mezuniyyet : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        // Banklarda əvəzedici şəxs mütləqdir
        public int? EvezEdenIsciId { get; set; }
        public Isci? EvezEdenIsci { get; set; }

        public MezuniyyetNovu Nov { get; set; }
        public MezuniyyetStatus Status { get; set; } = MezuniyyetStatus.Gozlemede;

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public int IsGunlerininSayi { get; set; } // Bayramlar çıxılmaqla hesablanan gün

        public string? Qeyd { get; set; }
        public string? ImtinaSebebi { get; set; }

        // --- Təsdiq Mərhələləri (Workflow) ---

        // 1. Şöbə Rəisi
        public bool? SobeReisiTesdiq { get; set; }
        public int? SobeReisiId { get; set; }
        public DateTime? SobeReisiTesdiqTarixi { get; set; }

        // 2. Rəhbər (Məsələn: Departament müdiri və ya Filial müdiri)
        public bool? RehberTesdiq { get; set; }
        public int? RehberId { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }

        // 3. HR (Son nöqtə və sənədləşmə)
        public bool? HrTesdiq { get; set; }
        public int? HrId { get; set; }
        public DateTime? HrTesdiqTarixi { get; set; }
    }
}
