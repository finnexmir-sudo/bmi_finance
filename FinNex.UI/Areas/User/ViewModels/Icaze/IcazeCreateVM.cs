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

       
        public int? EvezEdenIsciId { get; set; }

        [Required(ErrorMessage = "İcazə tarixi seçilməlidir")]
        public DateTime IcazeTarixi { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Başlama saatı seçilməlidir")]
        public string BaslamaSaati { get; set; } = "09:00";

        [Required(ErrorMessage = "Bitmə saatı seçilməlidir")]
        public string BitisSaati { get; set; } = "11:00";

        [Required(ErrorMessage = "Səbəb mütləq qeyd edilməlidir")]
        [MaxLength(500, ErrorMessage = "Səbəb 500 simvoldan çox ola bilməz")]
        public string? Sebeb { get; set; }

        // Nahara çıxmıram — nahar fasiləsini icazəyə qatıram (max 3 saat 45 dəqiqə)
        [Display(Name = "Nahar fasiləsini icazəyə qat")]
        public bool NaharNezereAlinmasin { get; set; } = false;

        [Range(0, 24, ErrorMessage = "Jeton saatı 0–24 aralığında olmalıdır")]
        public decimal JetonOdenenSaat { get; set; } = 0;

        // Dropdown
        public List<SelectListItem> EvezEdenList { get; set; } = new();
    }
}
