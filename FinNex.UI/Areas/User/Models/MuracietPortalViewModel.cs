using FinNex.Application.DTOs.HR.Icaze;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Domain.Entities.HR;

namespace FinNex.UI.Areas.User.Models
{
    public class MuracietPortalViewModel
    {
        public string AktivTab { get; set; } = "mezuniyyet";

        public List<MezuniyyetListDto> Mezuniyyetler { get; set; } = new();
        public List<IcazeListDto> Icazeler { get; set; } = new();

        // Statistika
        public int MezuniyyetCemi => Mezuniyyetler.Count;
        public int MezuniyyetTesdiq => Mezuniyyetler.Count(x => x.Status == MezuniyyetStatus.Tesdiqlenib);
        public int MezuniyyetGozlemede => Mezuniyyetler.Count(x =>
            x.Status != MezuniyyetStatus.Tesdiqlenib &&
            x.Status != MezuniyyetStatus.ImtinaEdildi);

        public int IcazeCemi => Icazeler.Count;
        public int IcazeTesdiq => Icazeler.Count(x => x.Status == IcazeStatus.Tesdiqlenib);
        public int IcazeGozlemede => Icazeler.Count(x =>
            x.Status != IcazeStatus.Tesdiqlenib &&
            x.Status != IcazeStatus.ImtinaEdildi);
    }
}