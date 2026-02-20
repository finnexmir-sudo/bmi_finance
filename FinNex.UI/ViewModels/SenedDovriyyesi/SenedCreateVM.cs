using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.ViewModels.SenedDovriyyesi;

public class SenedCreateVM
{
    [Required(ErrorMessage = "Şöbə seçilməlidir")]
    public int SobeId { get; set; }

    [Required(ErrorMessage = "Sənəd növü seçilməlidir")]
    public int SenedNovuId { get; set; }

    [Required(ErrorMessage = "Başlıq daxil edilməlidir")]
    [StringLength(300, ErrorMessage = "Başlıq maksimum 300 simvol ola bilər")]
    public string Basliq { get; set; } = null!;

    [Required(ErrorMessage = "Fayl yüklənməlidir")]
    public IFormFile Fayl { get; set; } = null!;


    [Required(ErrorMessage = "Açar söz daxil edilməlidir")]
    [StringLength(500, ErrorMessage = "Açar söz maksimum 500 simvol ola bilər")]
    public string AcarSoz { get; set; } = null!;

    //public List<int> TagIds { get; set; } = new();

    // Dropdowns
    public List<DropdownItemVM> Sobeler { get; set; } = new();
    public List<DropdownItemVM> SenedNovleri { get; set; } = new();
    //  public List<DropdownItemVM> Tagler { get; set; } = new();
}
