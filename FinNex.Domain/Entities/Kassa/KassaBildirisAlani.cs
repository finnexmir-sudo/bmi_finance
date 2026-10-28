using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Kassa
{
    /// <summary>
    /// Kassa günlük kurs beyannaməsi TƏSDİQLƏNDİKDƏ bildiriş alacaq işçilərin
    /// sabit siyahısı. Admin tərəfindən bir dəfə qurulur (KassaTesdiqEdici ilə
    /// EYNİ naxış) — kassir hər dəfə kimə göndəriləcəyini seçmir, siyahı sabitdir
    /// (istifadəçi qərarı, 28.10.2026).
    /// </summary>
    public class KassaBildirisAlani : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public DateTime AktivdirFrom { get; set; }
        public DateTime? AktivdirTo { get; set; }
        public string? Qeyd { get; set; }
    }
}
