using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MaasEditVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "İl daxil edilməlidir")]
        [Display(Name = "İl")]
        [Range(2000, 2100, ErrorMessage = "Düzgün il daxil edin")]
        public int Il { get; set; }

        [Required(ErrorMessage = "Ay seçilməlidir")]
        [Display(Name = "Ay")]
        [Range(1, 12, ErrorMessage = "Ay 1-12 arasında olmalıdır")]
        public int Ay { get; set; }

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

        // Display-only
        public string IsciTamAd { get; set; } = null!;
        public string Sobe { get; set; } = null!;
        public string Vezife { get; set; } = null!;
    }
}
