using FinNex.Application.DTOs.HR.Ezamiyyet;
using FinNex.Application.DTOs.HR.Icaze;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Domain.Entities.HR;

namespace FinNex.UI.Areas.User.ViewModels.Tesdiq
{
    public class TesdiqIndexVM
    {
        public List<MezuniyyetListDto> Mezuniyyetler { get; set; } = new();
        public List<IcazeListDto> Icazeler { get; set; } = new();
        public List<EzamiyyetMuracietListDto> Ezamiyyetler { get; set; } = new();
        public string RolBasliq { get; set; } = "";
        public string RolAciqlamasi { get; set; } = "";
        public StrukturRolTipi Rol { get; set; }

        public int MezuniyyetSayi => Mezuniyyetler.Count;
        public int IcazeSayi => Icazeler.Count;
        public int EzamiyyetSayi => Ezamiyyetler.Count;
        public int CemiSayi => MezuniyyetSayi + IcazeSayi + EzamiyyetSayi;
    }
}
