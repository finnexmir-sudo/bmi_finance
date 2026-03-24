using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class IsciMaliyeCreateVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "İşçi tələb olunur")]
        public int IsciId { get; set; }

        public string? IsciTamAd { get; set; }

        [Required(ErrorMessage = "Cari maaş tələb olunur")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Maaş müsbət olmalıdır")]
        public decimal CariMaas { get; set; }

        [StringLength(50)]
        public string? BankHesabNo { get; set; }

        [StringLength(50)]
        public string? SosialSigortaNo { get; set; }

        public List<SelectListItem> Isciler { get; set; } = new();
    }
}
