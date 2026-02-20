
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Musteri
{
    public class MusteriDetailDto : BaseDto
    {
        public string Ad { get; set; } = null!;
        public string Voen { get; set; } = null!;

        // Əlaqəli məlumatlar (istəyə görə)
        public List<MusteriListDto> Hesablar { get; set; } = new();
    }
}
