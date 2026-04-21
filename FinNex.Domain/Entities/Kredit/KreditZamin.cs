namespace FinNex.Domain.Entities.Kredit
{
    /// <summary>
    /// Kredit müraciətinə əlavə edilən zamin. Bir müraciətə bir neçə zamin olar.
    /// Tarixçə yoxlanması üçün FIN ilə axtarış aparılır — eyni FIN-li zamin
    /// başqa müraciətlərdə də çıxırsa, sistem əvvəlki baxışlardan xəbərdar edir.
    /// </summary>
    public class KreditZamin : BaseEntity
    {
        public int KreditMuracietId { get; set; }
        public KreditMuraciet KreditMuraciet { get; set; } = null!;

        public string AdSoyadAtaAdi { get; set; } = null!;
        public string? FIN { get; set; }
        public string? Telefon { get; set; }
        public string? IsYeri { get; set; }
        public decimal? EmekHaqqi { get; set; }
        public string? Unvan { get; set; }
        public string? Qeyd { get; set; }
    }
}
