using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Isci
{
    // List üçün (Siyahıda görünəcəklər)
    public class IsciListDto : BaseDto
    {
        // Ad və Soyadı ayrı-ayrı saxlamağa ehtiyac yoxdur, 
        // çünki siyahıda (Grid-də) adətən bütöv görünür.
        public string TamAd { get; set; } = null!;
        public string FIN { get; set; } = null!;

        // Struktur məlumatları
        public string SobeAdi { get; set; } = null!;
        public string VezifeAdi { get; set; } = null!;
        public string? Email {  get; set; }

        // Maliyyə məlumatı
        public decimal CariMaas { get; set; }

        public IsciStatus Status { get; set; }
    }

}
