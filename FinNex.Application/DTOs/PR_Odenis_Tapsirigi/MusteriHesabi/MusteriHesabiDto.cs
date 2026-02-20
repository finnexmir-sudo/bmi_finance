
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.MusteriHesabi
{
    public class MusteriHesabiDto : BaseDto
    {
        public string Adi { get; set; } = null!;
        public string VOEN { get; set; } = null!;
        public List<MusteriHesabiListDto> Hesablar { get; set; } = new();

    }
}
