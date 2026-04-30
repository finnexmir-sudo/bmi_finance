using FinNex.Application.DTOs.HR.Icaze;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.User.ViewModels.Icaze
{
    public class IcazeIzlemeVM
    {
        public List<IcazeIsciIstatistikDto> IsciIstatistikler { get; set; } = new();
        public IcazeIzlemeFiltrDto Filtr { get; set; } = new();
        public List<SelectListItem> Departamentler { get; set; } = new();

        public int UmumIsciSayi => IsciIstatistikler.Count;
        public int UmumMuraciet => IsciIstatistikler.Sum(x => x.CemiMuraciet);
        public double UmumTesdiqSaat => IsciIstatistikler.Sum(x => x.TesdiqSaat);
        public int UmumTesdiq => IsciIstatistikler.Sum(x => x.TesdiqlenibSayi);
        public int UmumGozleme => IsciIstatistikler.Sum(x => x.GozlemeSayi);
        public int UmumImtina => IsciIstatistikler.Sum(x => x.ImtinaEdildiSayi);
    }
}
