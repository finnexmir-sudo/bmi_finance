
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Davamiyyet
{
    public class DavamiyyetDetailDto
    {
        public int Id { get; set; }

        public string IsciTamAd { get; set; } = null!;
        public string Sobe { get; set; } = null!;
        public string Vezife { get; set; } = null!;

        public DateTime Tarix { get; set; }

        public DateTime? GirisVaxti { get; set; }
        public DateTime? CixisVaxti { get; set; }

        public DavamiyyetStatus Status { get; set; }
    }

}
