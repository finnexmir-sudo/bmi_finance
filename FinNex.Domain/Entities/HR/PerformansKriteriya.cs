namespace FinNex.Domain.Entities.HR
{
    public class PerformansKriteriya : BaseEntity
    {
        public int PerformansId { get; set; }
        public PerformansQiymetlendirme Performans { get; set; } = null!;

        public string KriteriyaAdi { get; set; } = null!;
        public decimal Ceki { get; set; } // % çəki

        public decimal? IsciQiymeti { get; set; }
        public decimal? MudirQiymeti { get; set; }   // Rehber1
        public decimal? Rehber2Qiymeti { get; set; } // Rehber2

        public string? IsciSherhi { get; set; }
        public string? MudirSherhi { get; set; }
        public string? Rehber2Sherhi { get; set; }

        public decimal? SobeReisiQiymeti { get; set; }
        public string? SobeReisiSherhi { get; set; }
    }
}
