using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Avtopark
{
    /// <summary>
    /// Maşın müraciəti VƏ açar (çıxış/qayıdış) jurnalı — BİR CƏDVƏLDƏ.
    ///
    /// Ayrı cədvəl saxlansaydı «hansı çıxış hansı müraciətə aiddir» bağlantısı
    /// əl ilə qurulmalı olardı və uyğunsuzluq riski yaranardı. Müraciət və
    /// faktiki çıxış eyni hadisənin iki mərhələsidir.
    ///
    /// PLAN vs FAKT — qəsdən ayrıdır:
    ///   <see cref="PlanBaslama"/> — işçinin İSTƏDİYİ çıxış vaxtı (tarix + saat)
    ///   <see cref="CixisTarixi"/>/<see cref="QayidisTarixi"/> — açarın FAKTİKİ
    ///   əldən-ələ keçdiyi an (kassa düyməni basanda `DateTime.Now` yazılır).
    ///
    /// ⚠️ PLANLAŞDIRILAN BİTMƏ YOXDUR (21.08.2026 qərarı). İstifadəçi sözü:
    /// «bitmə bilinmir axı — nə vaxt qayıtdı, kassada qayıtdı yazılacaq».
    /// Doğrudur: qayıdışın yeganə mənbəyi <see cref="QayidisTarixi"/>-dir.
    ///
    /// Maşının ikiqat götürülməsinin qarşısını PLAN yox, VƏZİYYƏT alır —
    /// `MasinMuracietService.CixdiAsync` (Qoruyucu 4.1): bir maşının eyni anda
    /// iki açıq çıxışı ola bilməz. Yəni açar verilən an maşın tutulur, «Gəldi»
    /// qeyd edilənə qədər ikinci açar verilmir.
    /// </summary>
    public class MasinMuraciet : BaseEntity
    {
        public int MasinId { get; set; }
        public Masin Masin { get; set; } = null!;

        /// <summary>Müraciəti edən (maşını götürən) işçi.</summary>
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public DateTime PlanBaslama { get; set; }

        /// <summary>
        /// KÖHNƏ sahə — 21.08.2026-dan YAZILMIR (həmişə `null`).
        ///
        /// Sütun QƏSDƏN silinmədi: 21.08.2026-dan əvvəlki qeydlərdə real
        /// planlaşdırılan bitmə vaxtı var və jurnal tarixçəsi pozulmamalıdır.
        /// Yeni müraciətlərdə `null` qalır; göstərmə qatı `null` olanda
        /// sadəcə heç nə yazmır.
        /// </summary>
        public DateTime? PlanBitme { get; set; }

        public string Meqsed { get; set; } = null!;
        public string? Marsrut { get; set; }

        public MasinMuracietStatus Status { get; set; } = MasinMuracietStatus.Gozlemede;

        // ── Rəhbər mərhələsi ─────────────────────────────────────────────
        /// <summary>Təsdiq/imtina edən rəhbərin işçi Id-si.</summary>
        public int? RehberId { get; set; }
        public Isci? Rehber { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }
        public string? ImtinaSebebi { get; set; }

        // ── Kassa mərhələsi ──────────────────────────────────────────────
        /// <summary>Açarın verildiyi an — kassa «Çıxdı» düyməsini basanda.</summary>
        public DateTime? CixisTarixi { get; set; }
        /// <summary>«Çıxdı» düyməsini basan işçi (kassa və ya rəhbər).</summary>
        public int? CixisQeydEdenId { get; set; }
        public Isci? CixisQeydEden { get; set; }

        /// <summary>Açarın geri alındığı an — kassa «Gəldi» düyməsini basanda.</summary>
        public DateTime? QayidisTarixi { get; set; }
        /// <summary>«Gəldi» düyməsini basan işçi (kassa və ya rəhbər).</summary>
        public int? QayidisQeydEdenId { get; set; }
        public Isci? QayidisQeydEden { get; set; }

        /// <summary>
        /// Çıxış/qayıdış anındakı spidometr.
        /// ⚠️ 19.08.2026 QƏRARI ilə ekranda YOXDUR — bax <see cref="Masin.CariKm"/>.
        /// Sütun gələcək üçün saxlanılır, hazırda həmişə boşdur.
        /// </summary>
        public int? CixisKm { get; set; }
        public int? QayidisKm { get; set; }
    }
}
