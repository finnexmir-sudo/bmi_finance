namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Isci ucun teyin olunmus Heyat Yigim Sigortasi (HYS).
    /// Hesablamada BaslamaTarixi &lt;= hesabTarixi &lt;= BitmeTarixi (ve ya null) olan
    /// AKTIV qeydler nezere alinir. Bir isci bir nece sirkette HYS ola biler —
    /// butun aktiv qeydlerin cemi maas bazasindan cixilir.
    ///
    /// HYS mantigi (cem):
    ///   maas = 2600
    ///   isci HYS A sirketi = 800, B sirketi = 200 → cem = 1000
    ///   vergi+DSMF bazasi = 2600 - 1000 = 1600 (cem HYS cixilir)
    ///   ITSS/Issizlik bazasi = 2600 + isegoturen_HYS (TAM qalib + isegoturen)
    ///   isegoturen payi = 1000 x 15% = 150
    /// </summary>
    public class IsciHYS : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        /// <summary>
        /// HYS-ı təklif edən sığorta şirkəti (isteğe görə). Bir işçi bir neçə
        /// şirkətdə HYS açdıqda, hər qeyd ayrıca uçotlanır.
        /// </summary>
        public string? Sirket { get; set; }

        /// <summary>
        /// Iscinin ayliq HYS meblegi (iscinin oz odediy hissesi).
        /// Brut-den cixilib vergi+DSMF bazasini azaldir.
        /// </summary>
        public decimal Mebleg { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; } // null = muddetsiz
        public string? Qeyd { get; set; }
    }
}
