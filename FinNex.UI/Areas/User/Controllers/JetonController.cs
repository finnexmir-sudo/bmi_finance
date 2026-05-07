using FinNex.Application.DTOs.HR.Jeton;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class JetonController : Controller
    {
        private readonly IJetonService _jetonService;
        private readonly IReytingService _reytingService;
        private readonly UserManager<AppUser> _userManager;

        public JetonController(IJetonService jetonService, IReytingService reytingService, UserManager<AppUser> userManager)
        {
            _jetonService = jetonService;
            _reytingService = reytingService;
            _userManager = userManager;
        }

        // ── GET /User/Jeton ──────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
                return RedirectToAction("Index", "Dashboard");

            var isciId = appUser.IsciId.Value;
            var balans = await _jetonService.AktivSaatBalansiAsync(isciId);
            var qaraVar = await _jetonService.AktivQaraJetonuVarmiAsync(isciId);
            var teyinatlar = await _jetonService.JetonTeyinatlariGetirAsync();

            ViewBag.Balans = balans;
            ViewBag.QaraVar = qaraVar;
            ViewBag.MusbetTeyinatlar = teyinatlar.Where(t => t.Nov == FinNex.Domain.Entities.HR.JetonNovu.Musbat).ToList();
            ViewData["Title"] = "Cüzdanım";
            return View();
        }

        // ── GET /User/Jeton/GetJetonlarim ────────────────────
        [HttpGet]
        public async Task<IActionResult> GetJetonlarim()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
                return Json(new { success = false });

            var list = await _jetonService.IsciAktivJetonlariniGetirAsync(appUser.IsciId.Value);
            return Json(new { success = true, data = list });
        }

        // ── GET /User/Jeton/GetRedimTarixcesi ────────────────
        [HttpGet]
        public async Task<IActionResult> GetRedimTarixcesi()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
                return Json(new { success = false });

            var list = await _jetonService.IsciRedimTarixcesiGetirAsync(appUser.IsciId.Value);
            return Json(new { success = true, data = list });
        }

        // ── POST /User/Jeton/RedimGonder ─────────────────────
        [HttpPost]
        public async Task<IActionResult> RedimGonder([FromBody] JetonRedimTelebiCreateDto dto)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
                return Json(new { success = false, message = "İşçi tapılmadı." });

            var result = await _jetonService.RedimTelebiYaratAsync(appUser.IsciId.Value, dto);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── GET /User/Jeton/GetReyting ───────────────────────
        [HttpGet]
        public async Task<IActionResult> GetReyting()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
                return Json(new { success = false });

            var dto = await _reytingService.IsciCariReytinqiniGetirAsync(appUser.IsciId.Value);
            return Json(new { success = true, data = dto });
        }
    }
}
