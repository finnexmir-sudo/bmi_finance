using Microsoft.AspNetCore.Http;

namespace FinNex.Application.DTOs.HR.Ezamiyyet
{
    public class EzamiyyetMuracietCreateDto
    {
        public string   Baslig        { get; set; } = null!;
        public int?     MekanId       { get; set; }
        public string?  YeniMekanAd   { get; set; }
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi   { get; set; }
        public string?  BaslamaSaati  { get; set; }
        public string?  BitisSaati    { get; set; }
        public string?  Qeyd          { get; set; }
        public IFormFile? Sened       { get; set; }
    }
}
