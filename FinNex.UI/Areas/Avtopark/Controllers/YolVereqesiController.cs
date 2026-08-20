using FinNex.Application.Interfaces.Avtopark;
using FinNex.Domain;
using FinNex.UI.Services.Kredit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FinNex.UI.Areas.Avtopark.Controllers
{
    /// <summary>
    /// «Minik avtomobillərin yol vərəqəsi» — Word sənədi hazırlayır.
    ///
    /// GİRİŞ: `[Authorize]` — **hər işçi** (istifadəçi qərarı, 20.08.2026).
    /// Sənəd sürücünün özündə olmalıdır, ona görə məhdudlaşdırılmır.
    ///
    /// «Qaracdan çıxış / Qaraca qayıdan vaxt» sətirləri **şablonda sabitdir**
    /// (00:01 / 24:00) və kod onlara TOXUNMUR — istifadəçi qərarı: «çıxış
    /// qayıdış olduğu kimi qalır». Ona görə şablonda o iki sətir üçün token
    /// da yoxdur; dəyişmək lazım olsa Word şablonunun özündə düzəldilir.
    /// </summary>
    [Area("Avtopark")]
    [Authorize]
    public class YolVereqesiController : AvtoparkControllerBase
    {
        private readonly IMasinService _masin;
        private readonly IConfiguration _config;

        public YolVereqesiController(
            IMasinService masin,
            IConfiguration config,
            UserManager<AppUser> userManager) : base(userManager)
        {
            _masin = masin;
            _config = config;
        }

        // Şablon DMS-dədir (CLAUDE.md — fayllar `wwwroot`-a YAZILMIR/oxunmur).
        // Repodakı nüsxə: docs/sablon/avtopark/Yol_vereqesi.docx
        private string SablonYolu()
        {
            var dms = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
            return Path.Combine(dms, "hesabat-sablonlari", "avtopark", "Yol_vereqesi.docx");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await DoldurAsync();

            // Standart dövr — cari ilin əvvəlindən sonuna. Forma açılanda hazır gəlsin.
            var il = DateTime.Today.Year;
            ViewBag.BasTarix = new DateTime(il, 1, 1).ToString("yyyy-MM-dd");
            ViewBag.SonTarix = new DateTime(il, 12, 31).ToString("yyyy-MM-dd");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yarat(int masinId, string? surucu, string? bt, string? st)
        {
            var m = await _masin.GetirAsync(masinId);
            if (m == null)
            {
                TempData["Error"] = "Maşın seçilməyib.";
                return RedirectToAction(nameof(Index));
            }

            var bas = ParseTarix(bt);
            var son = ParseTarix(st);
            if (bas == null || son == null)
            {
                TempData["Error"] = "Dövrün başlama və bitmə tarixi seçilməlidir.";
                return RedirectToAction(nameof(Index));
            }
            if (bas > son)
            {
                TempData["Error"] = "Başlama tarixi bitmə tarixindən sonra ola bilməz.";
                return RedirectToAction(nameof(Index));
            }

            // Sürücü boş qalarsa maşının təhkim olunmuş sürücüsü yazılır.
            var surucuAd = string.IsNullOrWhiteSpace(surucu) ? m.TehkimSurucuAdi : surucu.Trim();
            if (string.IsNullOrWhiteSpace(surucuAd))
            {
                TempData["Error"] = "Sürücü yazılmayıb və maşına təhkim olunmuş sürücü yoxdur.";
                return RedirectToAction(nameof(Index));
            }

            // Şablon yoxlaması SƏNƏD HAZIRLANMAZDAN ƏVVƏL — istifadəçi boş fayl almasın.
            var sablon = SablonYolu();
            if (!System.IO.File.Exists(sablon))
            {
                TempData["Error"] = $"Yol vərəqəsi şablonu tapılmadı: {sablon}";
                return RedirectToAction(nameof(Index));
            }

            var tokenler = new Dictionary<string, string?>
            {
                ["{dovr}"]   = DovrMetni(bas.Value, son.Value),
                ["{marka}"]  = $"{m.Marka} {m.Model}".Trim(),
                ["{nomre}"]  = m.DovletNomresi,
                ["{surucu}"] = surucuAd,
                // Rəhbərin adı əmrlərdəki ilə EYNİ mənbədən — iki yerdə saxlansa
                // biri dəyişəndə o biri köhnə qalar.
                ["{rehber}"] = _config["Emr:MudirAd"] ?? ""
            };

            var bayt = KreditWordService.Doldur(sablon, tokenler);
            var ad = $"Yol_vereqesi_{m.DovletNomresi}_{bas:yyyyMMdd}_{son:yyyyMMdd}.docx";
            return File(bayt,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ad);
        }

        // ═════════════════════════════════════════════════════════════════════

        private async Task DoldurAsync()
        {
            // Yalnız AKTİV maşınlar — təmirdə/istifadədən çıxmış maşına yol
            // vərəqəsi yazılmaz.
            ViewBag.Masinlar = await _masin.HamisiniGetirAsync(yalnizAktiv: true);
        }

        private static DateTime? ParseTarix(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            string[] formatlar = { "yyyy-MM-dd", "dd.MM.yyyy", "dd/MM/yyyy", "dd-MM-yyyy" };
            foreach (var f in formatlar)
                if (DateTime.TryParseExact(s.Trim(), f, System.Globalization.CultureInfo.InvariantCulture,
                                           System.Globalization.DateTimeStyles.None, out var d))
                    return d;
            return DateTime.TryParse(s, out var d2) ? d2 : null;
        }

        private static readonly string[] Aylar =
        {
            "yanvar", "fevral", "mart", "aprel", "may", "iyun",
            "iyul", "avqust", "sentyabr", "oktyabr", "noyabr", "dekabr"
        };

        /// <summary>
        /// «05 yanvar 2026-cı ildən 31 dekabr 2026-cı il tarixədək»
        ///
        /// Ay adları ƏL İLƏ yazılıb — `ToString("MMMM")` server mədəniyyətindən
        /// asılıdır və mühitdən-mühitə dəyişə bilər.
        ///
        /// ⚠️ ORİJİNAL ŞABLONDAN İKİ FƏRQ (20.08.2026):
        ///   1. Orijinalda başlanğıc sözlə («05 yanvar 2026»), bitmə isə rəqəmlə
        ///      («31.12.2026») yazılmışdı — bir cümlədə iki format. Burada
        ///      hər ikisi sözlə yazılır.
        ///   2. Orijinalda bitmə tarixi «2026-ci il» idi; düzgünü «2026-cı»dır
        ///      (altı → ı). Sıra şəkilçisi <see cref="IlSekilcisi"/> ilə hesablanır.
        /// Orijinal yazılış lazımdırsa dəyişiklik yalnız bu metoddadır.
        /// </summary>
        private static string DovrMetni(DateTime bas, DateTime son)
            => $"{bas:dd} {Aylar[bas.Month - 1]} {bas.Year}-{IlSekilcisi(bas.Year)} ildən " +
               $"{son:dd} {Aylar[son.Month - 1]} {son.Year}-{IlSekilcisi(son.Year)} il tarixədək";

        /// <summary>
        /// İlin sıra sayı şəkilçisi (2026 → «cı», 2027 → «ci», 2030 → «cu»).
        /// Şəkilçi son rəqəmin OXUNUŞUNA görədir: altı→ı, üç/dörd→ü, doqquz→u.
        /// Son rəqəm 0 olanda onluğun oxunuşu həlledicidir (2030 «otuzuncu»).
        /// </summary>
        private static string IlSekilcisi(int il)
        {
            var son1 = il % 10;
            if (son1 != 0)
                return son1 switch
                {
                    3 or 4 => "cü",
                    6      => "cı",
                    9      => "cu",
                    _      => "ci"   // 1, 2, 5, 7, 8
                };

            return (il % 100) switch
            {
                10 or 30 => "cu",     // onuncu, otuzuncu
                40 or 60 or 90 => "cı", // qırxıncı, altmışıncı, doxsanıncı
                _ => "ci"             // iyirminci, əllinci, yetmişinci, səksəninci, mininci
            };
        }
    }
}
