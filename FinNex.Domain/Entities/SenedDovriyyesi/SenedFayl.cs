namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public class SenedFayl : BaseEntity
    {
        public int SenedId { get; set; }
        public Sened Sened { get; set; } = null!;

        public int VersiyaNo { get; set; }

        public string OriginalAd { get; set; } = null!;
        public string StoredAd { get; set; } = null!;

        public string ContentType { get; set; } = null!;
        public long OlcuBytes { get; set; }

        public string Sha256 { get; set; } = null!;

        public string Yol { get; set; } = null!;

        public bool AktivVersiya { get; set; }

        // 🔴 Gələcək üçün
        public string StorageProvider { get; set; } = "Local"; // Local / Azure / S3
    }

}
