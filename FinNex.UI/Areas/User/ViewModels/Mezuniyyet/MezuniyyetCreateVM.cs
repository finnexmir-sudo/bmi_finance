using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.User.ViewModels.Mezuniyyet
{
    /// <summary>
    /// İşçi tərəfindən yeni məzuniyyət müraciəti üçün ViewModel.
    /// Domain entity-lərinə istinad yoxdur — Nov int kimi saxlanılır.
    /// </summary>
    public class MezuniyyetCreateVM
    {
        // ── Sistem avtomatik doldurur ──────────────────────────
        public int IsciId { get; set; }

        // ── Form sahələri ─────────────────────────────────────
        public int Nov { get; set; } = 1; // İşçi yalnız Əmək məzuniyyəti (1) müraciət edə bilər

        [Required(ErrorMessage = "Başlama tarixi seçilməlidir")]
        public DateTime BaslamaTarixi { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Bitmə tarixi seçilməlidir")]
        public DateTime BitmeTarixi { get; set; } = DateTime.Today.AddDays(6);

        public int? EvezEdenIsciId { get; set; }

        [MaxLength(500, ErrorMessage = "Qeyd 500 simvoldan çox ola bilməz")]
        public string? Qeyd { get; set; }

        // 1 = AySonuOdenis (default), 2 = QabaqcadanOdenis
        public int OdenisTipi { get; set; } = 1;

        // ── Dropdown məlumatları ───────────────────────────────
        public List<SelectListItem> NovList { get; set; } = new()
        {
            new SelectListItem("Əmək məzuniyyəti",      "1"),
            new SelectListItem("Xəstəlik məzuniyyəti",  "2"),
            new SelectListItem("Ezamiyyət",             "3"),
        };

        public List<SelectListItem> EvezEdenList { get; set; } = new();

        // ── Qalıq balans (yalnız göstərmək üçün) ─────────────
        public int IllikToplamGun { get; set; }
        public int IllikIstifadeGun { get; set; }
        public int IllikQaligGun => IllikToplamGun - IllikIstifadeGun;

        public int XestelikToplamGun { get; set; }
        public int XestelikIstifadeGun { get; set; }
        public int XestelikQaligGun => XestelikToplamGun - XestelikIstifadeGun;

        public int EzamiyyetToplamGun { get; set; }
        public int EzamiyyetIstifadeGun { get; set; }
        public int EzamiyyetQaligGun => EzamiyyetToplamGun - EzamiyyetIstifadeGun;

        // ── Seçilmiş növə görə qalıq günü qaytarır ───────────
        public int SecilenNovQaliq => Nov switch
        {
            1 => IllikQaligGun,
            2 => XestelikQaligGun,
            3 => EzamiyyetQaligGun,
            _ => 0
        };
    }
}
