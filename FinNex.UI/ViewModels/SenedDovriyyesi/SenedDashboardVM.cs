using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.UI.ViewModels.SenedDovriyyesi;

public class SenedDashboardVM
{
    public int UmumiSenedler { get; set; }
    public int YeniSenedler { get; set; }
    public int YoxlanilirSenedler { get; set; }
    public int TesdiqSenedler { get; set; }
    public int ArxivSenedler { get; set; }
    public List<SenedDashboardItemVM> SonSenedler { get; set; } = new();
}

public class SenedDashboardItemVM
{
    public int Id { get; set; }
    public string Basliq { get; set; } = null!;
    public string Sobe { get; set; } = null!;
    public string SenedNovu { get; set; } = null!;
    public SenedStatusu Status { get; set; }
    public int FaylSayi { get; set; }
    public DateTime YaradilmaTarixi { get; set; }
}
