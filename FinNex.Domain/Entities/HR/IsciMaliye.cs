namespace FinNex.Domain.Entities.HR
{
    public class IsciMaliye : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public decimal CariMaas { get; set; } // Müqavilə üzrə cari ştat maaşı
        public int MezuniyyetQaliqGunu { get; set; } = 21;
        public string? BankHesabNo { get; set; } // IBAN
        public string? SosialSigortaNo { get; set; } // SSN
    }
}