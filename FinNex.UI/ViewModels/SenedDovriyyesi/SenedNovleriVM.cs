namespace FinNex.UI.ViewModels.SenedDovriyyesi;

public class SenedNovleriVM
{
    public List<SenedNovuItemVM> Novler { get; set; } = new();
    public List<DropdownItemVM> Sobeler { get; set; } = new();
}

public class SenedNovuItemVM
{
    public int Id { get; set; }
    public string Kod { get; set; } = null!;
    public string Ad { get; set; } = null!;
    public int SobeId { get; set; }
    public string SobeAd { get; set; } = null!;
    public bool Aktiv { get; set; }
    public DateTime YaradilmaTarixi { get; set; }
}
