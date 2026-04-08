using System.ComponentModel.DataAnnotations;

namespace FinNex.Application.DTOs.SenedDovriyyesi.SenedSablon
{
    public class SenedSablonListDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public string? Tesvir { get; set; }

        public int SenedNovuId { get; set; }
        public string SenedNovuAd { get; set; } = null!;

        public string FaylAdi { get; set; } = null!;

        public bool Aktiv { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }

    public class SenedSablonCreateDto
    {
        [Required(ErrorMessage = "Şablon adı daxil edilməlidir")]
        [MaxLength(300)]
        public string Ad { get; set; } = null!;

        [MaxLength(1000)]
        public string? Tesvir { get; set; }

        [Required(ErrorMessage = "Sənəd növü seçilməlidir")]
        public int SenedNovuId { get; set; }
    }
}
