namespace FinNex.Domain.Entities.HR
{
    public class PerformansKriteriya : BaseEntity
    {
        public int PerformansId { get; set; }
        public PerformansQiymetlendirme Performans { get; set; } = null!;

        public string KriteriyaAdi { get; set; } = null!;
        public decimal Ceki { get; set; } // % çəki (məs: 20)

        public decimal? IsciQiymeti { get; set; } // 1-5
        public decimal? MudirQiymeti { get; set; } // 1-5

        public string? IsciSherhi { get; set; }
        public string? MudirSherhi { get; set; }
    }
}
