using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class TeyinatDeyisVM
    {
        public int IsciId { get; set; }
        public string IsciTamAd { get; set; } = null!;

        // Mövcud vəziyyət
        public string? KohneDepartament { get; set; }
        public string? KohneVezife { get; set; }

        [Required(ErrorMessage = "Departament seçilməlidir")]
        [Display(Name = "Yeni Departament")]
        public int YeniDepartamentId { get; set; }

        [Required(ErrorMessage = "Vəzifə seçilməlidir")]
        [Display(Name = "Yeni Vəzifə")]
        public int YeniVezifeId { get; set; }

        [Required(ErrorMessage = "Başlama tarixi mütləqdir")]
        [Display(Name = "Başlama Tarixi")]
        public DateTime BaslamaTarixi { get; set; } = DateTime.Today;

        public List<SelectListItem> Departamentler { get; set; } = new();
        public List<SelectListItem> Vezifeler { get; set; } = new();
    }
}
