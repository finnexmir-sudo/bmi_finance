using FinNex.Application.DTOs.HR.Mezuniyyet;

namespace FinNex.UI.Areas.HR.ViewModels.Mezuniyyet
{
    public class HrMezuniyyetIndexVM
    {
        public List<MezuniyyetListDto> Mezuniyyetler { get; set; } = new();
        public string PageTitle { get; set; } = "";
        public string TesdiqAction { get; set; } = ""; // POST action adı
    }
    public class HrMezuniyyetDetalVM
    {
        public MezuniyyetDto Mezuniyyet { get; set; } = null!;
        public string ReturnAction { get; set; } = "Hr"; // geri qayıdacaq action

        // Təsdiqçinin görməsi üçün əlavə kontekst
        public List<MezuniyyetOverlapDto> OverlapMezuniyyetler { get; set; } = new();
        public List<EvezediciKonfliktDto> EvezediciKonfliktleri { get; set; } = new();
    }
}
