// Areas/User/Controllers/GorushController.cs
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.User.ViewModels.Gorush;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class GorushController : Controller
    {
        private readonly IGorushService _gorushService;
        private readonly IIsciService _isciService;
        private readonly UserManager<AppUser> _userManager;

        public GorushController(
            IGorushService gorushService,
            IIsciService isciService,
            UserManager<AppUser> userManager)
        {
            _gorushService = gorushService;
            _isciService = isciService;
            _userManager = userManager;
        }

        // ── GET /User/Gorush ─────────────────────────────────
        public async Task<IActionResult> Index(string tab = "menim")
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var menim = await _gorushService.GetMenimGorushlerimAsync(isciId.Value);
            var teshkil = await _gorushService.GetTeshkilEtdiklerimAsync(isciId.Value);

            var vm = new GorushIndexVM
            {
                AktivTab = tab,
                MenimGorushler = menim.Success ? menim.Data!.ToList() : new(),
                TeshkilEtdiklerim = teshkil.Success ? teshkil.Data!.ToList() : new(),
            };

            ViewData["Title"] = "Görüşlər";
            return View(vm);
        }

        // ── GET /User/Gorush/Detay/5 ─────────────────────────
        public async Task<IActionResult> Detay(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();
            ViewBag.IsciId = isciId.Value; // ← əlavə et

            var result = await _gorushService.GetDetayAsync(id, isciId.Value);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = "Tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = result.Data.Bashliq;
            return View(result.Data);
        }


        // ── GET /User/Gorush/Yarat ───────────────────────────
        public async Task<IActionResult> Yarat()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var vm = await BuildYaratVMAsync(isciId.Value);
            ViewData["Title"] = "Görüş Yarat";
            return View(vm);
        }

        // ── POST /User/Gorush/Yarat ──────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yarat(GorushYaratVM vm)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            if (!ModelState.IsValid)
            {
                var freshVm = await BuildYaratVMAsync(isciId.Value);
                freshVm.Bashliq = vm.Bashliq;
                return View(freshVm);
            }

            var dto = new GorushCreateDto
            {
                Bashliq = vm.Bashliq,
                Agenda = vm.Agenda,
                TeshkilatciIsciId = isciId.Value,
                Tarix = vm.Tarix,
                BaslamaSaati = vm.BaslamaSaati,
                BitisSaati = vm.BitisSaati,
                Yer = vm.Yer,
                OnlineLink = null,
                Nov = GorushNovu.Offline,
                IshtirakciIsciIdler = vm.SecilmisIshtirakcilar
            };

            var result = await _gorushService.YaratAsync(dto);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index), new { tab = "teshkil" });
        }

        // ── POST /User/Gorush/QeydEleveEt ────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QeydEleveEt(int gorushId, string qeydler)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _gorushService.QeydEleveEtAsync(
                gorushId, isciId.Value, qeydler);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Detay), new { id = gorushId });
        }

        // ── POST /User/Gorush/StatusYenile ────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StatusYenile(int id,
            GorushStatus status, string? qeydler)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _gorushService.StatusYenileAsync(
                id, status, qeydler, isciId.Value);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Detay), new { id });
        }

        private async Task<GorushYaratVM> BuildYaratVMAsync(int isciId)
        {
            var iscilerResult = await _isciService.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv);

            return new GorushYaratVM
            {
                TeshkilatciIsciId = isciId,
                IsciList = iscilerResult.Success
                    ? iscilerResult.Data!
                        .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                        .ToList()
                    : new()
            };
        }

        private async Task<int?> GetIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private IActionResult RedirectToLogin() =>
            RedirectToAction("Login", "Account", new { area = "" });

        // GET /User/Gorush/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _gorushService.GetDetayAsync(id, isciId.Value);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = "Tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var g = result.Data;
            if (g.TeshkilatciIsciId != isciId.Value)
            {
                TempData["Error"] = "Yalnız təşkilatçı redaktə edə bilər.";
                return RedirectToAction(nameof(Detay), new { id });
            }

            if (g.Status == GorushStatus.Bitdi || g.Status == GorushStatus.LegvEdildi)
            {
                TempData["Error"] = "Bu görüş redaktə edilə bilməz.";
                return RedirectToAction(nameof(Detay), new { id });
            }

            var iscilerResult = await _isciService.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv);

            var secilmisIds = g.Ishtirakcılar.Select(x => x.IsciId).ToList();

            var vm = new GorushEditVM
            {
                Id = g.Id,
                Bashliq = g.Bashliq,
                Agenda = g.Agenda,
                Tarix = g.Tarix,
                BaslamaSaati = g.BaslamaSaati,
                BitisSaati = g.BitisSaati,
                Yer = g.Yer,
                SecilmisIshtirakcilar = secilmisIds,
                IsciList = iscilerResult.Success
                    ? iscilerResult.Data!
                        .Where(x => x.Id != isciId.Value)
                        .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                        .ToList()
                    : new()
            };

            ViewData["Title"] = "Görüşü Redaktə Et";
            return View(vm);
        }

        // POST /User/Gorush/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(GorushEditVM vm)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            if (!ModelState.IsValid)
            {
                var iscilerResult = await _isciService.HamisiniGetirAsync(
                    x => x.Status == IsciStatus.Aktiv);
                vm.IsciList = iscilerResult.Success
                    ? iscilerResult.Data!
                        .Where(x => x.Id != isciId.Value)
                        .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                        .ToList()
                    : new();
                return View(vm);
            }

            var dto = new GorushEditDto
            {
                Id = vm.Id,
                Bashliq = vm.Bashliq,
                Agenda = vm.Agenda,
                TeshkilatciIsciId = isciId.Value,
                Tarix = vm.Tarix,
                BaslamaSaati = vm.BaslamaSaati,
                BitisSaati = vm.BitisSaati,
                Yer = vm.Yer,
                OnlineLink = null,
                Nov = GorushNovu.Offline,
                IshtirakciIsciIdler = vm.SecilmisIshtirakcilar
            };

            var result = await _gorushService.EditAsync(dto);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Detay), new { id = vm.Id });
        }
        
    }
}