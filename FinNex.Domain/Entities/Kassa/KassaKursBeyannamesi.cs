using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Kassa
{
    /// <summary>
    /// Kassanın bir günlük valyuta alış-satış məzənnə dəsti (BMI-nin
    /// "frmExchange" modulunun köçürülməsi, 09.10.2026). Hər gün üçün BİR
    /// beyannamə, ona bağlı 5 valyutalıq <see cref="KassaKursSetri"/> sətirləri.
    ///
    /// Status/təsdiq konkret bu beyannamə sətrinə aiddir — BMI-dəki kimi
    /// "bütün gözləyənlər" üzərində qlobal UPDATE YOXDUR (bax BMI-nin
    /// frmExchangeTesdiq.Approval metodundakı məlum bugun qarşısı burada alınıb).
    /// </summary>
    public class KassaKursBeyannamesi : BaseEntity
    {
        public DateTime Tarix { get; set; }

        public int IcraciIsciId { get; set; }
        public Isci Icraci { get; set; } = null!;

        public KassaKursStatus Status { get; set; } = KassaKursStatus.Gozleyir;

        public int? TesdiqEdenIsciId { get; set; }
        public Isci? TesdiqEden { get; set; }
        public DateTime? TesdiqTarixi { get; set; }
        public string? ImtinaSebebi { get; set; }

        public ICollection<KassaKursSetri> Setirler { get; set; } = new List<KassaKursSetri>();
    }
}
