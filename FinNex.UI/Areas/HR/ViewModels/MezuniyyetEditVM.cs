using System.ComponentModel.DataAnnotations;
using FinNex.Domain.Entities.HR;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MezuniyyetEditVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Başlama tarixi seçilməlidir")]
        [Display(Name = "Başlama Tarixi")]
        public DateTime BaslamaTarixi { get; set; }

        [Required(ErrorMessage = "Bitmə tarixi seçilməlidir")]
        [Display(Name = "Bitmə Tarixi")]
        public DateTime BitmeTarixi { get; set; }

        [Display(Name = "Qeyd")]
        [MaxLength(1000, ErrorMessage = "Qeyd 1000 simvoldan çox ola bilməz")]
        public string? Qeyd { get; set; }

        [Required(ErrorMessage = "Status seçilməlidir")]
        [Display(Name = "Status")]
        public MezuniyyetStatus Status { get; set; }

        // Display-only
        public string IsciTamAd { get; set; } = null!;
        public MezuniyyetNovu Nov { get; set; }
        public string Sobe { get; set; } = null!;
    }
}
