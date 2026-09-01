namespace FinNex.Domain.Entities.HR
{
    public class EzamiyyetMuraciet : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public string Baslig { get; set; } = null!;

        public int MekanId { get; set; }
        public EzamiyyetMekan Mekan { get; set; } = null!;

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }

        // Gündaxili saat aralığı (istəyə bağlı — tam gün üçün null)
        public TimeSpan? BaslamaSaati { get; set; }
        public TimeSpan? BitisSaati { get; set; }

        // ── Avtopark bağlantısı (01.09.2026) ─────────────────────────────
        /// <summary>
        /// İşçinin ezamiyyət üçün istədiyi maşın. `null` = maşın lazım deyil.
        ///
        /// NİYƏ BURADA SAXLANILIR: maşın müraciəti (`MasinMuraciet`) ezamiyyət
        /// TƏSDİQLƏNƏNDƏ yaradılır — təsdiqə qədər seçim bir yerdə durmalıdır.
        /// Təsdiqdən sonra əsl bağ əks istiqamətdədir:
        /// <c>MasinMuraciet.EzamiyyetMuracietId</c>.
        ///
        /// ⚠️ Bu sahə «istək»dir, «verilmiş maşın» DEYİL — rəhbər imtina etsə,
        /// yaxud müraciət ləğv olunsa burada dəyər qalır, amma maşın müraciəti
        /// yaranmır/ləğv olunur. «Hansı maşın verildi» sualının cavabı həmişə
        /// `MasinMuraciet`-dədir.
        /// </summary>
        public int? MasinId { get; set; }
        public Avtopark.Masin? Masin { get; set; }

        public string? SenedYolu { get; set; }
        public string? SenedAd  { get; set; }

        public string? Qeyd { get; set; }

        public EzamiyyetStatus Status { get; set; } = EzamiyyetStatus.Gozleyir;

        // Rəhbər təsdiq sahəsi
        public bool?     RehberTesdiq        { get; set; }
        public int?      RehberId            { get; set; }
        public Isci?     Rehber              { get; set; }
        public DateTime? RehberTesdiqTarixi  { get; set; }
        public string?   RehberQeydi         { get; set; }

        // ADMS cihaz izləmə — ezamiyyət çıxış/qaydış
        public DateTime? CihazCixisVaxti   { get; set; }
        public DateTime? CihazQayidisVaxti { get; set; }

        // Gün bağlananda cihaz oxuması olmayan SAATLI ezamiyyətin vaxtları
        // müraciətdəki PLAN üzrə avtomatik yazılıbsa true (gecə servisi).
        public bool CihazVaxtPlanUzre { get; set; } = false;

        // Geri dönüş notu — işçi ezamiyyətdən qayıtdıqdan sonra əlavə edir
        public string? GeriDonusQeydi { get; set; }
    }
}
