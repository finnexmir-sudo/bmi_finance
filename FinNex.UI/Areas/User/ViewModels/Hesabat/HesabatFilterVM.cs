// Areas/User/ViewModels/Hesabat/HesabatFilterVM.cs
namespace FinNex.UI.Areas.User.ViewModels.Hesabat;

public class HesabatFilterVM
{
    public DateTime? BaslamaTarixFrom { get; set; }
    public DateTime? BaslamaTarixTo { get; set; }
    public int? DepartamentId { get; set; }
    public string? Nov { get; set; }        // "Mezuniyyet" | "Icaze" | null
    public int? Status { get; set; }
    public string? Axtaris { get; set; }    // işçi adı
}

public class HesabatIndexVM
{
    public HesabatFilterVM Filter { get; set; } = new();

    public List<HesabatSatirVM> Satirlar { get; set; } = new();

    // Statistika
    public int CemiMezuniyyet { get; set; }
    public int CemiIcaze { get; set; }
    public int Gozleyen { get; set; }
    public int Tesdiqlenib { get; set; }
    public int ImtinaEdildi { get; set; }
    public int BuAyMezuniyyet { get; set; }
    public int BuAyIcaze { get; set; }

    // Dropdown-lar üçün
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Departamentler { get; set; } = new();
}

public class HesabatSatirVM
{
    public int Id { get; set; }
    public string Nov { get; set; } = "";           // "Məzuniyyət" | "İcazə"
    public string NovIcon { get; set; } = "";
    public string IsciAdSoyad { get; set; } = "";
    public string SobeAdi { get; set; } = "";
    public string VezifeAdi { get; set; } = "";

    // Məzuniyyət sahələri
    public string? MezuniyyetNov { get; set; }
    public DateTime? BaslamaTarixi { get; set; }
    public DateTime? BitmeTarixi { get; set; }
    public int? IsGunlerininSayi { get; set; }

    // İcazə sahələri
    public DateTime? IcazeTarixi { get; set; }
    public TimeSpan? BaslamaSaati { get; set; }
    public TimeSpan? BitisSaati { get; set; }
    public double? IcazeSaati { get; set; }

    public string StatusText { get; set; } = "";
    public int StatusValue { get; set; }
    public string EvezEden { get; set; } = "";

    // Təsdiq zənciri
    public bool? SobeReisiTesdiq { get; set; }
    public string? SobeReisiAd { get; set; }
    public DateTime? SobeReisiTesdiqTarixi { get; set; }

    public bool? RehberTesdiq { get; set; }
    public string? RehberAd { get; set; }
    public DateTime? RehberTesdiqTarixi { get; set; }

    public bool? HrTesdiq { get; set; }
    public string? HrAd { get; set; }
    public DateTime? HrTesdiqTarixi { get; set; }

    public string? ImtinaSebebi { get; set; }
    public DateTime YaradilmaTarixi { get; set; }
}