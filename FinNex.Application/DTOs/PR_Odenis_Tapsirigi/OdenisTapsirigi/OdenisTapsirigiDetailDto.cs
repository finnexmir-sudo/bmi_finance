
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi
{
    public class OdenisTapsirigiDetailDto : BaseDto
    {
        public string Nomre { get; set; } = null!;
        public DateTime Tarix { get; set; }

        // Mebleg
        public decimal Mebleg { get; set; }
        public string Valyuta { get; set; } = null!;
        public string? MeblegYazi { get; set; }

        // Teyinat
        public string Teyinat { get; set; } = null!;
        public string? ElaveInformasiya { get; set; }
        public string? BudceTesnifatininKodu { get; set; }
        public string? BudceSeviyyesininKodu { get; set; }

        // A1 - Oduyun bank
        public string OduyenBankAdi { get; set; } = null!;

        // A2 - Oduyun musteri
        public string OduyenMusteriAdi { get; set; } = null!;
        public string OduyenHesabIban { get; set; } = null!;

        // B1 - Alan bank
        public string AlanBankAdi { get; set; } = null!;

        // B2 - Alan musteri
        public string AlanMusteriAdi { get; set; } = null!;
        public string AlanHesabIban { get; set; } = null!;
    }
}
