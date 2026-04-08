namespace FinNex.UI.ViewModels.SenedDovriyyesi;

public class IcazelerVM
{
    public List<IcazeItemVM> Icazeler { get; set; } = new();
    public List<DropdownItemVM> Istifadeciler { get; set; } = new();
    public List<DropdownItemVM> Sobeler { get; set; } = new();
}

public class IcazeItemVM
{
    public int Id { get; set; }
    public int IstifadeciId { get; set; }
    public string IstifadeciAd { get; set; } = null!;
    public int SobeId { get; set; }
    public string SobeAd { get; set; } = null!;
    public int IcazeNovu { get; set; }
    public string IcazeNovuAd => IcazeNovu == 1 ? "Baxis" : "Tam";
}
