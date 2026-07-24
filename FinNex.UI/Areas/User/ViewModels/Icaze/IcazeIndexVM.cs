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

        // Təsdiqlənmiş icazələrin FAKTİKİ istifadəsi — yalnız jeton (mükafat) çıxılır.
        // Nahar artıq IstifadeSaati-nin içində (EffektivFaktikiSaat/EffektivSaat, real
        // kəsişmə ilə) çıxılıb — burada TƏKRAR çıxılmamalıdır (ikiqat olmasın).
        public double IstifadeOlunanSaat => Icazeler
            .Where(x => x.WorkflowMerhele == "Təsdiqlənib")
            .Sum(x => Math.Max(0.0, x.IstifadeSaati - (double)x.JetonOdenenSaat));
    }
}
