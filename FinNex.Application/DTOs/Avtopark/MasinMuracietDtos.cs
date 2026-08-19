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
        public DateTime PlanBitme { get; set; }
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

        [Required(ErrorMessage = "Başlama tarixi və saatı seçilməlidir.")]
        [Display(Name = "Başlama")]
        public DateTime PlanBaslama { get; set; }

        [Required(ErrorMessage = "Bitmə tarixi və saatı seçilməlidir.")]
        [Display(Name = "Bitmə")]
        public DateTime PlanBitme { get; set; }

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
