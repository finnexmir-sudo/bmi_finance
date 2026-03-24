using System.ComponentModel.DataAnnotations;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class IsciEditVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad mütləqdir")]
        [Display(Name = "Ad")]
        public string Ad { get; set; } = null!;

        [Required(ErrorMessage = "Soyad mütləqdir")]
        [Display(Name = "Soyad")]
        public string Soyad { get; set; } = null!;

        [Display(Name = "Ata adı")]
        public string? AtaAdi { get; set; }

        [Required(ErrorMessage = "FIN mütləqdir")]
        [Display(Name = "FIN")]
        public string FIN { get; set; } = null!;

        [Required(ErrorMessage = "Şəxsiyyət vəsiqəsi mütləqdir")]
        [Display(Name = "Şəxsiyyət Vəsiqəsi")]
        public string SeriyaNomre { get; set; } = null!;

        [Required(ErrorMessage = "Doğum tarixi mütləqdir")]
        [Display(Name = "Doğum Tarixi")]
        public DateTime DogumTarixi { get; set; }

        [Required(ErrorMessage = "Cins mütləqdir")]
        [Display(Name = "Cins")]
        public int CinsId { get; set; }

        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }

        [EmailAddress(ErrorMessage = "Düzgün email formatı daxil edin")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Ünvan")]
        public string? Unvan { get; set; }

        [Required(ErrorMessage = "İşə qəbul tarixi mütləqdir")]
        [Display(Name = "İşə Qəbul Tarixi")]
        public DateTime IsheQebulTarixi { get; set; }

        [Display(Name = "İşdən ayrılma tarixi")]
        public DateTime? IsdenAyrilmaTarixi { get; set; }

        [Display(Name = "Status")]
        public IsciStatus Status { get; set; }
    }
}
