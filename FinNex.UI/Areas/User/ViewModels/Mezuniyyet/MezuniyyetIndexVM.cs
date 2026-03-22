using FinNex.Application.DTOs.HR.Mezuniyyet;

namespace FinNex.UI.Areas.User.ViewModels.Mezuniyyet
{
    public class MezuniyyetIndexVM
    {
        // ── Siyahı ────────────────────────────────────────────
        public List<MezuniyyetListDto> Mezuniyyetler { get; set; } = new();

        // ── Statistika (string müqayisəsi, entity enum yox) ──
        public int Cemi => Mezuniyyetler.Count;

        public int TesdiqCem => Mezuniyyetler
            .Count(x => x.WorkflowMerhele == "Təsdiqlənib");

        public int GozlemeCem => Mezuniyyetler
            .Count(x => x.WorkflowMerhele != "Təsdiqlənib"
                     && x.WorkflowMerhele != "İmtina edildi");

        public int ImtinaCem => Mezuniyyetler
            .Count(x => x.WorkflowMerhele == "İmtina edildi");

        public int TesdiqGun => Mezuniyyetler
            .Where(x => x.WorkflowMerhele == "Təsdiqlənib")
            .Sum(x => x.IsGunlerininSayi);

        // ── Məzuniyyət qalığı ──────────────────────────────────
        public int IllikToplamGun { get; set; }
        public int IllikIstifadeGun { get; set; }
        public int IllikQaligGun => IllikToplamGun - IllikIstifadeGun;

        public int XestelikToplamGun { get; set; }
        public int XestelikIstifadeGun { get; set; }
        public int XestelikQaligGun => XestelikToplamGun - XestelikIstifadeGun;

        public int EzamiyyetToplamGun { get; set; }
        public int EzamiyyetIstifadeGun { get; set; }
        public int EzamiyyetQaligGun => EzamiyyetToplamGun - EzamiyyetIstifadeGun;

        // ── Filtr ─────────────────────────────────────────────
        public string? FilterNov { get; set; }
        public string? FilterStatus { get; set; }

        public IEnumerable<MezuniyyetListDto> FilteredList => Mezuniyyetler
            .Where(x => string.IsNullOrEmpty(FilterNov) || x.NovText == FilterNov)
            .Where(x => string.IsNullOrEmpty(FilterStatus) || x.WorkflowMerhele == FilterStatus);
    }
}
