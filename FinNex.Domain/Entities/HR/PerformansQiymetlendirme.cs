namespace FinNex.Domain.Entities.HR
{
    public class PerformansQiymetlendirme : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        // Rehber1 (əsas rəhbər — əvvəlki QiymetlendirenIsciId)
        public int QiymetlendirenIsciId { get; set; }
        public Isci QiymetlendirenIsci { get; set; } = null!;

        // Rehber2 (ikinci / skip-level rəhbər — nullable)
        public int? Rehber2Id { get; set; }
        public Isci? Rehber2 { get; set; }

        public int? SobeReisiId { get; set; }
        public Isci? SobeReisi { get; set; }
        public decimal SobeReisiOrtalamaQiymet { get; set; }
        public string? SobeReisiSherhi { get; set; }
        public DateTime? SobeReisiQiymetlendirmeTarixi { get; set; }

        public PerformansDovrTipi DovrTipi { get; set; }
        public int Il { get; set; }
        public int? Rubu { get; set; }

        public PerformansStatus Status { get; set; } = PerformansStatus.Gozlemede;

        public KampaniyaTipi KampaniyaTipi { get; set; } = KampaniyaTipi.IsciQiymetlendirme;

        // Köhnə sahə — uyğunluq üçün saxlanılır
        public bool MenecerKriteriyalari { get; set; }

        public decimal IsciOrtalamaQiymet { get; set; }
        public decimal MudirOrtalamaQiymet { get; set; }   // Rehber1 ortalama
        public decimal Rehber2OrtalamaQiymet { get; set; } // Rehber2 ortalama
        public decimal YekunQiymet { get; set; }

        public string? IsciSherhi { get; set; }
        public string? MudirSherhi { get; set; }   // Rehber1 şərhi
        public string? Rehber2Sherhi { get; set; } // Rehber2 şərhi
        public string? InkisafPlani { get; set; }

        public DateTime? IsciQiymetlendirmeTarixi { get; set; }
        public DateTime? MudirQiymetlendirmeTarixi { get; set; }
        public DateTime? Rehber2QiymetlendirmeTarixi { get; set; }

        public ICollection<PerformansKriteriya> Kriteriyalar { get; set; } = new List<PerformansKriteriya>();
    }
}
