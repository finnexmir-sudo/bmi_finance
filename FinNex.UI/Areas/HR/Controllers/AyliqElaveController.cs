using FinNex.Application.DTOs.HR.AyliqElave;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// İşçilərə aylıq Bonus / Overtime təyin edən səhifə.
    /// Bütün biznes məntiqi <see cref="IAyliqElaveService"/> daxilindədir;
    /// controller yalnız səhifə render və form post-u idarə edir.
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib + "," + RoleNames.Rehber)]
    public class AyliqElaveController : Controller
    {
        private readonly IAyliqElaveService _service;

        public AyliqElaveController(IAyliqElaveService service)
        {
            _service = service;
        }

        // ── GET /HR/AyliqElave ───────────────────────────────────────────────
        public async Task<IActionResult> Index(int? il, int? ay)
        {
            var cIl = il ?? DateTime.Now.Year;
            var cAy = ay ?? DateTime.Now.Month;
            if (cAy < 1 || cAy > 12) cAy = DateTime.Now.Month;

            var setirler = await _service.GetSetirlerAsync(cIl, cAy);

            ViewBag.Il    = cIl;
            ViewBag.Ay    = cAy;
            ViewBag.Iller = Enumerable.Range(DateTime.Now.Year - 2, 4).ToList();
            ViewData["Title"] = "Aylıq Əlavə — Bonus / Overtime";
            return View(setirler);
        }

        // ── POST /HR/AyliqElave/Saxla ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Saxla(int il, int ay, List<AyliqElaveSetirDto> setirler)
        {
            var r = await _service.SaxlaAsync(il, ay, setirler ?? new List<AyliqElaveSetirDto>());

            if (!r.Success)
            {
                TempData["Error"] = r.Message;
            }
            else
            {
                TempData["Success"] = r.Data > 0
                    ? $"Yadda saxlanıldı — {r.Data} qeyd yeniləndi."
                    : "Dəyişiklik olmadı.";
            }

            return RedirectToAction(nameof(Index), new { il, ay });
        }
    }
}
