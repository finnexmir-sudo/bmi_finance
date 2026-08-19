using FinNex.Application.Interfaces.Avtopark;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Avtopark.Controllers
{
    /// <summary>
    /// Açar jurnalı — «Çıxdı / Gəldi».
    ///
    /// KİM GÖRÜR (19.08.2026 qərarı): `Kassa` rolu (açarlar fiziki olaraq
    /// kassada saxlanılır) **və əlavə olaraq** `Rehber` — kassa işçisi
    /// olmayanda axın dayanmasın. `Admin` də daxildir.
    ///
    /// Düymə açarın ƏLDƏN-ƏLƏ KEÇDİYİ anı qeyd edir — vaxt `DateTime.Now`-dan
    /// gəlir, formadan YOX. Formadan gəlsəydi jurnal həqiqətə uyğun olmazdı.
    /// «Kim basdı» sualının cavabı `CixisQeydEdenId` / `QayidisQeydEdenId`-dədir.
    /// </summary>
    [Area("Avtopark")]
    [Authorize(Roles = RoleNames.Kassa + "," + RoleNames.Rehber + "," + RoleNames.Admin)]
    public class KassaController : AvtoparkControllerBase
    {
        private readonly IMasinMuracietService _muraciet;

        public KassaController(IMasinMuracietService muraciet, UserManager<AppUser> userManager)
            : base(userManager) => _muraciet = muraciet;

        public async Task<IActionResult> Index()
        {
            var list = await _muraciet.GetKassaSiyahisiAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cixdi(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return IsciProfiliYoxdur();

            var res = await _muraciet.CixdiAsync(id, isciId.Value, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Geldi(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return IsciProfiliYoxdur();

            var res = await _muraciet.GeldiAsync(id, isciId.Value, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Tarix aralığı üzrə açar jurnalı.</summary>
        public async Task<IActionResult> Jurnal(DateTime? bas, DateTime? son, int? masinId)
        {
            ViewBag.Bas = (bas ?? DateTime.Today.AddDays(-30)).ToString("yyyy-MM-dd");
            ViewBag.Son = (son ?? DateTime.Today).ToString("yyyy-MM-dd");
            ViewBag.MasinId = masinId;

            var list = await _muraciet.GetJurnalAsync(bas, son, masinId);
            return View(list);
        }
    }
}
