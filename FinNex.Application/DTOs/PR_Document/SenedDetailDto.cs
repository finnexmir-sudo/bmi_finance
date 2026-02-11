namespace FinNex.Application.DTOs.PR_Document
{
    public class SenedDetailDto : BaseDto
    {
        public int SobeId { get; set; }
        public string SobeAd { get; set; } = null!;
        public int SenedNovuId { get; set; }
        public string SenedNovuAd { get; set; } = null!;
        public string Basliq { get; set; } = null!;
        public string AcarSoz { get; set; } = null!;
        public string StatusAd { get; set; } = null!;
        public int StatusDeger { get; set; }
        public List<SenedFaylDto> Fayllar { get; set; } = new();
    }
}
