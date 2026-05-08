namespace FinNex.Domain.Entities.HR
{
    public class Icaze : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int? EvezEdenIsciId { get; set; }
        public Isci? EvezEdenIsci { get; set; }

        public DateTime IcazeTarixi { get; set; }
        public TimeSpan BaslamaSaati { get; set; }
        public TimeSpan BitisSaati { get; set; }
        public double IcazeSaati => (BitisSaati - BaslamaSaati).TotalHours;

        public string? Sebeb { get; set; }
        public string? ImtinaSebebi { get; set; }

        public IcazeStatus Status { get; set; } = IcazeStatus.Gozlemede;

        public bool? SobeReisiTesdiq { get; set; }
        public int? SobeReisiId { get; set; }
        public Isci? SobeReisi { get; set; }
        public DateTime? SobeReisiTesdiqTarixi { get; set; }

        public bool? RehberTesdiq { get; set; }
        public int? RehberId { get; set; }
        public Isci? Rehber { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }

        public bool? HrTesdiq { get; set; }
        public int? HrId { get; set; }
        public Isci? HrTesdiqleyen { get; set; }
        public DateTime? HrTesdiqTarixi { get; set; }

        // HR tərəfindən işarələnir: işçi geri dönmür (günün qalan hissəsini bitirir)
        public bool Birdefelik { get; set; } = false;

        // Bu icazənin neçə saatı jetonla ödənilir (0 = tam adi icazə).
        // Adi icazə saatı = IcazeSaati - JetonOdenenSaat.
        // HR final təsdiqdə işçinin aktiv jetonlarından FIFO ilə bu qədər saat tutulur.
        public decimal JetonOdenenSaat { get; set; } = 0;

        // Rəhbər təsdiqindən işarələnir: işçi nahar fasiləsini götürməyib,
        // buna görə 1 saat nahar icazə müddətindən çıxılır.
        public bool NaharNezereAlinmasin { get; set; } = false;

        public IcazeCixisGiris? CixisGiris { get; set; }
    }
}