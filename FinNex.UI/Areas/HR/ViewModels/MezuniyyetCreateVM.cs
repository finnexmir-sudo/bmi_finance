using System.ComponentModel.DataAnnotations;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MezuniyyetCreateVM
    {
        [Required(ErrorMessage = "İşçi seçilməlidir")]
        [Display(Name = "İşçi")]
        public int IsciId { get; set; }

        [Required(ErrorMessage = "Məzuniyyət növü seçilməlidir")]
        [Display(Name = "Məzuniyyət Növü")]
        public MezuniyyetNovu Nov { get; set; }

        [Required(ErrorMessage = "Başlama tarixi seçilməlidir")]
        [Display(Name = "Başlama Tarixi")]
        public DateTime BaslamaTarixi { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Bitmə tarixi seçilməlidir")]
        [Display(Name = "Bitmə Tarixi")]
        public DateTime BitmeTarixi { get; set; } = DateTime.Today.AddDays(14);

        [Display(Name = "Qeyd")]
        [MaxLength(1000, ErrorMessage = "Qeyd 1000 simvoldan çox ola bilməz")]
        public string? Qeyd { get; set; }

        // Dropdowns
        public List<SelectListItem> Isciler { get; set; } = new();
    }
}
