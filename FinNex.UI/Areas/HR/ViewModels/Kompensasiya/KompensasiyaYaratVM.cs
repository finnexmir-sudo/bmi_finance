using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels.Kompensasiya
{
    public class KompensasiyaYaratVM
    {
        [Required(ErrorMessage = "İşçi seçin")]
        [Display(Name = "İşçi")]
        public int IsciId { get; set; }

        [Required(ErrorMessage = "Ayrılma tarixi mütləqdir")]
        [Display(Name = "Ayrılma tarixi")]
        [DataType(DataType.Date)]
        public DateTime AyrilmaTarixi { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Hesablanan il")]
        public int HesablananIl { get; set; } = DateTime.Now.Year;

        [Required]
        [Range(1, 12)]
        [Display(Name = "Hesablanan ay")]
        public int HesablananAy { get; set; } = DateTime.Now.Month;

        [Display(Name = "Qeyd")]
        [StringLength(500)]
        public string? Qeyd { get; set; }

        [Display(Name = "Kompensasiya günü (qismi)")]
        [Range(0, 365, ErrorMessage = "Gün sayı 0–365 aralığında olmalıdır")]
        public decimal? ManualGunSayi { get; set; }

        public List<SelectListItem> Isciler { get; set; } = new();
    }
}
