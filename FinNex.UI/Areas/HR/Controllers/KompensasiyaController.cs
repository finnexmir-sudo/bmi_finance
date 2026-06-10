using FinNex.Application.DTOs.HR.Kompensasiya;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using FinNex.UI.Areas.HR.ViewModels.Kompensasiya;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// İstifadə edilməmiş əmək məzuniyyəti günlərinə görə kompensasiya
    /// hesablama səhifəsi. HR işçi və ayrılma tarixini seçir,
    /// sistem keçmiş qalıqları + cari il prorate-i hesablayır,
    /// nəticə yadda saxlandıqda hədəf ayın maaş hesablamasında avtomatik
    /// gəlir kimi daxil olur.
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
    public class KompensasiyaController : Controller
    {
        private readonly IKompensasiyaService _service;
        private readonly IIsciService _isciService;
        private readonly UserManager<AppUser> _userManager;

        public KompensasiyaController(
            IKompensasiyaService service,
            IIsciService isciService,
            UserManager<AppUser> userManager)
        {
            _service = service;
            _isciService = isciService;
            _userManager = userManager;
        }

        // ── GET /HR/Kompensasiya ───────────────────────────────
        public async Task<IActionResult> Index()
        {
            var r = await _service.HamisiniGetirAsync();
            ViewData["Title"] = "Məzuniyyət Kompensasiyası";
            return View(r.Success ? r.Data : new List<KompensasiyaListDto>());
        }

        // ── GET /HR/Kompensasiya/Yarat ─────────────────────────
        public async Task<IActionResult> Yarat()
        {
            var vm = new KompensasiyaYaratVM();
            await IsciDropdownDoldur(vm);
            return View(vm);
        }

        // ── POST /HR/Kompensasiya/Yarat ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yarat(KompensasiyaYaratVM vm)
        {
            if (!ModelState.IsValid)
            {
                await IsciDropdownDoldur(vm);
                return View(vm);
            }

            var hesablayan = await _userManager.GetUserAsync(User);
            var hesablayanIsciId = hesablayan?.IsciId ?? 0;

            var dto = new KompensasiyaYaratDto
            {
                IsciId = vm.IsciId,
                AyrilmaTarixi = vm.AyrilmaTarixi,
                HesablananIl = vm.HesablananIl,
                HesablananAy = vm.HesablananAy,
                Qeyd = vm.Qeyd,
                ManualGunSayi = vm.ManualGunSayi
            };
            var r = await _service.YaratAsync(dto, hesablayanIsciId);
            if (!r.Success)
            {
                ModelState.AddModelError("", r.Message ?? "Yadda saxlama uğursuz.");
                await IsciDropdownDoldur(vm);
                return View(vm);
            }

            TempData["Success"] = r.Message;
            return RedirectToAction(nameof(Detal), new { id = r.Data });
        }

        // ── POST /HR/Kompensasiya/Hesabla (AJAX preview) ──────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hesabla([FromBody] HesablaSorgu req)
        {
            if (req == null || req.IsciId <= 0 || req.AyrilmaTarixi == default)
                return Json(new { error = "İşçi və ayrılma tarixi mütləqdir." });

            var r = await _service.HesablaAsync(req.IsciId, req.AyrilmaTarixi);
            if (!r.Success)
                return Json(new { error = r.Message });

            return Json(new { data = r.Data });
        }

        // ── GET /HR/Kompensasiya/Detal/{id} ──────────────────
        public async Task<IActionResult> Detal(int id)
        {
            var r = await _service.IdIleGetirAsync(id);
            if (!r.Success || r.Data == null)
            {
                TempData["Error"] = r.Message ?? "Qeyd tapılmadı.";
                return RedirectToAction(nameof(Index));
            }
            return View(r.Data);
        }

        // ── POST /HR/Kompensasiya/LegvEt/{id} ─────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LegvEt(int id)
        {
            var r = await _service.LegvEtAsync(id);
            TempData[r.Success ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Index));
        }

        // ──────────────────────────────────────────────────────
        private async Task IsciDropdownDoldur(KompensasiyaYaratVM vm)
        {
            var iscilerR = await _isciService.HamisiniGetirAsync();
            var isciler = iscilerR.Success && iscilerR.Data != null
                ? iscilerR.Data : new List<FinNex.Application.DTOs.HR.Isci.IsciListDto>();

            vm.Isciler = isciler
                .OrderBy(x => x.Sira).ThenBy(x => x.TamAd)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.TamAd
                          + (x.IsdenAyrilmaTarixi.HasValue
                              ? $" — işdən çıxıb {x.IsdenAyrilmaTarixi:dd.MM.yyyy}"
                              : "")
                })
                .ToList();
        }

        public class HesablaSorgu
        {
            public int IsciId { get; set; }
            public DateTime AyrilmaTarixi { get; set; }
        }
    }
}
