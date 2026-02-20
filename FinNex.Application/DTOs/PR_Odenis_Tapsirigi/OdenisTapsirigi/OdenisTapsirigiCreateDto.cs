
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi
{
    public class OdenisTapsirigiCreateDto
    {
        public DateTime Tarix { get; set; }

        // A1 - Oduyun bank
        public int OduyenBankId { get; set; }

        // A2 - Oduyun musteri
        public int OduyenMusteriId { get; set; }
        public int OduyenHesabId { get; set; }

        // B1 - Alan bank
        public int AlanBankId { get; set; }

        // B2 - Alan musteri
        public int AlanMusteriId { get; set; }
        public int AlanHesabId { get; set; }

        // C - Mebleg
        public decimal Mebleg { get; set; }
        public int ValyutaId { get; set; }
        public string? MeblegYazi { get; set; }

        // D1 - Teyinat
        public string Teyinat { get; set; } = null!;

        // D2 - Elave informasiya
        public string? ElaveInformasiya { get; set; }

        // D3 - Budce tesnifatinin kodu
        public string? BudceTesnifatininKodu { get; set; }

        // D4 - Budce seviyyesinin kodu
        public string? BudceSeviyyesininKodu { get; set; }
    }
}
