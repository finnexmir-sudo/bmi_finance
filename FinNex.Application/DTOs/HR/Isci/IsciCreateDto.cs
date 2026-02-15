using FinNex.Domain.Entities.HR;
using System.ComponentModel.DataAnnotations;

namespace FinNex.Application.DTOs.HR.Isci
{
    public class IsciCreateDto
    {
        [Required]
        public string Ad { get; set; } = null!;
        [Required]
        public string Soyad { get; set; } = null!;
        public string AtaAdi { get; set; } = null!;
        [Required]
        public string FIN { get; set; } = null!;
        public string SeriyaNomre { get; set; } = null!;

        public DateTime DogumTarixi { get; set; }
        public Cins Cins { get; set; }

        public string? Telefon { get; set; }
        [Required]
        public string? Email { get; set; }
        public string? Unvan { get; set; }

        public int DepartamentId { get; set; }
        public int VezifeId { get; set; }

        public DateTime IsheBaslamaTarixi { get; set; }

        public int? AppUserId { get; set; }
    }

}
