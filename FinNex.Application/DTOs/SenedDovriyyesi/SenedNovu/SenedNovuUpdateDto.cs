using System.ComponentModel.DataAnnotations;

namespace FinNex.Application.DTOs.SenedDovriyyesi.SenedNovu
{
    public class SenedNovuUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Kod { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Ad { get; set; } = null!;

        public bool Aktiv { get; set; }
    }
}
