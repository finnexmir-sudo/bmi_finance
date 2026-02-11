
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.MusteriHesabi
{
    public class MusteriHesabiUpdateDto : BaseDto
    {
        public string Iban { get; set; } = null!;
        public string Valyuta { get; set; } = null!;
    }
}
