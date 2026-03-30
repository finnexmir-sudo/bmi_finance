// Areas/User/Controllers/XatirlatmaController.cs
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Services.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.User.ViewModels.Xatirlatma;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class XatirlatmaController : Controller
    {
        private readonly IXatirlatmaService _xatirlatmaService;
        private readonly UserManager<AppUser> _userManager;

        public XatirlatmaController(
            IXatirlatmaService xatirlatmaService,
            UserManager<AppUser> userManager)
        {
            _xatirlatmaService = xatirlatmaService;
            _userManager = userManager;
        }

        // ── GET /User/Xatirlatma ─────────────────────────────
        public async Task<IActionResult> Index()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _xatirlatmaService.GetIsciXatirlatmalariAsync(isciId.Value);

            var vm = new XatirlatmaIndexVM
            {
                Xatirlatmalar = result.Success ? result.Data!.ToList() : new(),
            };

            ViewData["Title"] = "Xatırlatmalar";
            return View(vm);
        }

        // ── GET /User/Xatirlatma/Yarat ───────────────────────
        public IActionResult Yarat()
        {
            ViewData["Title"] = "Xatırlatma Yarat";
            return View(new XatirlatmaYaratVM
            {
                XatirlatmaTarixi = DateTime.Now.AddHours(1)
            });
        }

        // ── POST /User/Xatirlatma/Yarat ──────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yarat(XatirlatmaYaratVM vm)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            if (!ModelState.IsValid)
                return View(vm);

            var dto = new XatirlatmaCreateDto
            {
                IsciId = isciId.Value,
                Bashliq = vm.Bashliq,
                Qeyd = vm.Qeyd,
                XatirlatmaTarixi = vm.XatirlatmaTarixi
            };

            var result = await _xatirlatmaService.YaratAsync(dto);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── POST /User/Xatirlatma/Oxu ────────────────────────
        [HttpPost]
        public async Task<IActionResult> Oxu(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return Json(new { success = false });

            await _xatirlatmaService.OxunduIsareEtAsync(id, isciId.Value);
            return Json(new { success = true });
        }

        // ── POST /User/Xatirlatma/HamisiniOxu ───────────────
        [HttpPost]
        public async Task<IActionResult> HamisiniOxu()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return Json(new { success = false });

            await _xatirlatmaService.HamisiniOxunduIsareEtAsync(isciId.Value);
            return Json(new { success = true });
        }

        // ── POST /User/Xatirlatma/Sil ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _xatirlatmaService.SilAsync(id, isciId.Value);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── GET /User/Xatirlatma/OxunmamisSayi (AJAX) ───────
        [HttpGet]
        public async Task<IActionResult> OxunmamisSayi()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return Json(new { say = 0 });

            var result = await _xatirlatmaService.OxunmamisSayiAsync(isciId.Value);
            return Json(new { say = result.Success ? result.Data : 0 });
        }

        private async Task<int?> GetIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private IActionResult RedirectToLogin() =>
            RedirectToAction("Login", "Account", new { area = "" });
    }
}