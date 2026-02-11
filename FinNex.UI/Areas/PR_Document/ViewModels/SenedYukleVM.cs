using System.ComponentModel.DataAnnotations;
using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.UI.Areas.PR_Document.ViewModels
{
    public class SenedYukleVM
    {
        [Required(ErrorMessage = "Şöbə seçilməlidir")]
        public int SobeId { get; set; }

        [Required(ErrorMessage = "Sənəd növü seçilməlidir")]
        public int SenedNovuId { get; set; }

        [Required(ErrorMessage = "Başlıq daxil edilməlidir")]
        [MaxLength(250, ErrorMessage = "Başlıq 250 simvoldan artıq ola bilməz")]
        public string Basliq { get; set; } = null!;

        [Required(ErrorMessage = "Açar söz daxil edilməlidir")]
        [MaxLength(200, ErrorMessage = "Açar söz 200 simvoldan artıq ola bilməz")]
        public string AcarSoz { get; set; } = null!;

        [Required(ErrorMessage = "Fayl seçilməlidir")]
        public IFormFile Fayl { get; set; } = null!;

        public List<Sobe> Sobeler { get; set; } = new();
        public List<SenedNovu> SenedNovleri { get; set; } = new();
    }
}
