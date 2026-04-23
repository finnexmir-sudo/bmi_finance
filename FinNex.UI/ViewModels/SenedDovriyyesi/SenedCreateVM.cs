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

    // Köhnə tək fayl sahəsi (geri uyğunluq üçün saxlanılır).
    public IFormFile? Fayl { get; set; }

    // Bir neçə fayl eyni anda yüklənə bilər.
    // Məcburi yoxlama controller-də əl ilə aparılır (Fayl + Fayllar birlikdə).
    public List<IFormFile> Fayllar { get; set; } = new();

    // Sənədin tarixi — default bu gün, istifadəçi dəyişə bilər.
    [DataType(DataType.Date)]
    public DateTime SenedTarixi { get; set; } = DateTime.Now.Date;

    [Required(ErrorMessage = "Açar söz daxil edilməlidir")]
    [StringLength(500, ErrorMessage = "Açar söz maksimum 500 simvol ola bilər")]
    public string AcarSoz { get; set; } = null!;

    public List<int> TagIds { get; set; } = new();

    // Dropdowns
    public List<DropdownItemVM> Sobeler { get; set; } = new();
    public List<DropdownItemVM> SenedNovleri { get; set; } = new();
    public List<DropdownItemVM> Tagler { get; set; } = new();
}
