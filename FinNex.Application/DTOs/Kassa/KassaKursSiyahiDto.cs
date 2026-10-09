using FinNex.Domain.Entities.Kassa;

namespace FinNex.Application.DTOs.Kassa
{
    /// <summary>Son beyannamələrin cədvəli — hər sətir bir (gün × valyuta) cütüdür.</summary>
    public class KassaKursSiyahiDto
    {
        public int BeyannameId { get; set; }
        public DateTime Tarix { get; set; }
        public string Valyuta { get; set; } = null!;
        public decimal? NagdAlis { get; set; }
        public decimal? NagdSatis { get; set; }
        public decimal? QeyriNagdAlis { get; set; }
        public decimal? QeyriNagdSatis { get; set; }
        public string IcraciAdi { get; set; } = null!;
        public KassaKursStatus Status { get; set; }
        public DateTime? TesdiqTarixi { get; set; }
        public string? TesdiqEdenAdi { get; set; }
    }
}
