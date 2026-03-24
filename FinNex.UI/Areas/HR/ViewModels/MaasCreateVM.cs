using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MaasCreateVM
    {
        [Required(ErrorMessage = "İşçi tələb olunur")]
        public int IsciId { get; set; }

        [Required(ErrorMessage = "İl tələb olunur")]
        [Range(2000, 2100, ErrorMessage = "Düzgün il daxil edin")]
        public int Il { get; set; } = DateTime.Today.Year;

        [Required(ErrorMessage = "Ay tələb olunur")]
        [Range(1, 12, ErrorMessage = "Ay 1-12 arasında olmalıdır")]
        public int Ay { get; set; } = DateTime.Today.Month;

        public List<SelectListItem> Isciler { get; set; } = new();
    }
}
