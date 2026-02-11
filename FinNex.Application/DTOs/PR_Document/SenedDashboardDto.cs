namespace FinNex.Application.DTOs.PR_Document
{
    public class SenedDashboardDto
    {
        public int UmumiSened { get; set; }
        public int YeniSened { get; set; }
        public int TesdiqSened { get; set; }
        public int ArxivSened { get; set; }
        public List<SenedListDto> SonSenedler { get; set; } = new();
    }
}
