namespace FinNex.Domain.Entities.HR
{
    // DETAIL: Maaşın tərkibindəki hər bir hərəkət (Maaş, Vergi, Bonus və s.)
    public class MaasDetay : BaseEntity
    {
        public int MaasId { get; set; }
        public Maas Maas { get; set; } = null!;

        public int MaasNovuId { get; set; }
        public MaasNovu MaasNovu { get; set; } = null!;

        public decimal Mebleg { get; set; } // Rəqəmsal dəyər
        public string? Aciqlama { get; set; } // Məs: "Gecikmə cəriməsi"
    }
}