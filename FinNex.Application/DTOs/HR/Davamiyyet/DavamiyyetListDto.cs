
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Davamiyyet
{
    public class DavamiyyetListDto
    {
        public int Id { get; set; }

        public string IsciTamAd { get; set; } = null!;
        public DateTime Tarix { get; set; }

        public DateTime? GirisVaxti { get; set; }
        public DateTime? CixisVaxti { get; set; }

        public DavamiyyetStatus Status { get; set; }
    }

}
