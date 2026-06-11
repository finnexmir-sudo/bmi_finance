using FinNex.Application.DTOs.HR.Ezamiyyet;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels.Ezamiyyet
{
    public class EzamiyyetIzlemeVM
    {
        public List<EzamiyyetIsciIzlemeDto> Isciler { get; set; } = new();
        public EzamiyyetFiltrDto Filtr { get; set; } = new();
        public List<SelectListItem> Departamentler { get; set; } = new();

        public int    UmumIsci    => Isciler.Count;
        public int    UmumEzam    => Isciler.Sum(x => x.CemiEzam);
        public int    UmumGun     => Isciler.Sum(x => x.CemiGun);
        public int    UmumTesdiq  => Isciler.Sum(x => x.TesdiqlenibSayi);
        public double UmumFaktiki => Isciler.Sum(x => x.FaktikiSaat);
    }
}
