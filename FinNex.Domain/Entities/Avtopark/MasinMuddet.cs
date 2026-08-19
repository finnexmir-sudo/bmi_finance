namespace FinNex.Domain.Entities.Avtopark
{
    /// <summary>
    /// Maşının bitmə tarixi olan öhdəliyi — sığorta, texniki baxış, yağ dəyişmə…
    ///
    /// YENİLƏNMƏ QAYDASI: müddət uzadılanda köhnə sətir SİLİNMİR —
    /// <see cref="Aktivdir"/> = false olur, yeni sətir əlavə edilir. Beləliklə
    /// «bu maşının sığortası nə vaxt hansı məbləğə uzadılıb» tarixçəsi qalır.
    /// Xəbərdarlıq yalnız <see cref="Aktivdir"/> sətirlərə baxır.
    ///
    /// ⚠️ 19.08.2026 QƏRARI — YALNIZ TARİXƏ GÖRƏ. Kilometrə görə izləmə
    /// («hər 10 000 km yağ dəyişmə») İSTİFADƏ OLUNMUR; yağ dəyişmə də tarixlə,
    /// ildə bir dəfə yazılır. <see cref="SonKm"/> gələcək üçün boş saxlanılır —
    /// heç bir hesablama ona baxmır.
    /// </summary>
    public class MasinMuddet : BaseEntity
    {
        public int MasinId { get; set; }
        public Masin Masin { get; set; } = null!;

        public int NovId { get; set; }
        public MasinMuddetNovu Nov { get; set; } = null!;

        /// <summary>Müddətin bitdiyi tarix — xəbərdarlıq bundan geriyə sayılır.</summary>
        public DateTime SonTarix { get; set; }

        /// <summary>Neçə gün əvvəl xəbərdarlıq getsin. Növün defaultu ilə açılır.</summary>
        public int XeberdarliqGun { get; set; } = 30;

        /// <summary>Ödənilən məbləğ (sığorta haqqı, baxış rüsumu…) — məlumat üçün.</summary>
        public decimal? Mebleg { get; set; }

        /// <summary>
        /// Polis/qəbz faylının DMS-dəki NİSBİ yolu — məs. «avtopark/abc123.pdf».
        /// Mütləq yol saxlanılmır (CLAUDE.md — fayl saxlama qaydası).
        /// </summary>
        public string? SenedFaylYolu { get; set; }

        /// <summary>Faylın istifadəçiyə göstərilən orijinal adı.</summary>
        public string? SenedFaylAdi { get; set; }

        public string? Qeyd { get; set; }

        /// <summary>Cari sətirdirmi. Uzadılanda köhnəsi false olur, silinmir.</summary>
        public bool Aktivdir { get; set; } = true;

        /// <summary>
        /// Bu sətir üçün xəbərdarlıq artıq göndərilibmi.
        /// Fon xidməti saatda bir işə düşür — bayraq olmasa eyni xəbərdarlıq
        /// gündə 24 dəfə yazılardı. Bildiriş servisinin 15 saniyəlik dublikat
        /// qoruması bu qədər uzun aralığı tutmur.
        /// </summary>
        public bool XeberdarliqGonderilib { get; set; } = false;

        /// <summary>Xəbərdarlığın göndərildiyi an (audit üçün).</summary>
        public DateTime? XeberdarliqTarixi { get; set; }

        /// <summary>⚠️ İSTİFADƏ OLUNMUR — bax sinif şərhi. Həmişə boşdur.</summary>
        public int? SonKm { get; set; }
    }
}
