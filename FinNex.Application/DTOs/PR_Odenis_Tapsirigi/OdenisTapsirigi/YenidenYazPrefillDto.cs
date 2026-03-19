
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi
{
    public class YenidenYazPrefillDto
    {
        public int? OduyenBankId { get; set; }
        public string? OduyenBankAd { get; set; }
        public string? OduyenBankKod { get; set; }
        public string? OduyenBankVoen { get; set; }
        public string? OduyenBankSwift { get; set; }
        public int? OduyenHesabId { get; set; }
        public string? OduyenHesabIban { get; set; }

        public int? OduyenMusteriId { get; set; }
        public string? OduyenMusteriAd { get; set; }
        public string? OduyenMusteriVoen { get; set; }

        public int? AlanBankId { get; set; }
        public string? AlanBankAd { get; set; }
        public string? AlanBankKod { get; set; }
        public string? AlanBankVoen { get; set; }
        public string? AlanBankSwift { get; set; }
        public int? AlanHesabId { get; set; }
        public string? AlanHesabIban { get; set; }

        public int? AlanMusteriId { get; set; }
        public string? AlanMusteriAd { get; set; }
        public string? AlanMusteriVoen { get; set; }
        public string? AlanMusteriHesabIban { get; set; }

        public string? Valyuta { get; set; }
        public decimal Mebleg { get; set; }
        public string? MeblegYazi { get; set; }
        public string? Teyinat { get; set; }
        public string? ElaveInformasiya { get; set; }
        public string? BudceTesnifatininKodu { get; set; }
        public string? BudceSeviyyesininKodu { get; set; }
    }
}
