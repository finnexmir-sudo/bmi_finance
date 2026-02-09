
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Musteri
{
    public class MusteriListDto : BaseDto
    {
        public string Ad { get; set; } = null!;
        public string Voen { get; set; } = null!;
    }
    public class MusteriCreateDto
    {
        public string Ad { get; set; } = null!;
        public string Voen { get; set; } = null!;
    }
    public class MusteriUpdateDto : BaseDto
    {

        public string Ad { get; set; } = null!;

        // Voen adətən dəyişdirilmir
    }
    public class MusteriDetailDto : BaseDto
    {
        public string Ad { get; set; } = null!;
        public string Voen { get; set; } = null!;

        // Əlaqəli məlumatlar (istəyə görə)
        public List<MusteriHesabiDto> Hesablar { get; set; } = new();
    }
}
