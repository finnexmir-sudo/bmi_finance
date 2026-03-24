using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Isci
{
    public class IsciListDto : BaseDto
    {
        public string TamAd { get; set; } = null!;
        public string FIN { get; set; } = null!;

        public string SobeAdi { get; set; } = null!;
        public string VezifeAdi { get; set; } = null!;

        public string? Email { get; set; }
        public string? Telefon { get; set; }

        public decimal CariMaas { get; set; }

        public IsciStatus Status { get; set; }
    }
}