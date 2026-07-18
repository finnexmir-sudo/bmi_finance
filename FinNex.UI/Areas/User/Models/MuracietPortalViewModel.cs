using FinNex.Application.DTOs.HR.Icaze;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.DTOs.HR.Ezamiyyet;
using FinNex.Domain.Entities.HR;

namespace FinNex.UI.Areas.User.Models
{
    /// <summary>
    /// Müraciət portalı (Məzuniyyət + İcazə + Ezamiyyət) ViewModel.
    /// </summary>
    public class MuracietPortalViewModel
    {
        public string AktivTab { get; set; } = "mezuniyyet";

        public List<MezuniyyetListDto> Mezuniyyetler { get; set; } = new();
        public List<IcazeListDto> Icazeler { get; set; } = new();
        public List<EzamiyyetMuracietListDto> Ezamiyyetler { get; set; } = new();

        // ── Məzuniyyət statistikası ────────────────────────────
        public int MezuniyyetCemi => Mezuniyyetler.Count;

        public int MezuniyyetTesdiq => Mezuniyyetler
            .Count(x => x.WorkflowMerhele == "Təsdiqlənib");

        public int MezuniyyetGozlemede => Mezuniyyetler
            .Count(x => x.WorkflowMerhele != "Təsdiqlənib"
                     && x.WorkflowMerhele != "İmtina edildi");

        // ── İcazə statistikası ─────────────────────────────────
        public int IcazeCemi => Icazeler.Count;

        public int IcazeTesdiq => Icazeler
            .Count(x => x.WorkflowMerhele == "Təsdiqlənib");

        public int IcazeGozlemede => Icazeler
            .Count(x => x.WorkflowMerhele != "Təsdiqlənib"
                     && x.WorkflowMerhele != "İmtina edildi");

        // ── Ezamiyyət statistikası ─────────────────────────────
        public int EzamiyyetCemi => Ezamiyyetler.Count;
        public int EzamiyyetTesdiq => Ezamiyyetler.Count(x => x.Status == EzamiyyetStatus.Tesdiqlendi);
        public int EzamiyyetGozlemede => Ezamiyyetler.Count(x => x.Status == EzamiyyetStatus.Gozleyir);
    }
}
