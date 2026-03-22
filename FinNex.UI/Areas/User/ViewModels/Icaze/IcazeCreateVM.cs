using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.User.ViewModels.Icaze
{
    /// <summary>
    /// Yeni saatlıq icazə müraciəti üçün ViewModel.
    /// Domain entity-lərinə istinad yoxdur.
    /// </summary>
    public class IcazeCreateVM
    {
        // Sistem avtomatik doldurur
        public int IsciId { get; set; }

        [Required(ErrorMessage = "Əvəzedici işçi seçilməlidir")]
        public int EvezEdenIsciId { get; set; }

        [Required(ErrorMessage = "İcazə tarixi seçilməlidir")]
        public DateTime IcazeTarixi { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Başlama saatı seçilməlidir")]
        public TimeSpan BaslamaSaati { get; set; } = new TimeSpan(9, 0, 0);

        [Required(ErrorMessage = "Bitmə saatı seçilməlidir")]
        public TimeSpan BitisSaati { get; set; } = new TimeSpan(11, 0, 0);

        [MaxLength(500, ErrorMessage = "Səbəb 500 simvoldan çox ola bilməz")]
        public string? Sebeb { get; set; }

        // Dropdown
        public List<SelectListItem> EvezEdenList { get; set; } = new();
    }
}
