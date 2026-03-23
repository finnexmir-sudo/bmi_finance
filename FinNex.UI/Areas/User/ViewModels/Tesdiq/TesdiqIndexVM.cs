using FinNex.Application.DTOs.HR.Icaze;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Domain.Entities.HR;

namespace FinNex.UI.Areas.User.ViewModels.Tesdiq
{
    public class TesdiqIndexVM
    {
        public List<MezuniyyetListDto> Mezuniyyetler { get; set; } = new();
        public List<IcazeListDto> Icazeler { get; set; } = new();
        public string RolBasliq { get; set; } = "";
        public string RolAciqlamasi { get; set; } = "";
        public StrukturRolTipi Rol { get; set; }

        public int MezuniyyetSayi => Mezuniyyetler.Count;
        public int IcazeSayi => Icazeler.Count;
        public int CemiSayi => MezuniyyetSayi + IcazeSayi;
    }
}
