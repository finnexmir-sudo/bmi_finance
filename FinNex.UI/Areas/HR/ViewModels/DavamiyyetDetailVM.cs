using FinNex.Domain.Entities.HR;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class DavamiyyetDetailVM
    {
        public int Id { get; set; }
        public string IsciTamAd { get; set; } = null!;
        public string Sobe { get; set; } = null!;
        public string Vezife { get; set; } = null!;
        public DateTime Tarix { get; set; }
        public DateTime? GirisVaxti { get; set; }
        public DateTime? CixisVaxti { get; set; }
        public DavamiyyetStatus Status { get; set; }

        // Computed
        public TimeSpan? IsSaati =>
            GirisVaxti.HasValue && CixisVaxti.HasValue
                ? CixisVaxti.Value - GirisVaxti.Value
                : null;
    }
}
