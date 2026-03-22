using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class VezifeCreateVM
    {
        [Required(ErrorMessage = "Vəzifə adı mütləq daxil edilməlidir")]
        [StringLength(100, ErrorMessage = "Vəzifə adı maksimum 100 simvol ola bilər")]
        [Display(Name = "Vəzifə Adı")]
        public string Ad { get; set; } = null!;

        [Required(ErrorMessage = "Departament mütləq seçilməlidir")]
        [Display(Name = "Departament")]
        public int DepartamentId { get; set; }

        [Display(Name = "Əsas Maaş")]
        [Range(0, 999999.99, ErrorMessage = "Maaş 0 ilə 999999.99 arasında olmalıdır")]
        public decimal? Maas { get; set; }

        [StringLength(500, ErrorMessage = "Təsvir maksimum 500 simvol ola bilər")]
        [Display(Name = "Təsvir")]
        public string? Tesvir { get; set; }

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; } = true;

        public List<SelectListItem> Departamentlar { get; set; } = new();
    }
}