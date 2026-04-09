namespace FinNex.Domain.Entities.HR
{
    public class PerformansQiymetlendirme : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int QiymetlendirenIsciId { get; set; }
        public Isci QiymetlendirenIsci { get; set; } = null!;

        public PerformansDovrTipi DovrTipi { get; set; }
        public int Il { get; set; }
        public int? Rubu { get; set; } // 1-4 (Rüblük üçün)

        public PerformansStatus Status { get; set; } = PerformansStatus.Gozlemede;

        public decimal IsciOrtalamaQiymet { get; set; }
        public decimal MudirOrtalamaQiymet { get; set; }
        public decimal YekunQiymet { get; set; }

        public string? IsciSherhi { get; set; }
        public string? MudirSherhi { get; set; }
        public string? InkisafPlani { get; set; }

        public DateTime? IsciQiymetlendirmeTarixi { get; set; }
        public DateTime? MudirQiymetlendirmeTarixi { get; set; }

        public ICollection<PerformansKriteriya> Kriteriyalar { get; set; } = new List<PerformansKriteriya>();
    }
}
