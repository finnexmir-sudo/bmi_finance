using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Isci
{
    public class IsciCreateDto
    {
        public int? UserId { get; set; }

        public string Ad { get; set; } = null!;
        public string Soyad { get; set; } = null!;
        public string? AtaAdi { get; set; }

        public string FIN { get; set; } = null!;
        public string SeriyaNomre { get; set; } = null!;

        public DateTime DogumTarixi { get; set; }
        public Cins Cins { get; set; }

        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public string? Unvan { get; set; }

        public DateTime IsheQebulTarixi { get; set; }

        // İlkin təyinat - IsciTeyinat yaratmaq üçün
        public int DepartamentId { get; set; }
        public int VezifeId { get; set; }

        // İlkin maaş - IsciMaliye yaratmaq üçün
        public decimal? BaslangicMaas { get; set; }
        public int? BaslangicMezuniyyet { get; set; }
    }
}
