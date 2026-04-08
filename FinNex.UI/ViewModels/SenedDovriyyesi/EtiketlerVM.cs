namespace FinNex.UI.ViewModels.SenedDovriyyesi;

public class EtiketlerVM
{
    public List<EtiketItemVM> Etiketler { get; set; } = new();
}

public class EtiketItemVM
{
    public int Id { get; set; }
    public string Ad { get; set; } = null!;
    public int SenedSayi { get; set; }
    public DateTime YaradilmaTarixi { get; set; }
}
