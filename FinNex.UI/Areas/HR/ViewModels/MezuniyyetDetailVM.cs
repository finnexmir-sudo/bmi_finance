using FinNex.Domain.Entities.HR;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MezuniyyetDetailVM
    {
        public int Id { get; set; }
        public string IsciTamAd { get; set; } = null!;
        public string Sobe { get; set; } = null!;
        public string Vezife { get; set; } = null!;
        public MezuniyyetNovu Nov { get; set; }
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public string? Qeyd { get; set; }
        public MezuniyyetStatus Status { get; set; }

        // Computed
        public int GunSayi => (BitmeTarixi - BaslamaTarixi).Days + 1;
    }
}
