using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Avtopark
{
    /// <summary>
    /// Texniki baxış / sığorta / yağ dəyişmə xəbərdarlığı KİMƏ getsin.
    ///
    /// 19.08.2026 QƏRARI: alıcılar **rola görə hesablanmır**, admin işçiləri
    /// bir-bir seçir. Səbəb: bu iş adətən konkret bir-iki adamın üzərindədir
    /// (təsərrüfat müdiri, sürücü) və rol siyahısı ilə üst-üstə düşmür.
    ///
    /// ⚠️ SİYAHI BOŞ OLARSA XƏBƏRDARLIQ HEÇ KİMƏ GETMİR — səssiz qalmasın deyə
    /// Admin → «Müddət xəbərdarlığı alıcıları» ekranında boş siyahı üçün açıq
    /// xəbərdarlıq göstərilir. Rola fallback qəsdən yoxdur: gizli alıcı
    /// «kim xəbər aldı?» sualını cavabsız qoyar.
    /// </summary>
    public class AvtoparkXeberdarliqAlicisi : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        /// <summary>Müvəqqəti söndürmək üçün — sətri silmədən.</summary>
        public bool Aktivdir { get; set; } = true;
    }
}
