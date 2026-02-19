using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MaasCreateVM
    {
        [Required(ErrorMessage = "İşçi seçilməlidir")]
        [Display(Name = "İşçi")]
        public int IsciId { get; set; }

        [Required(ErrorMessage = "İl daxil edilməlidir")]
        [Display(Name = "İl")]
        [Range(2000, 2100, ErrorMessage = "Düzgün il daxil edin")]
        public int Il { get; set; } = DateTime.Today.Year;

        [Required(ErrorMessage = "Ay seçilməlidir")]
        [Display(Name = "Ay")]
        [Range(1, 12, ErrorMessage = "Ay 1-12 arasında olmalıdır")]
        public int Ay { get; set; } = DateTime.Today.Month;

        [Required(ErrorMessage = "Əsas maaş daxil edilməlidir")]
        [Display(Name = "Əsas Maaş")]
        [Range(0, 99999999, ErrorMessage = "Düzgün məbləğ daxil edin")]
        public decimal EsasMaas { get; set; }

        [Display(Name = "Əlavə")]
        [Range(0, 99999999, ErrorMessage = "Düzgün məbləğ daxil edin")]
        public decimal Elave { get; set; }

        [Display(Name = "Cərimə")]
        [Range(0, 99999999, ErrorMessage = "Düzgün məbləğ daxil edin")]
        public decimal Cerime { get; set; }

        // Dropdowns
        public List<SelectListItem> Isciler { get; set; } = new();
    }
}
