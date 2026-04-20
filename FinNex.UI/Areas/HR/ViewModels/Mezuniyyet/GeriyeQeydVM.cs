using System.ComponentModel.DataAnnotations;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels.Mezuniyyet
{
    public class GeriyeQeydVM
    {
        [Required(ErrorMessage = "İşçi seçilməlidir.")]
        [Display(Name = "İşçi")]
        public int IsciId { get; set; }

        [Required(ErrorMessage = "Məzuniyyət növü seçilməlidir.")]
        [Display(Name = "Məzuniyyət növü")]
        public MezuniyyetNovu Nov { get; set; } = MezuniyyetNovu.Illik;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Başlama tarixi")]
        public DateTime BaslamaTarixi { get; set; } = DateTime.Today.AddDays(-1);

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Bitmə tarixi")]
        public DateTime BitmeTarixi { get; set; } = DateTime.Today.AddDays(-1);

        [Required(ErrorMessage = "Səbəb qeyd edilməlidir.")]
        [MinLength(5, ErrorMessage = "Səbəb ən azı 5 simvoldan ibarət olmalıdır.")]
        [MaxLength(500)]
        [Display(Name = "Səbəb")]
        public string Sebeb { get; set; } = "";

        public List<SelectListItem> Isciler { get; set; } = new();

        public int MaksimumKohneGun { get; set; } = 90;
    }
}
