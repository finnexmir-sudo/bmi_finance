using FinNex.Domain.Entities.Kassa;

namespace FinNex.Application.DTOs.Kassa
{
    /// <summary>Bir günün tam beyannaməsi (oxuma) — forma + status üçün.</summary>
    public class KassaKursGunlukDto
    {
        public int? BeyannameId { get; set; }
        public DateTime Tarix { get; set; }
        public KassaKursStatus? Status { get; set; }
        public string? IcraciAdi { get; set; }
        public string? TesdiqEdenAdi { get; set; }
        public DateTime? TesdiqTarixi { get; set; }
        public string? ImtinaSebebi { get; set; }
        public List<KassaKursSetriDto> Setirler { get; set; } = new();

        /// <summary>Bu gün üçün ya heç beyannamə yoxdur, ya da imtina olunub —
        /// kassir forma göndərə bilər. "Gözləyir"/"Təsdiqləndi" olanda bloklanır.</summary>
        public bool YazaBiler => Status == null || Status == KassaKursStatus.Imtina;
    }
}
