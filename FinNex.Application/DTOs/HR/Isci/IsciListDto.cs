using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Isci
{
    public class IsciListDto
    {
        public int Id { get; set; }

        public string TamAd { get; set; } = null!;
        public string FIN { get; set; } = null!;

        public string Sobe { get; set; } = null!;
        public string Vezife { get; set; } = null!;
        public string Email { get; set; } = null!;
        public IsciStatus Status { get; set; }
    }

}
