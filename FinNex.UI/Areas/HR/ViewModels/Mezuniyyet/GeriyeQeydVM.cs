using System.ComponentModel.DataAnnotations;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels.Mezuniyyet
{
    public class GeriyeQeydVM
    {
        [Required(ErrorMessage = "İşçi seçilməlidir.")]
        [Display(Name = "İşçi")]
        public int IsciId { get; set; }

        [Required(ErrorMessage = "Məzuniyyət növü seçilməlidir.")]
        [Display(Name = "Məzuniyyət növü")]
        public MezuniyyetNovu Nov { get; set; } = MezuniyyetNovu.Illik;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Başlama tarixi")]
        public DateTime BaslamaTarixi { get; set; } = DateTime.Today.AddDays(-1);

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Bitmə tarixi")]
        public DateTime BitmeTarixi { get; set; } = DateTime.Today.AddDays(-1);

        [MaxLength(500)]
        [Display(Name = "Səbəb")]
        public string? Sebeb { get; set; }

        /// <summary>
        /// Əmr nömrəsi suffiksi (frekans). Default "G" — Geriyə.
        /// HR seçimə uyğun başqa qısa kod verə bilər.
        /// </summary>
        [MaxLength(4)]
        [Display(Name = "Əmr suffiksi")]
        public string? EmrSuffiks { get; set; } = "G";

        /// <summary>
        /// İSTƏGƏ BAĞLI — mövcud əmr nömrəsinə "araya əlavə et" rejimi.
        /// Boş qaldıqda: avtomatik son+1 yeni nömrə verilir.
        /// Doldurulduqda: bu nömrəyə suffiks əlavə olunur (məs. K/M 5G 2026).
        /// </summary>
        [Display(Name = "Mövcud əmr nömrəsi")]
        public int? EmrRegem { get; set; }

        /// <summary>
        /// İSTƏGƏ BAĞLI — yalnız <see cref="EmrRegem"/> verilibsə əhəmiyyətli.
        /// Default: cari il.
        /// </summary>
        [Display(Name = "Mövcud əmr ili")]
        public int? EmrIl { get; set; }

        public List<SelectListItem> Isciler { get; set; } = new();

        /// <summary>Suffiks variantları üçün seçim siyahısı (UI-da dropdown).</summary>
        public List<SelectListItem> SuffiksSecimleri { get; set; } = new()
        {
            new SelectListItem { Value = "G", Text = "G — Geriyə qeyd" },
            new SelectListItem { Value = "X", Text = "X — Xüsusi hal" },
            new SelectListItem { Value = "T", Text = "T — Təcili" },
            new SelectListItem { Value = "",  Text = "(suffiksiz)" }
        };

        public int MaksimumKohneGun { get; set; } = 90;
    }
}
