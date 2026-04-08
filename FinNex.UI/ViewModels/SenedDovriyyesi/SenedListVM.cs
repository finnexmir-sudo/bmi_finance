using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.UI.ViewModels.SenedDovriyyesi;

public class SenedListVM
{
    // Filter values
    public int? SobeId { get; set; }
    public int? SenedNovuId { get; set; }
    public SenedStatusu? Status { get; set; }
    public string? AxtarisKelimesi { get; set; }
    public bool Silinmisler { get; set; }

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // Data
    public List<SenedListItemVM> Senedler { get; set; } = new();

    // Dropdowns
    public List<DropdownItemVM> Sobeler { get; set; } = new();
    public List<DropdownItemVM> SenedNovleri { get; set; } = new();
}

public class SenedListItemVM
{
    public int Id { get; set; }
    public string? SenedNomresi { get; set; }
    public string Basliq { get; set; } = null!;
    public string AcarSoz { get; set; } = null!;
    public SenedStatusu Status { get; set; }
    public string Sobe { get; set; } = null!;
    public string IsciAdi { get; set; }=null!;
    public string SenedNovu { get; set; } = null!;
    public int FaylSayi { get; set; }
    public DateTime YaradilmaTarixi { get; set; }
    public bool Silinib { get; set; }
}

public class DropdownItemVM
{
    public int Id { get; set; }
    public string Ad { get; set; } = null!;
}
