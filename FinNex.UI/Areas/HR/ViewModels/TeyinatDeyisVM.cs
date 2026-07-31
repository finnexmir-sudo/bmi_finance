using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class TeyinatDeyisVM
    {
        public int IsciId { get; set; }
        // NULLABLE olmalıdır: display-only sahədir, formda input yoxdur — non-nullable
        // olanda .NET 8 onu avtomatik Required sayır və POST hər dəfə validasiyadan kəsilir
        // ("The IsciTamAd field is required."). TeyinatRedakteVM-dəki kimi string?.
        public string? IsciTamAd { get; set; }

        // Mövcud vəziyyət
        public string? KohneDepartament { get; set; }
        public string? KohneVezife { get; set; }

        [Required(ErrorMessage = "Departament seçilməlidir")]
        [Display(Name = "Yeni Departament")]
        public int YeniDepartamentId { get; set; }

        [Required(ErrorMessage = "Vəzifə seçilməlidir")]
        [Display(Name = "Yeni Vəzifə")]
        public int YeniVezifeId { get; set; }

        // input type=date "yyyy-MM-dd" göndərir — az-Latn-AZ mədəniyyətində DateTime
        // birbaşa bind olmur (bax: OdenisNezaretiCreateDto), string alıb invariant parse edirik.
        [Required(ErrorMessage = "Başlama tarixi mütləqdir")]
        [Display(Name = "Başlama Tarixi")]
        public string? BaslamaTarixi { get; set; }

        public List<SelectListItem> Departamentler { get; set; } = new();
        public List<SelectListItem> Vezifeler { get; set; } = new();
    }
}
