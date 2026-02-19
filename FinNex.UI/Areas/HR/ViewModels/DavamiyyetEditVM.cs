using System.ComponentModel.DataAnnotations;
using FinNex.Domain.Entities.HR;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class DavamiyyetEditVM
    {
        public int Id { get; set; }

        [Display(Name = "Giriş Vaxtı")]
        public DateTime? GirisVaxti { get; set; }

        [Display(Name = "Çıxış Vaxtı")]
        public DateTime? CixisVaxti { get; set; }

        [Required(ErrorMessage = "Status seçilməlidir")]
        [Display(Name = "Status")]
        public DavamiyyetStatus Status { get; set; }

        // Display-only (read from DB)
        public string IsciTamAd { get; set; } = null!;
        public DateTime Tarix { get; set; }
        public string Sobe { get; set; } = null!;
        public string Vezife { get; set; } = null!;
    }
}
