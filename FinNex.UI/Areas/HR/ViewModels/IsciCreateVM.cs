using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

public class IsciCreateVM
{
    [Required]
    [Display(Name = "İstifadəçi adı")]
    public string IstifadeciAd { get; set; } = null!;

    [Required]
    [Display(Name = "Ad")]
    public string Ad { get; set; } = null!;

    [Required]
    [Display(Name = "Soyad")]
    public string Soyad { get; set; } = null!;
    public string? AtaAdi { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Telefon { get; set; } = null!;

    [Required]
    public string SeriyaNomre { get; set; } = null!;
    [Required]

    public string Unvan { get; set; } = null!;

    [Required]
    public DateTime DogumTarixi { get; set; }

    [Required]
    public string FIN { get; set; } = null!;

    [Required]
    [Display(Name = "Cins")]
    public int CinsId { get; set; }

    [Required]
    [Display(Name = "Şöbə")]
    public int DepartamentId { get; set; }

    [Required]
    [Display(Name = "Vezife")]
    public int VezifeId { get; set; }

    [Required]
    [Display(Name = "İşə qəbul tarixi")]
    public DateTime IseQebulTarixi { get; set; } = DateTime.Today;

    [Display(Name = "Başlanğıc maaş")]
    [Range(0, double.MaxValue, ErrorMessage = "Maaş 0-dan kiçik ola bilməz")]
    public decimal? BaslangicMaas { get; set; }
    public int? BaslangicMezuniyyet { get; set; }

    public List<SelectListItem> Departments { get; set; } = new();
    public int IsciId { get; set; }
    public int UserId { get; set; }
}
