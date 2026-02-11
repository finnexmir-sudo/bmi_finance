namespace FinNex.Application.DTOs.SenedDovriyyesi.Fayl
{
    public class SenedFaylUploadDto
    {
        public int SenedId { get; set; }
        public string OriginalAd { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long OlcuBytes { get; set; }
        public Stream Stream { get; set; } = null!;
    }
    public class SenedFaylDto
    {
        public int Id { get; set; }
        public int VersiyaNo { get; set; }
        public string OriginalAd { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long OlcuBytes { get; set; }
        public bool AktivVersiya { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }
}
