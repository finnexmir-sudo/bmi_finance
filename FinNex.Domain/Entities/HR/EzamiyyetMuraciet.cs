namespace FinNex.Domain.Entities.HR
{
    public class EzamiyyetMuraciet : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public string Baslig { get; set; } = null!;

        public int MekanId { get; set; }
        public EzamiyyetMekan Mekan { get; set; } = null!;

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }

        // Gündaxili saat aralığı (istəyə bağlı — tam gün üçün null)
        public TimeSpan? BaslamaSaati { get; set; }
        public TimeSpan? BitisSaati { get; set; }

        public string? SenedYolu { get; set; }
        public string? SenedAd  { get; set; }

        public string? Qeyd { get; set; }

        public EzamiyyetStatus Status { get; set; } = EzamiyyetStatus.Gozleyir;

        // Rəhbər təsdiq sahəsi
        public bool?     RehberTesdiq        { get; set; }
        public int?      RehberId            { get; set; }
        public Isci?     Rehber              { get; set; }
        public DateTime? RehberTesdiqTarixi  { get; set; }
        public string?   RehberQeydi         { get; set; }

        // Geri dönüş notu — işçi ezamiyyətdən qayıtdıqdan sonra əlavə edir
        public string? GeriDonusQeydi { get; set; }
    }
}
