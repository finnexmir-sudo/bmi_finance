using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Musteri;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

namespace FinNex.UI.Areas.PR_Odenis_Tapsirigi.ViewModels
{
    public class OdenisTapsirigiCreateVM
    {
        public OdenisTapsirigi Odenis { get; set; } = new();

        public List<Bank> Banklar { get; set; } = new();
        public List<Musteri> Musteriler { get; set; } = new();

        public List<BankHesabi> OduyenBankHesablari { get; set; } = new();
        public List<BankHesabi> AlanBankHesablari { get; set; } = new();

        public List<MusteriHesabi> OduyenMusteriHesablari { get; set; } = new();
        public List<MusteriHesabi> AlanMusteriHesablari { get; set; } = new();

        public MusteriCreateDto? MusteriCreateDto { get; set; }
        public YenidenYazPrefillDto? YenidenYazDto { get; set; }
    }
}
