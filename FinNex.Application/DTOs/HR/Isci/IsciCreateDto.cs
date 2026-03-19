using FinNex.Domain.Entities.HR;
using System.ComponentModel.DataAnnotations;

namespace FinNex.Application.DTOs.HR.Isci
{

    // Create üçün (Formdan gələnlər)
    public class IsciCreateDto
    {
        public int UserId { get; set; }
        public string Ad { get; set; } = null!;
        public string Soyad { get; set; } = null!;
        public string AtaAdi { get; set; } = null!;
        public string FIN { get; set; } = null!;
        public string SeriyaNomre { get; set; } = null!;
        public DateTime DogumTarixi { get; set; }
        public Cins Cins { get; set; }
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public string Unvan { get; set; } = null!;
        public int DepartamentId { get; set; }
        public int VezifeId { get; set; }
        public DateTime IsheBaslamaTarixi { get; set; }

    }

}
