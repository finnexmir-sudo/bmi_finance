using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MaasTarixcesiRedakteVM
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string? IsciTamAd { get; set; }

        public decimal KohneMaas { get; set; }

        [Required(ErrorMessage = "Yeni maaş mütləqdir")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Maaş 0-dan böyük olmalıdır")]
        public decimal YeniMaas { get; set; }

        [Display(Name = "Əmr Nömrəsi")]
        public string? EmrNomresi { get; set; }

        public DateTime DeyismeTarixi { get; set; }

        // Ən son qeyd olduqda CariMaas da yenilənəcək
        public bool EnSonQeyddirmi { get; set; }
    }
}
