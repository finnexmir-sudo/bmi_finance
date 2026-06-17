using FinNex.Application.DTOs.HR.DigerTutulma;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using FinNex.UI.Areas.HR.ViewModels.DigerTutulma;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// İşçidən "digər tutulma" (məs. xəstəlik sığortası payı) — işçi və
    /// məbləğ + müddət daxil edilir, sistem aylıq paya bölür. Maaşa təsir
    /// etmir; yalnız provodkada net ödənişdən aylıq pay çıxılır.
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.Muhasib + "," + RoleNames.Admin + "," + RoleNames.HR)]
    public class DigerTutulmaController : Controller
    {
        private readonly IDigerTutulmaService _service;
        private readonly IIsciService _isciService;
        private readonly UserManager<AppUser> _userManager;

        public DigerTutulmaController(
            IDigerTutulmaService service,
            IIsciService isciService,
            UserManager<AppUser> userManager)
        {
            _service = service;
            _isciService = isciService;
            _userManager = userManager;
        }

        // ── GET /HR/DigerTutulma ───────────────────────────────
        public async Task<IActionResult> Index()
        {
            var r = await _service.HamisiniGetirAsync();
            ViewData["Title"] = "İşçidən Digər Tutulma";
            return View(r.Success ? r.Data : new List<DigerTutulmaListDto>());
        }

        // ── GET /HR/DigerTutulma/Yarat ─────────────────────────
        public async Task<IActionResult> Yarat()
        {
            var vm = new DigerTutulmaYaratVM();
            await IsciDropdownDoldur(vm);
            return View(vm);
        }

        // ── POST /HR/DigerTutulma/Yarat ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yarat(DigerTutulmaYaratVM vm)
        {
            if (!ModelState.IsValid)
            {
                await IsciDropdownDoldur(vm);
                return View(vm);
            }

            var istifadeci = await _userManager.GetUserAsync(User);
            var yaradanIsciId = istifadeci?.IsciId ?? 0;

            var dto = new DigerTutulmaYaratDto
            {
                IsciId = vm.IsciId,
                Mebleg = vm.Mebleg,
                MuddetAy = vm.MuddetAy,
                BaslamaIl = vm.BaslamaIl,
                BaslamaAy = vm.BaslamaAy,
                Aciqlama = vm.Aciqlama
            };

            var r = await _service.YaratAsync(dto, yaradanIsciId);
            if (!r.Success)
            {
                ModelState.AddModelError("", r.Message ?? "Yadda saxlama uğursuz oldu.");
                await IsciDropdownDoldur(vm);
                return View(vm);
            }

            TempData["Success"] = r.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── POST /HR/DigerTutulma/Sil/{id} ─────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            var r = await _service.LegvEtAsync(id);
            TempData[r.Success ? "Success" : "Error"] = r.Success ? "Qeyd silindi." : r.Message;
            return RedirectToAction(nameof(Index));
        }

        private async Task IsciDropdownDoldur(DigerTutulmaYaratVM vm)
        {
            var iscilerR = await _isciService.HamisiniGetirAsync();
            var isciler = iscilerR.Success && iscilerR.Data != null
                ? iscilerR.Data
                : new List<FinNex.Application.DTOs.HR.Isci.IsciListDto>();

            vm.Isciler = isciler
                .OrderBy(x => x.Sira).ThenBy(x => x.TamAd)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.TamAd })
                .ToList();
        }
    }
}
