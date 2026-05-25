using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.SenedDovriyyesi.ViewModels
{
    public class SenedUpdateVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Şöbə seçilməlidir")]
        [Display(Name = "Şöbə")]
        public int SobeId { get; set; }

        [Required(ErrorMessage = "Sənəd növü seçilməlidir")]
        [Display(Name = "Sənəd Növü")]
        public int SenedNovuId { get; set; }

        [Required(ErrorMessage = "Başlıq daxil edilməlidir")]
        [MaxLength(250, ErrorMessage = "Başlıq 250 simvoldan çox ola bilməz")]
        [Display(Name = "Başlıq")]
        public string Basliq { get; set; } = null!;

        [MaxLength(500, ErrorMessage = "Açar söz 500 simvoldan çox ola bilməz")]
        [Display(Name = "Açar Söz")]
        public string AcarSoz { get; set; } = string.Empty;

        public List<int> TagIds { get; set; } = new();

        /// <summary>Görünmə kateqoriyası — Umumi və ya Xususi.</summary>
        [Display(Name = "Kateqoriya")]
        public FinNex.Domain.Entities.SenedDovriyyesi.SenedKateqoriyasi Kateqoriya { get; set; } = FinNex.Domain.Entities.SenedDovriyyesi.SenedKateqoriyasi.Umumi;
    }
}
