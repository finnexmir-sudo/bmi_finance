
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Davamiyyet
{
    public class DavamiyyetCreateDto
    {
        public int IsciId { get; set; }

        public DateTime Tarix { get; set; }

        public DateTime? GirisVaxti { get; set; }
        public DateTime? CixisVaxti { get; set; }

        public DavamiyyetStatus Status { get; set; }
    }

}
