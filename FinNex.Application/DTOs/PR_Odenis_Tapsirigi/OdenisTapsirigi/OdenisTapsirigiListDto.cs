
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi
{
    public class OdenisTapsirigiListDto : BaseDto
    {
        public string Nomre { get; set; } = null!;
        public DateTime Tarix { get; set; }
        public decimal Mebleg { get; set; }
        public string Valyuta { get; set; } = null!;
    }
}
