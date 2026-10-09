using FinNex.Domain.Entities.Kassa;

namespace FinNex.Application.DTOs.Kassa
{
    /// <summary>Təsdiq ekranı üçün — gözləyən/keçmiş beyannamələrin başlığı + sətirləri.</summary>
    public class KassaKursBeyannameOzetDto
    {
        public int BeyannameId { get; set; }
        public DateTime Tarix { get; set; }
        public string IcraciAdi { get; set; } = null!;
        public KassaKursStatus Status { get; set; }
        public DateTime? TesdiqTarixi { get; set; }
        public string? TesdiqEdenAdi { get; set; }
        public string? ImtinaSebebi { get; set; }
        public List<KassaKursSetriDto> Setirler { get; set; } = new();
    }
}
