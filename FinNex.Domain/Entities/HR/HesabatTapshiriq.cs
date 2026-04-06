namespace FinNex.Domain.Entities.HR
{
    public class HesabatTapshiriq : BaseEntity
    {
        public int SablonId { get; set; }
        public HesabatSablonu Sablon { get; set; } = null!;

        // Dövr
        public DateTime DovrBaslangic { get; set; }
        public DateTime DovrSon { get; set; }

        // Deadline
        public DateTime Deadline { get; set; }

        // Status
        public HesabatTapshiriqStatus Status { get; set; } = HesabatTapshiriqStatus.Gozleyir;

        // İcra məlumatları
        public int? IcraEdenIsciId { get; set; }
        public Isci? IcraEdenIsci { get; set; }
        public DateTime? IcraTarixi { get; set; }

        // Qeyd
        public string? Qeyd { get; set; }
    }

    public enum HesabatTapshiriqStatus
    {
        Gozleyir = 1,
        Icrada = 2,
        Tamamlandi = 3,
        Gecikib = 4
    }
}
