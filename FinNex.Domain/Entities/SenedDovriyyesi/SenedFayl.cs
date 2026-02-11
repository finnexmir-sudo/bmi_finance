namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public class SenedFayl : BaseEntity
    {
        public int SenedId { get; set; }
        public Sened Sened { get; set; } = null!;

        public int VersiyaNo { get; set; } = 1;

        public string OriginalAd { get; set; } = null!;
        public string StoredAd { get; set; } = null!;      // GUID.pdf

        public string ContentType { get; set; } = null!;
        public long OlcuBytes { get; set; }

        public string Sha256 { get; set; } = null!;
        public string Yol { get; set; } = null!;           // server path

        public bool AktivVersiya { get; set; } = true;
    }
}
