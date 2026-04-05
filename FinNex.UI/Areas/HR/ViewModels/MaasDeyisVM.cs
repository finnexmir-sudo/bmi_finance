using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MaasDeyisVM
    {
        public int IsciId { get; set; }
        public string? IsciTamAd { get; set; }
        public decimal KohneMaas { get; set; }

        [Required(ErrorMessage = "Yeni maaş mütləqdir")]
        [Display(Name = "Yeni Maaş")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Maaş 0-dan böyük olmalıdır")]
        public decimal YeniMaas { get; set; }

        [Display(Name = "Əmr Nömrəsi")]
        public string? EmrNomresi { get; set; }
    }
}
