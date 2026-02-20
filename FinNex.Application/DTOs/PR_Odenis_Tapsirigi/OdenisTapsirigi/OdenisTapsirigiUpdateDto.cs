
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi
{
    public class OdenisTapsirigiUpdateDto : BaseDto
    {
        public DateTime Tarix { get; set; }

        public int OduyenBankId { get; set; }

        public int OduyenMusteriId { get; set; }
        public int OduyenHesabId { get; set; }

        public int AlanBankId { get; set; }

        public int AlanMusteriId { get; set; }
        public int AlanHesabId { get; set; }

        public decimal Mebleg { get; set; }
        public string Valyuta { get; set; } = null!;
        public string? MeblegYazi { get; set; }

        public string Teyinat { get; set; } = null!;
        public string? ElaveInformasiya { get; set; }
        public string? BudceTesnifatininKodu { get; set; }
        public string? BudceSeviyyesininKodu { get; set; }
    }
}
