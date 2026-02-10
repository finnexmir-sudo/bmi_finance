
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
        public string Valyuta { get; set; } = null!;
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

    public class OdenisTapsirigiListDto : BaseDto
    {
        public string Nomre { get; set; } = null!;
        public DateTime Tarix { get; set; }
        public decimal Mebleg { get; set; }
        public string Valyuta { get; set; } = null!;
    }

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
