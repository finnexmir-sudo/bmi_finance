using FinNex.Application.DTOs.Kassa;

namespace FinNex.UI.Areas.Kassa.ViewModels
{
    public class ExchangeIndexVM
    {
        public KassaKursGunlukDto Gunluk { get; set; } = null!;
        public IList<KassaKursSiyahiDto> SonQeydler { get; set; } = new List<KassaKursSiyahiDto>();
    }
}
