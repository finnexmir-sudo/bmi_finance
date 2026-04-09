namespace FinNex.Domain.Entities.HR
{
    public class TelimIshtiraki : BaseEntity
    {
        public int TelimId { get; set; }
        public Telim Telim { get; set; } = null!;

        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public bool Istirakdir { get; set; } // İştirak etdi?
        public decimal? Qiymet { get; set; } // Təlim sonrası qiymət (1-5)
        public string? Qeyd { get; set; }
    }
}
