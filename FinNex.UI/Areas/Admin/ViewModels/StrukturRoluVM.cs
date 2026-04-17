using System.ComponentModel.DataAnnotations;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.Admin.ViewModels;

public class StrukturRoluItemVM
{
    public int Id { get; set; }
    public int IsciId { get; set; }
    public string IsciAdSoyad { get; set; } = "";
    public StrukturRolTipi RolTipi { get; set; }
    public int? DepartamentId { get; set; }
    public string? DepartamentAd { get; set; }
    public DateTime BaslamaTarixi { get; set; }
    public DateTime? BitmeTarixi { get; set; }
    public bool Aktivdir { get; set; }
}

public class StrukturRoluIndexVM
{
    public List<StrukturRoluItemVM> Items { get; set; } = new();

    // Filtrlər
    public StrukturRolTipi? RolFilter { get; set; }
    public int? DepartamentFilter { get; set; }
    public List<SelectListItem> Departamentler { get; set; } = new();

    // Statistikalar
    public int UmumiSayi { get; set; }
    public int SobeReisiSayi { get; set; }
    public int RehberSayi { get; set; }
    public int HrSayi { get; set; }
}

public class StrukturRoluFormVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "İşçi seçilməlidir.")]
    [Display(Name = "İşçi")]
    public int IsciId { get; set; }

    [Required(ErrorMessage = "Rol tipi seçilməlidir.")]
    [Display(Name = "Rol")]
    public StrukturRolTipi RolTipi { get; set; }

    [Display(Name = "Departament")]
    public int? DepartamentId { get; set; }

    [Required]
    [Display(Name = "Başlama tarixi")]
    [DataType(DataType.Date)]
    public DateTime BaslamaTarixi { get; set; }

    [Display(Name = "Bitmə tarixi")]
    [DataType(DataType.Date)]
    public DateTime? BitmeTarixi { get; set; }

    [Display(Name = "Aktiv")]
    public bool Aktivdir { get; set; } = true;

    // Dropdown məlumatları
    public List<SelectListItem> Isciler { get; set; } = new();
    public List<SelectListItem> Departamentler { get; set; } = new();
}
