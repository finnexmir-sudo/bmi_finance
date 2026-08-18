namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Bazar (Mərkəzi Bank / banklararası) kredit faiz dərəcəsi — VM 98.2.1 üzrə
    /// işçi kreditlərinin hesabi gəlirini hesablamaq üçün.
    ///
    /// Mühasib əl ilə idarə edir: dərəcə dəyişəndə YENİ SƏTİR əlavə olunur,
    /// köhnəsi silinmir. Beləliklə keçmiş dövrlər öz vaxtındakı dərəcə ilə
    /// yenidən hesablana bilir (audit izi).
    ///
    /// AXTARIŞ QAYDASI: verilmiş tarix və valyuta üçün <c>Tarix &lt;= hədəf</c>
    /// olan ƏN SON sətir götürülür.
    ///
    /// VALYUTA: BMI `kurval` kodu saxlanılır (`SOKNAMEVALUT`) — «00» = AZN.
    /// Eyni kod Oracle sorğusunda `SUBSTR(licschpkre, 6, 2)` ilə gəlir, ona görə
    /// tutuşdurma birbaşadır. Beynəlxalq qısaltma (USD/EUR) `kurval`-da YOXDUR —
    /// ona görə ISO kodu DEYİL, BMI kodu saxlanılır.
    /// Bax: CLAUDE.md — «Kredit Müqaviləsi — Şablonlar YALNIZ AZN üçündür».
    ///
    /// 18.08.2026: bu gün valyutalı işçi krediti YOXDUR (açıq portfeldə 310/310
    /// AZN). Cədvəl gələcək üçündür — dərəcə yazılmayan valyutada hesablama
    /// SƏSSİZCƏ 0 vermir, sətir «dərəcə təyin edilməyib» kimi görünür.
    /// </summary>
    public class KreditFaizDerecesi : BaseEntity
    {
        /// <summary>Dərəcənin qüvvəyə mindiyi tarix (bu tarix DAXİLDİR).</summary>
        public DateTime Tarix { get; set; }

        /// <summary>BMI `kurval` valyuta kodu — «00» = AZN.</summary>
        public string ValyutaKodu { get; set; } = AznKodu;

        /// <summary>İllik faiz dərəcəsi, faizlə (məs. 9,25).</summary>
        public decimal Derece { get; set; }

        /// <summary>Mənbə/qərar qeydi — məs. «MB qərarı 25.07.2026».</summary>
        public string? Qeyd { get; set; }

        /// <summary>AZN-in BMI `kurval` kodu. Sabit yazma — bu sabiti işlət.</summary>
        public const string AznKodu = "00";
    }
}
