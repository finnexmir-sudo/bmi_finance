namespace FinNex.Domain.Entities.HR
{
    public class ReytingKateqoriyasi : BaseEntity
    {
        public string Ad { get; set; } = null!;          // "A+", "A", "B", "C"
        public string? RengKodu { get; set; }             // "#22c55e" — UI rəngi
        public int MinXal { get; set; }
        public int MaxXal { get; set; }
        public decimal PulAmsali { get; set; }            // 1.50, 1.20, 1.00, 0.80
        public decimal SaatAmsali { get; set; }           // 1.20, 1.10, 1.00, 0.90
        public int Sira { get; set; }                     // sıralama (1 = ən yüksək)
        public bool Aktivdir { get; set; } = true;

        public ICollection<IsciReytinqi> IsciReytinqleri { get; set; } = new List<IsciReytinqi>();
    }
}
