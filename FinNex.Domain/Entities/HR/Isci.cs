namespace FinNex.Domain.Entities.HR
{
    public class Isci : BaseEntity
    {
        // Şəxsi məlumatlar
        public string Ad { get; set; } = null!;
        public string Soyad { get; set; } = null!;
        public string? AtaAdi { get; set; }
        public string FIN { get; set; } = null!;
        public string SeriyaNomre { get; set; } = null!;
        public DateTime DogumTarixi { get; set; }
        public Cins Cins { get; set; }
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public string? Unvan { get; set; }

        // ── Məzuniyyət üçün şəxsi faktlar (Ə.M. 117) — HR əl ilə doldurur ──
        /// <summary>14 yaşınadək uşaqların sayı (M.117 əlavə məzuniyyəti üçün).</summary>
        public int UsaqSayi { get; set; } = 0;
        /// <summary>18 yaşınadək əlilliyi olan uşağı var (M.117 → +5 gün).</summary>
        public bool EngelliUsaqVar { get; set; } = false;
        /// <summary>Tək valideyn — M.117 əlavəsini ataya da açır.</summary>
        public bool TekValideyn { get; set; } = false;

        // İşçi statusu
        public DateTime IsheQebulTarixi { get; set; }
        public DateTime? IsdenAyrilmaTarixi { get; set; }
        public string? FesihSebebi { get; set; }
        public IsciStatus Status { get; set; } = IsciStatus.Aktiv;

        /// <summary>
        /// İşçi siyahılarında göstərmə sırası (kiçik nömrə əvvəl).
        /// HR-də "İşçi Sıralaması" səhifəsində drag-and-drop ilə təyin olunur.
        /// Yeni işçi əlavə olunduqda 0 olur — eyni sıralı işçilər arasında
        /// ad/soyad əlifba sırası ilə düzülür.
        /// </summary>
        public int Sira { get; set; } = 0;

        /// <summary>
        /// Ümumi iş stajı başlanğıc tarixi (əvvəlki iş yerləri daxil).
        /// LEGACY — köhnə hesablamalar üçün saxlanılır.
        /// Yeni yanaşma: EvvelkiStajPeriodlari (kitabçə) + bank tenure.
        /// </summary>
        public DateTime? UmumiIsStajiBaslangic { get; set; }

        /// <summary>
        /// İşçinin əvvəlki iş yerlərində iş dövrləri (əmək kitabçası).
        /// JSON: [{"s":"2010-01-15","e":"2015-06-30"}, ...]
        /// Cari ümumi staj = bu dövrlərin cəmi + bu bankda staj (IsheQebulTarixi → bu gün).
        /// </summary>
        public string? EvvelkiStajPeriodlari { get; set; }

        // Sistem əlaqəsi
        public int? AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        // Əlaqələr
        public IsciMaliye? Maliye { get; set; }
        public ICollection<MezuniyyetBalans> MezuniyyetBalanslari { get; set; } = new List<MezuniyyetBalans>();

        public ICollection<Maas> Maaslar { get; set; } = new List<Maas>();
        public ICollection<IsciMaasTarixcesi> MaasTarixcesi { get; set; } = new List<IsciMaasTarixcesi>();
        public ICollection<IsciTeyinat> IsciTeyinatlari { get; set; } = new List<IsciTeyinat>();

        public string TamAd => $"{Ad} {Soyad} {AtaAdi}";
    }
}