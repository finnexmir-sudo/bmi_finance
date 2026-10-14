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

        // ── İllik "36 Saat Hüququ" (icazə sisteminin illik balansı) — MÜKAFAT
        // jetonlarından (yuxarıdakı sahələr) TAMAMİLƏ AYRI bir kəmiyyətdir.
        // Rəhbər tələbi (14.10.2026): "illik saatlardan kimde ne qalib, ne
        // istifade olunub". Mənbə — DashboardService-in öz kartı ilə EYNİ:
        // hüquq = JetonService.IllikHuquqQalanSaatAsync (Qara Jeton kəsintisi
        // nəzərə alınmış), istifadə = IcazeService.GetIsciIzlemeAsync.TesdiqSaat
        // (cari il, JetonOdenenSaat onsuz da çıxılmış) — ikisi də CLAUDE.md-də
        // "altı yerdən biri" kimi sənədləşmiş kanonik mənbələrdir, yeni düstur
        // YAZILMADI.
        public decimal IllikHuquqSaat { get; set; }
        public decimal IllikIstifadeSaat { get; set; }
        public decimal IllikQaliqSaat => IllikHuquqSaat - IllikIstifadeSaat;
    }
}
