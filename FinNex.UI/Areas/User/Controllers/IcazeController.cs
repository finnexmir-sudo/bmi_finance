using FinNex.Application.DTOs.HR.Icaze;
using FinNex.Application.Interfaces;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.User.ViewModels.Icaze;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Operator")]
    public class IcazeController : Controller
    {
        private readonly IIcazeService _icazeService;
        private readonly IIsciService _isciService;
        private readonly UserManager<AppUser> _userManager;

        public IcazeController(
            IIcazeService icazeService,
            IIsciService isciService,
            UserManager<AppUser> userManager)
        {
            _icazeService = icazeService;
            _isciService = isciService;
            _userManager = userManager;
        }

        // ── GET /User/Icaze ────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _icazeService.GetIsciIcazeleriAsync(isciId.Value);

            var vm = new IcazeIndexVM
            {
                Icazeler = result.Success ? result.Data!.ToList() : new()
            };

            if (!result.Success)
                TempData["Error"] = result.Message;

            return View(vm);
        }

        // ── GET /User/Icaze/Create ─────────────────────────────
        public async Task<IActionResult> Create()
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var vm = new IcazeCreateVM
            {
                IsciId = isciId.Value,
                IcazeTarixi = DateTime.Today,
                BaslamaSaati = new TimeSpan(9, 0, 0),
                BitisSaati = new TimeSpan(11, 0, 0),
                EvezEdenList = await BuildEvezEdenListAsync(isciId.Value),
            };

            return View(vm);
        }

        // ── POST /User/Icaze/Create ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IcazeCreateVM vm)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            vm.IsciId = isciId.Value;

            if (vm.BitisSaati <= vm.BaslamaSaati)
                ModelState.AddModelError("BitisSaati", "Bitmə saatı başlama saatından sonra olmalıdır.");

            if (!ModelState.IsValid)
            {
                vm.EvezEdenList = await BuildEvezEdenListAsync(isciId.Value);
                return View(vm);
            }

            var createDto = new IcazeCreateDto
            {
                IsciId = isciId.Value,
                EvezEdenIsciId = vm.EvezEdenIsciId ?? vm.IsciId,
                IcazeTarixi = vm.IcazeTarixi,
                BaslamaSaati = vm.BaslamaSaati,
                BitisSaati = vm.BitisSaati,
                Sebeb = vm.Sebeb,
            };

            var result = await _icazeService.YaratAsync(createDto);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                vm.EvezEdenList = await BuildEvezEdenListAsync(isciId.Value);
                return View(vm);
            }

            TempData["Success"] = result.Message ?? "Müraciət uğurla göndərildi.";
            return RedirectToAction(nameof(Index));
        }

        // ── GET /User/Icaze/Detail/5 ───────────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _icazeService.GetDetayAsync(id);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = "İcazə tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            return View(result.Data!);
        }

        // ── POST /User/Icaze/Legv ──────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Legv(int id)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _icazeService.LegvEtAsync(id, isciId.Value);

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ══ Köməkçi metodlar ══════════════════════════════════

        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private async Task<List<SelectListItem>> BuildEvezEdenListAsync(int isciId)
        {
            var result = await _isciService.HamisiniGetirAsync(
                x => x.Id != isciId && x.Status == IsciStatus.Aktiv,
                izlemeden: true);

            return result.Success
                ? result.Data!
                    .OrderBy(x => x.TamAd)
                    .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                    .ToList()
                : new();
        }

        private IActionResult RedirectToLogin() =>
            RedirectToAction("Login", "Account", new { area = "" });
    }
}
