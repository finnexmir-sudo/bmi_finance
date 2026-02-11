using FinNex.Application.DTOs.PR_Document;

namespace FinNex.UI.Areas.PR_Document.ViewModels
{
    public class SenedDashboardVM
    {
        public int UmumiSened { get; set; }
        public int YeniSened { get; set; }
        public int TesdiqSened { get; set; }
        public int ArxivSened { get; set; }
        public List<SenedListDto> SonSenedler { get; set; } = new();
    }
}
