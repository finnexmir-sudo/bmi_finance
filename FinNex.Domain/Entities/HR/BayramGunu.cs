namespace FinNex.Domain.Entities.HR
{
    public class BayramGunu : BaseEntity
    {
        public string? Ad { get; set; } // Məsələn: Qurban Bayramı
        public DateTime Tarix { get; set; }
        public bool HerIlTeyinOlunur { get; set; } // Sabit tarixlər üçün (20 Yanvar və s.)

        /// <summary>
        /// Qeydin növü:
        ///   Bayram = istirahət günü (default)
        ///   IsGunu = iş günü override (şənbə/bazarı iş etmək)
        /// </summary>
        public GunTipi Tip { get; set; } = GunTipi.Bayram;

        /// <summary>
        /// Məzuniyyət günlərinə daxildirmi? Default: false.
        /// HR per-day konfiqurasiya edir:
        ///   false — bu gün məzuniyyət günü kimi sayılmır (skip) — adi hal
        ///   true  — bu gün məzuniyyət günü kimi sayılır (məs. Qurban bayramının
        ///           ikinci günü 1 gün hesablansa, 1-ci gün false, 2-ci gün true)
        /// </summary>
        public bool MezuniyyetdeHesablanir { get; set; } = false;

        // Xüsusi başlayış vaxtı: bu gün işə gec başlanır (məs. 10:00) → gecikməni şərtləndirmir
        public TimeSpan? XususiBaslayisVaxti { get; set; }

        // Xüsusi bitmə vaxtı: qısa gün (məs. 16:00) → erkən çıxmanı şərtləndirmir
        public TimeSpan? XususiBitisVaxti { get; set; }
    }
}
