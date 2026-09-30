using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Services.HR;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// Əmək məzuniyyəti hüququ hesablamasının qanuni ədədləri (yaş həddi,
    /// əlavə gün, staj pilləsi) — 30.09.2026, istifadəçi tələbi: "qanun
    /// dəyişsə kod dəyişməli olmasın". Yalnız HR/Admin — Rehber bura
    /// daxil DEYİL (bütün şirkətin məzuniyyət hesablamasına təsir edir,
    /// Vəzifə İşdə statusundan fərqli olaraq maaş/balansa birbaşa toxunur).
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
    public class MezuniyyetHuquqParametrleriController : Controller
    {
        private readonly IMezuniyyetHuquqParametrleriService _service;

        public MezuniyyetHuquqParametrleriController(IMezuniyyetHuquqParametrleriService service)
        {
            _service = service;
        }

        // GET /HR/MezuniyyetHuquqParametrleri
        public async Task<IActionResult> Index()
        {
            var dto = await _service.AlAsync();
            ViewData["Title"] = "Məzuniyyət Hüququ Parametrləri";
            return View(dto);
        }

        // POST /HR/MezuniyyetHuquqParametrleri
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(MezuniyyetHuquqParametrleriDto model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Formada səhv var — yenidən yoxlayın.";
                return View(model);
            }

            int? icraciId = int.TryParse(
                User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out var id) ? id : null;

            var result = await _service.SaxlaAsync(model, icraciId);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
