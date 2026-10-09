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

        /// <summary>Kassir forma göndərə bilər — heç beyannamə yoxdursa, imtina
        /// olunubsa, YA DA artıq təsdiqlənib (gün ərzində kurs dəyişə bilər,
        /// bu halda YENİ beyannamə yaranır — köhnə təsdiqlənmiş sətir
        /// tarixçədə qalır). Yalnız "Gözləyir" (hələ qərar verilməyib)
        /// bloklayır — eyni anda iki təklif göndərilməsin (09.10.2026,
        /// istifadəçi tələbi: "gun icinde kursu yeniden deyismek imkani yoxdur").</summary>
        public bool YazaBiler => Status == null || Status != KassaKursStatus.Gozleyir;
    }
}
