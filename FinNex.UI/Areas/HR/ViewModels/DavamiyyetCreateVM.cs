using System.ComponentModel.DataAnnotations;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class DavamiyyetCreateVM
    {
        [Required(ErrorMessage = "İşçi seçilməlidir")]
        [Display(Name = "İşçi")]
        public int IsciId { get; set; }

        [Required(ErrorMessage = "Tarix seçilməlidir")]
        [Display(Name = "Tarix")]
        public DateTime Tarix { get; set; } = DateTime.Today;

        [Display(Name = "Giriş Vaxtı")]
        public DateTime? GirisVaxti { get; set; }

        [Display(Name = "Çıxış Vaxtı")]
        public DateTime? CixisVaxti { get; set; }

        [Required(ErrorMessage = "Status seçilməlidir")]
        [Display(Name = "Status")]
        public DavamiyyetStatus Status { get; set; }

        // Dropdowns
        public List<SelectListItem> Isciler { get; set; } = new();
    }
}
