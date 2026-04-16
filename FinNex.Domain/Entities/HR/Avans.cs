namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Isci avans muracietleri.
    /// Isci her ay 1 defe avans iste biler (maasin max 30%-i).
    /// Muhasib tesdiq edir, ay sonu maas hesablananda NET-den cixilir.
    /// </summary>
    public class Avans : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int Il { get; set; }
        public int Ay { get; set; }
        public decimal Mebleg { get; set; }
        public string? Sebeb { get; set; }

        public AvansStatus Status { get; set; } = AvansStatus.Gozlemede;

        public int? MuhasibId { get; set; }
        public Isci? Muhasib { get; set; }
        public DateTime? TesdiqTarixi { get; set; }
        public string? ImtinaSebebi { get; set; }
    }
}
