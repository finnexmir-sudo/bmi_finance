using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Isci
{
    public class IsciDetailDto
    {
        public int Id { get; set; }

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

        public string SobeAdi { get; set; } = null!;
        public string VezifeAdi { get; set; } = null!;

        public DateTime IsheQebulTarixi { get; set; }
        public DateTime? IsdenAyrilmaTarixi { get; set; }

        public IsciStatus Status { get; set; }

        public bool LoginVar { get; set; }
        public string TamAd => $"{Ad} {Soyad} {AtaAdi}".Trim();
    }
}