namespace FinNex.Application.DTOs.SenedDovriyyesi.Sened;

public class SenedDashboardDto
{
    public int UmumiSenedler { get; set; }
    public int YeniSenedler { get; set; }
    public int YoxlanilirSenedler { get; set; }
    public int TesdiqSenedler { get; set; }
    public int ArxivSenedler { get; set; }
    public List<SenedListDto> SonSenedler { get; set; } = new();
}
