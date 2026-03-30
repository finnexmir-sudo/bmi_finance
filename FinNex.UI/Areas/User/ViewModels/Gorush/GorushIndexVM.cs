// Areas/User/ViewModels/Gorush/GorushIndexVM.cs
using FinNex.Application.DTOs.Communication;
using FinNex.Domain.Entities.Communication;

namespace FinNex.UI.Areas.User.ViewModels.Gorush
{
    public class GorushIndexVM
    {
        public string AktivTab { get; set; } = "menim";
        public List<GorushListDto> MenimGorushler { get; set; } = new();
        public List<GorushListDto> TeshkilEtdiklerim { get; set; } = new();

        public List<GorushListDto> YaxinlashanGorushler =>
            MenimGorushler
                .Where(x => x.Tarix >= DateTime.Today &&
                            x.Tarix <= DateTime.Today.AddDays(7) &&
                            x.Status == GorushStatus.Planlanib)
                .OrderBy(x => x.Tarix)
                .ToList();
    }
}