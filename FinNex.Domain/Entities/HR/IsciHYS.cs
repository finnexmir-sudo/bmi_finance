namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Isci ucun teyin olunmus Heyat Yigim Sigortasi (HYS).
    /// Hesablamada yalniz BaslamaTarixi <= hesabTarixi <= BitmeTarixi (ve ya null) olan
    /// aktiv qeydler nezere alinir.
    ///
    /// HYS mantigi:
    ///   maas = 2600, isci HYS = 1000
    ///   vergi+DSMF bazasi = 2600 - 1000 = 1600 (HYS cixilir)
    ///   ITSS/Issizlik bazasi = 2600 (TAM qalib — HYS cixilmir)
    ///   isegoturen payi = 1000 x 15% = 150
    ///   bazaya dusen gelir (IsciAyliqQazanc) = 2600 + 150 = 2750
    /// </summary>
    public class IsciHYS : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

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
