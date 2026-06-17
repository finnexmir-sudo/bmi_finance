using FinNex.Application.DTOs.HR.Icaze;

namespace FinNex.UI.Areas.User.ViewModels.Icaze
{
    /// <summary>
    /// İcazə siyahı səhifəsi üçün ViewModel.
    /// Domain entity-lərinə istinad yoxdur.
    /// </summary>
    public class IcazeIndexVM
    {
        public List<IcazeListDto> Icazeler { get; set; } = new();

        // ── Statistika (string müqayisəsi) ─────────────────────
        public int Cemi => Icazeler.Count;

        public int TesdiqCem => Icazeler
            .Count(x => x.WorkflowMerhele == "Təsdiqlənib");

        public int GozlemeCem => Icazeler
            .Count(x => x.WorkflowMerhele != "Təsdiqlənib"
                     && x.WorkflowMerhele != "İmtina edildi");

        public int ImtinaCem => Icazeler
            .Count(x => x.WorkflowMerhele == "İmtina edildi");

        // Təsdiqlənmiş icazələrin FAKTİKİ istifadəsi (faktiki varsa o, yoxdursa planlaşdırılan).
        public double IstifadeOlunanSaat => Icazeler
            .Where(x => x.WorkflowMerhele == "Təsdiqlənib")
            .Sum(x => x.IstifadeSaati);
    }
}
