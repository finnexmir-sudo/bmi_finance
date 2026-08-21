using System.ComponentModel.DataAnnotations;
using FinNex.Domain.Entities.Avtopark;

namespace FinNex.Application.DTOs.Avtopark
{
    /// <summary>Maşın müraciəti — siyahı/detal üçün.</summary>
    public class MasinMuracietListDto
    {
        public int Id { get; set; }

        public int MasinId { get; set; }
        public string MasinAdi { get; set; } = "";
        public string MasinNomresi { get; set; } = "";

        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = "";
        public string? SobeAdi { get; set; }

        public DateTime PlanBaslama { get; set; }
        /// <summary>Yalnız KÖHNƏ qeydlərdə dolu — bax `MasinMuraciet.PlanBitme`.</summary>
        public DateTime? PlanBitme { get; set; }
        public string Meqsed { get; set; } = "";
        public string? Marsrut { get; set; }

        public MasinMuracietStatus Status { get; set; }

        public string? RehberAdi { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }
        public string? ImtinaSebebi { get; set; }

        public DateTime? CixisTarixi { get; set; }
        public string? CixisQeydEdenAdi { get; set; }
        public DateTime? QayidisTarixi { get; set; }
        public string? QayidisQeydEdenAdi { get; set; }

        public string StatusMetni => Status switch
        {
            MasinMuracietStatus.Gozlemede => "Rəhbər təsdiqini gözləyir",
            MasinMuracietStatus.Tesdiqlenib => "Təsdiqlənib — açar gözləyir",
            MasinMuracietStatus.Cixib => "Çıxıb (maşın çöldədir)",
            MasinMuracietStatus.Qayidib => "Qayıdıb",
            MasinMuracietStatus.ImtinaEdildi => "İmtina edildi",
            MasinMuracietStatus.LegvEdildi => "Ləğv edildi",
            _ => Status.ToString()
        };

        /// <summary>Bootstrap rəng sinfi — siyahılarda nişan üçün.</summary>
        public string StatusRengi => Status switch
        {
            MasinMuracietStatus.Gozlemede => "warning",
            MasinMuracietStatus.Tesdiqlenib => "info",
            MasinMuracietStatus.Cixib => "primary",
            MasinMuracietStatus.Qayidib => "success",
            MasinMuracietStatus.ImtinaEdildi => "danger",
            MasinMuracietStatus.LegvEdildi => "secondary",
            _ => "secondary"
        };

        /// <summary>
        /// «Rəhbər təsdiqi» addımı bu müraciətdə ÜMUMİYYƏTLƏ varmı.
        ///
        /// ⚠️ Servisdəki qayda ilə EYNİ olmalıdır (`MasinMuracietService.IlkinStatus`):
        /// müraciət edən özü rəhbərdirsə addım atlanır. Şərti Razor içində
        /// yenidən qurma — CLAUDE.md «Rol Prioriteti» tələsi məhz budur.
        /// Controller bu bayrağı doldurur.
        /// </summary>
        public bool RehberAddimiVar { get; set; } = true;

        /// <summary>İşçi bu müraciəti hələ ləğv edə bilirmi (açar verilməyibsə).</summary>
        public bool LegvEdileBiler =>
            Status is MasinMuracietStatus.Gozlemede or MasinMuracietStatus.Tesdiqlenib;
    }

    /// <summary>Yeni maşın müraciəti.</summary>
    public class MasinMuracietCreateDto
    {
        [Required(ErrorMessage = "Maşın seçilməlidir.")]
        [Display(Name = "Maşın")]
        public int MasinId { get; set; }

        /// <summary>Servis doldurur (giriş etmiş istifadəçidən) — formadan GƏLMİR.</summary>
        public int IsciId { get; set; }

        // ── ÇIXIŞ VAXTI — TARİX və SAAT AYRI SAHƏLƏRDƏ (21.08.2026) ──────
        // Əvvəl tək `datetime-local` input idi. İki səbəbdən bölündü:
        //   · tarix demək olar həmişə BUGÜNDÜR — default doldurulur;
        //   · `datetime-local`-un saat hissəsi brauzerdən-brauzerə fərqli
        //     davranır; layihədə İcazə modulunda artıq işlənən «HH:mm» mətn
        //     maskası (rəqəm yazılır, «:» avtomatik qoyulur) daha rahatdır.
        // Servis ikisini birləşdirib `MasinMuraciet.PlanBaslama` yazır.

        [Required(ErrorMessage = "Tarix seçilməlidir.")]
        [DataType(DataType.Date)]
        [Display(Name = "Tarix")]
        public DateTime Tarix { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Saat yazılmalıdır (məs. 10:00).")]
        [RegularExpression(@"^([01]?\d|2[0-3]):[0-5]\d$",
            ErrorMessage = "Saat «SS:DD» formatında olmalıdır (məs. 10:00).")]
        [Display(Name = "Saat")]
        public string Saat { get; set; } = "";

        [Required(ErrorMessage = "Məqsəd yazılmalıdır.")]
        [StringLength(300)]
        [Display(Name = "Məqsəd")]
        public string Meqsed { get; set; } = "";

        [StringLength(300)]
        [Display(Name = "Marşrut")]
        public string? Marsrut { get; set; }

        /// <summary>
        /// Müraciət edən özü Rəhbərdirmi — marşrutu bu həll edir.
        /// Controller `User.IsInRole` ilə doldurur; formadan GƏLMİR
        /// (gəlsəydi istifadəçi POST-u dəyişib öz müraciətini avtomatik
        /// təsdiqlədə bilərdi).
        /// </summary>
        public bool MuracietSahibiRehberdirmi { get; set; }
    }
}
