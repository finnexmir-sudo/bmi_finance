namespace FinNex.Application.DTOs.PR_Document
{
    public class SenedFaylDto
    {
        public int Id { get; set; }
        public string OriginalAd { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long OlcuBytes { get; set; }
        public int VersiyaNo { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }
}
