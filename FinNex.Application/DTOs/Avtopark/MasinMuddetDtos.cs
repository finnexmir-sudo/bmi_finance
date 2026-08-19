using System.ComponentModel.DataAnnotations;

namespace FinNex.Application.DTOs.Avtopark
{
    /// <summary>Müddət növü (sığorta, baxış, yağ…) — lookup.</summary>
    public class MasinMuddetNovuDto
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Ad mütləqdir.")]
        [StringLength(80)]
        [Display(Name = "Növün adı")]
        public string Ad { get; set; } = "";

        [Range(0, 365, ErrorMessage = "Xəbərdarlıq 0–365 gün aralığında olmalıdır.")]
        [Display(Name = "Xəbərdarlıq (gün)")]
        public int XeberdarliqGun { get; set; } = 30;

        [Display(Name = "Aktivdir")]
        public bool Aktivdir { get; set; } = true;

        [Display(Name = "Sıra")]
        public int Sira { get; set; }
    }

    /// <summary>Maşının müddət qeydi — oxumaq üçün.</summary>
    public class MasinMuddetDto
    {
        public int Id { get; set; }

        public int MasinId { get; set; }
        public string MasinAdi { get; set; } = "";
        public string MasinNomresi { get; set; } = "";

        public int NovId { get; set; }
        public string NovAdi { get; set; } = "";

        public DateTime SonTarix { get; set; }
        public int XeberdarliqGun { get; set; }
        public decimal? Mebleg { get; set; }
        public string? SenedFaylYolu { get; set; }
        public string? SenedFaylAdi { get; set; }
        public string? Qeyd { get; set; }
        public bool Aktivdir { get; set; }
        public bool XeberdarliqGonderilib { get; set; }

        /// <summary>Bitməsinə neçə gün qalıb (mənfi = keçib).</summary>
        public int QalanGun => (SonTarix.Date - DateTime.Today).Days;

        public bool Kecib => QalanGun < 0;
        public bool Yaxinlasir => !Kecib && QalanGun <= XeberdarliqGun;

        public string VeziyyetMetni =>
            Kecib ? $"{-QalanGun} gün keçib"
            : QalanGun == 0 ? "Bu gün bitir"
            : $"{QalanGun} gün qalıb";

        public string VeziyyetRengi => Kecib ? "danger" : Yaxinlasir ? "warning" : "success";
    }

    /// <summary>Müddət qeydi — yaratmaq/redaktə üçün.</summary>
    public class MasinMuddetCreateDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Maşın seçilməlidir.")]
        [Display(Name = "Maşın")]
        public int MasinId { get; set; }

        [Required(ErrorMessage = "Növ seçilməlidir.")]
        [Display(Name = "Növ")]
        public int NovId { get; set; }

        [Required(ErrorMessage = "Son tarix seçilməlidir.")]
        [Display(Name = "Son tarix")]
        public DateTime SonTarix { get; set; } = DateTime.Today;

        [Range(0, 365, ErrorMessage = "Xəbərdarlıq 0–365 gün aralığında olmalıdır.")]
        [Display(Name = "Neçə gün əvvəl xəbərdarlıq")]
        public int XeberdarliqGun { get; set; } = 30;

        [Display(Name = "Məbləğ")]
        public decimal? Mebleg { get; set; }

        [StringLength(500)]
        [Display(Name = "Qeyd")]
        public string? Qeyd { get; set; }

        // Fayl controller-də ayrıca `IFormFile` kimi qəbul olunur — Application
        // qatı `Microsoft.AspNetCore.Http`-ə bağlanmasın deyə DTO-da yalnız
        // yazılmış yol/ad saxlanılır.
        public string? SenedFaylYolu { get; set; }
        public string? SenedFaylAdi { get; set; }
    }

    /// <summary>Müddət xəbərdarlığını alacaq işçi.</summary>
    public class AvtoparkAliciDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = "";
        public string? SobeAdi { get; set; }
        public bool Aktivdir { get; set; }
    }
}
