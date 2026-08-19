using FinNex.Application.Interfaces.Avtopark;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Avtopark.Controllers
{
    /// <summary>
    /// Maşın müraciətlərinin təsdiq paneli — Rəhbər (və Admin).
    ///
    /// Admin da daxildir: rəhbər olmayanda və ya sistemdə hələ rəhbər rolu
    /// təyin edilməyəndə axın dayanmasın. Kim təsdiqlədiyi jurnalda
    /// `RehberId` ilə qalır, yəni «admin təsdiqlədi» halı gizlənmir.
    /// </summary>
    [Area("Avtopark")]
    [Authorize(Roles = RoleNames.Rehber + "," + RoleNames.Admin)]
    public class TesdiqController : AvtoparkControllerBase
    {
        private readonly IMasinMuracietService _muraciet;

        public TesdiqController(IMasinMuracietService muraciet, UserManager<AppUser> userManager)
            : base(userManager) => _muraciet = muraciet;

        public async Task<IActionResult> Index()
        {
            var list = await _muraciet.GetTesdiqGozleyenlerAsync();
            ViewBag.AcigCixisSayi = (await _muraciet.GetAcigCixislarAsync()).Count;
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tesdiq(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return IsciProfiliYoxdur();

            var res = await _muraciet.TesdiqEtAsync(id, isciId.Value, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Imtina(int id, string? sebeb)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return IsciProfiliYoxdur();

            var res = await _muraciet.ImtinaEtAsync(id, isciId.Value, sebeb, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Qayıtmayanlar — kimin üstündə maşın qalıb.</summary>
        public async Task<IActionResult> AcigCixislar()
        {
            var list = await _muraciet.GetAcigCixislarAsync();
            return View(list);
        }
    }
}
