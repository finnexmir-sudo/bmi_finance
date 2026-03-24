using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Isci
{
    public class IsciListDto : BaseDto
    {
        public string TamAd { get; set; } = null!;
        public string FIN { get; set; } = null!;

        // IsciTeyinat-dan (aktiv)
        public string? SobeAdi { get; set; }
        public string? VezifeAdi { get; set; }

        public string? Email { get; set; }
        public string? Telefon { get; set; }

        // IsciMaliye-dan
        public decimal CariMaas { get; set; }

        public IsciStatus Status { get; set; }
    }
}
