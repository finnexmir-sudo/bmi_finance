namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Xəstəlik bülletəni. HR tərəfindən sənəd əsasında yazılır,
    /// yarandıqda avtomatik təsdiqlənir (workflow yoxdur).
    ///
    /// Xəstəlik ödənişi ayrıca XestelikOdenis cədvəlində saxlanılır
    /// və maaş hesablandığı zaman avtomatik brüt-ə əlavə olunur.
    ///
    /// Şirkət maksimum 14 iş günü ödəyir, qalanı DSMF tərəfindən
    /// (informativ — sistem hesablamır, sadəcə göstərir).
    /// </summary>
    public class Xestelik : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }

        /// <summary>
        /// İş günü sayı (BayramGunu cədvəlindən bayramlar + həftəsonu çıxılmış).
        /// Sistem avtomatik hesablayır.
        /// </summary>
        public int IsGunSayi { get; set; }

        /// <summary>Bülletən nömrəsi (məcburi)</summary>
        public string BulletenNomresi { get; set; } = null!;

        /// <summary>Müalicə müəssisəsi (istəğə bağlı)</summary>
        public string? MualiceMuessisesi { get; set; }

        public string? Qeyd { get; set; }

        public XestelikStatus Status { get; set; } = XestelikStatus.Tesdiqlenib;

        // Audit
        public int? HrId { get; set; }
        public DateTime? HrTesdiqTarixi { get; set; }

        // Ödənişlər (bir xəstəlik bir neçə aya bölünə bilər)
        public ICollection<XestelikOdenis> Odenisler { get; set; } = new List<XestelikOdenis>();
    }
}
