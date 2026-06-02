using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.HR.ViewModels.MuqavileYenileme
{
    public class MuqavileYenilemeVM
    {
        public int      IsciId          { get; set; }
        public string?  IsciTamAd       { get; set; }
        public string?  DepartamentAd   { get; set; }
        public string?  VezifeAd        { get; set; }
        public DateTime? KohneBitmeTarixi { get; set; }

        [Required(ErrorMessage = "Yeni bitmə tarixi daxil edilməlidir")]
        [Display(Name = "Yeni bitmə tarixi")]
        public DateTime? YeniBitmeTarixi { get; set; }

        [Display(Name = "Qeyd")]
        [MaxLength(500)]
        public string? Qeyd { get; set; }

        public List<MuqavileYenilemeRowVM> Tarixce { get; set; } = new();
    }

    public class MuqavileYenilemeRowVM
    {
        public int       Id               { get; set; }
        public DateTime? KohneBitmeTarixi { get; set; }
        public DateTime  YeniBitmeTarixi  { get; set; }
        public DateTime  YenilemeTarixi   { get; set; }
        public string?   Qeyd             { get; set; }
    }
}
