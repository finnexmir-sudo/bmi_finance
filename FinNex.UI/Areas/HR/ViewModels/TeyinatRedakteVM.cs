using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class TeyinatRedakteVM
    {
        public int IsciId { get; set; }
        public string IsciTamAd { get; set; } = null!;

        [Required(ErrorMessage = "Departament seçilməlidir")]
        [Display(Name = "Departament")]
        public int DepartamentId { get; set; }

        [Required(ErrorMessage = "Vəzifə seçilməlidir")]
        [Display(Name = "Vəzifə")]
        public int VezifeId { get; set; }

        public List<SelectListItem> Departamentler { get; set; } = new();
        public List<SelectListItem> Vezifeler { get; set; } = new();
    }
}
