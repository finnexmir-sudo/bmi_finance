namespace FinNex.UI.Areas.HR.ViewModels.Jeton
{
    /// <summary>
    /// İşçilərin jeton saat sıralaması üçün bir sətir.
    /// Cari balans (xərclənə bilən aktiv müsbət saat) əsas göstəricidir.
    /// </summary>
    public class IsciJetonSaatVM
    {
        public int IsciId { get; set; }
        public string AdSoyad { get; set; } = null!;
        public string Departament { get; set; } = "—";
        public string? Vezife { get; set; }

        // Aktiv müsbət jetonların cari qalan saatı (xərclənə bilən balans)
        public decimal CariBalansSaat { get; set; }
        // İndiyədək qazanılmış bütün müsbət jeton saatı (ləğv olunanlar xaric)
        public decimal ToplamQazanilmisSaat { get; set; }
        // İstifadə olunmuş = qazanılmış − cari qalan
        public decimal IstifadeOlunmusSaat => ToplamQazanilmisSaat - CariBalansSaat;

        // Aktiv müsbət jeton sayı
        public int AktivMusbetSayi { get; set; }
        // Aktiv qara (cəza) jeton sayı
        public int AktivQaraSayi { get; set; }
    }
}
