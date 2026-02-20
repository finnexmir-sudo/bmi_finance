using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

public class IsciCreateVM
{
    [Required]
    [Display(Name = "Ad")]
    public string Ad { get; set; } = null!;

    [Required]
    [Display(Name = "Soyad")]
    public string Soyad { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Telefon { get; set; } = null!;

    [Required]
    [Display(Name = "Şöbə")]
    public int DepartmentId { get; set; }

    [Required]
    [Display(Name = "İşə qəbul tarixi")]
    public DateTime IseQebulTarixi { get; set; } = DateTime.Today;

    public List<SelectListItem> Departments { get; set; } = new();
}
