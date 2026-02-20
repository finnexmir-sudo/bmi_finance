using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Isci
{
    public class IsciDetailDto
    {
        public int Id { get; set; }

        public string Ad { get; set; } = null!;
        public string Soyad { get; set; } = null!;
        public string AtaAdi { get; set; } = null!;

        public string FIN { get; set; } = null!;
        public string SeriyaNomre { get; set; } = null!;

        public DateTime DogumTarixi { get; set; }
        public Cins Cins { get; set; }

        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public string? Unvan { get; set; }

        public string Sobe { get; set; } = null!;
        public string Vezife { get; set; } = null!;

        public DateTime IsheBaslamaTarixi { get; set; }
        public DateTime? IshtenCixmaTarixi { get; set; }

        public IsciStatus Status { get; set; }

        public bool LoginVar { get; set; }
    }

}
