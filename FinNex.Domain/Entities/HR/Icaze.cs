namespace FinNex.Domain.Entities.HR
{
    public class Icaze : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        // Bankda əvəzedici mütləqdir
        public int EvezEdenIsciId { get; set; }
        public Isci EvezEdenIsci { get; set; } = null!;

        // İcazə tarixi və saatları
        public DateTime IcazeTarixi { get; set; }       // Hansı gün
        public TimeSpan BaslamaSaati { get; set; }       // Neçədən
        public TimeSpan BitisSaati { get; set; }         // Neçəyə qədər
        public double IcazeSaati                         // Hesablanan müddət
            => (BitisSaati - BaslamaSaati).TotalHours;

        public string? Sebeb { get; set; }               // Qeyd / səbəb
        public string? ImtinaSebebi { get; set; }        // Rədd olduqda

        public IcazeStatus Status { get; set; } = IcazeStatus.Gozlemede;

        // ── Workflow: 3 mərhələ ──────────────────────────
        // 1. Şöbə rəisi
        public bool? SobeReisiTesdiq { get; set; }
        public int? SobeReisiId { get; set; }
        public DateTime? SobeReisiTesdiqTarixi { get; set; }

        // 2. Rəhbər
        public bool? RehberTesdiq { get; set; }
        public int? RehberId { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }

        // 3. HR
        public bool? HrTesdiq { get; set; }
        public int? HrId { get; set; }
        public DateTime? HrTesdiqTarixi { get; set; }
    }
}