using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.HR.ViewModels.Mezuniyyet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
    public class XestelikEzamiyyetController : Controller
    {
        private readonly IMezuniyyetService _mezuniyyetService;
        private readonly IIsciService _isciService;
        private readonly UserManager<AppUser> _userManager;

        public XestelikEzamiyyetController(
            IMezuniyyetService mezuniyyetService,
            IIsciService isciService,
            UserManager<AppUser> userManager)
        {
            _mezuniyyetService = mezuniyyetService;
            _isciService = isciService;
            _userManager = userManager;
        }

        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        // ══════════════════════════════════════════════════════
        // SİYAHI
        // ══════════════════════════════════════════════════════

        public async Task<IActionResult> Index()
        {
            var result = await _mezuniyyetService.GetListAsync();
            // Qeyd: Xəstəlik artıq ayrıca cədvəldədir (HR/Xestelik). Bu səhifə yalnız ezamiyyət göstərir.
            var list = result.Success
                ? result.Data!
                    .Where(x => x.Nov == MezuniyyetNovu.Ezamiyyet)
                    .OrderByDescending(x => x.BaslamaTarixi)
                    .ToList()
                : new();

            ViewData["Title"] = "Ezamiyyət";
            return View(list);
        }

        // ══════════════════════════════════════════════════════
        // YARAT
        // ══════════════════════════════════════════════════════

        public async Task<IActionResult> Create()
        {
            await FillCreateViewBagsAsync();
            var today = DateTime.Today;
            return View(new MezuniyyetCreateDto
            {
                Nov = MezuniyyetNovu.Ezamiyyet,
                BaslamaTarixi = today,
                BitmeTarixi = today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MezuniyyetCreateDto dto)
        {
            var hrIsciId = await GetCurrentIsciIdAsync();
            if (hrIsciId == null) return Forbid();

            // Bu səhifə yalnız Ezamiyyət üçündür (Xestelik ayrı cədvəldədir)
            dto.Nov = MezuniyyetNovu.Ezamiyyet;

            if (dto.BitmeTarixi < dto.BaslamaTarixi)
                ModelState.AddModelError("BitmeTarixi", "Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");

            if (dto.IsciId <= 0)
                ModelState.AddModelError("IsciId", "İşçi seçilməlidir.");

            if (!ModelState.IsValid)
            {
                await FillCreateViewBagsAsync();
                return View(dto);
            }

            dto.EvezEdenIsciId = null;

            var result = await _mezuniyyetService.YaratAsync(dto);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                await FillCreateViewBagsAsync();
                return View(dto);
            }

            var approveResult = await _mezuniyyetService.HrTesdiqAsync(
                result.Data!.Id, true, "HR tərəfindən yaradılıb", hrIsciId.Value);

            TempData[approveResult.Success ? "Success" : "Error"] = approveResult.Success
                ? "Qeyd uğurla yaradıldı və təsdiqləndi."
                : approveResult.Message;

            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════
        // DETAL
        // ══════════════════════════════════════════════════════

        public async Task<IActionResult> Detal(int id)
        {
            var result = await _mezuniyyetService.IdIleGetirAsync(id);
            if (!result.Success || result.Data == null) return NotFound();

            var vm = new HrMezuniyyetDetalVM
            {
                Mezuniyyet = result.Data,
                ReturnAction = "Index"
            };

            ViewData["Title"] = "Qeyd Detalı";
            return View(vm);
        }

        // ══════════════════════════════════════════════════════
        // REDAKTƏ
        // ══════════════════════════════════════════════════════

        public async Task<IActionResult> Edit(int id)
        {
            var result = await _mezuniyyetService.IdIleGetirAsync(id);
            if (!result.Success || result.Data == null) return NotFound();

            var dto = result.Data;
            var updateDto = new MezuniyyetUpdateDto
            {
                Id = dto.Id,
                BaslamaTarixi = dto.BaslamaTarixi,
                BitmeTarixi = dto.BitmeTarixi,
                EvezEdenIsciId = dto.EvezEdenIsciId,
                Nov = dto.Nov,
                Qeyd = dto.Qeyd,
                Status = dto.Status,
                ImtinaSebebi = dto.ImtinaSebebi
            };

            await FillEditViewBagsAsync(dto.IsciId);
            return View(updateDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MezuniyyetUpdateDto dto)
        {
            if (dto.BitmeTarixi < dto.BaslamaTarixi)
                ModelState.AddModelError("BitmeTarixi", "Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");

            if (!ModelState.IsValid)
            {
                await FillEditViewBagsAsync();
                return View(dto);
            }

            var result = await _mezuniyyetService.YenileAsync(dto);

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════
        // SİL
        // ══════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _mezuniyyetService.SilAsync(id);

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════
        // KÖMƏKÇİ METODLAR
        // ══════════════════════════════════════════════════════

        private async Task FillCreateViewBagsAsync()
        {
            var iscilerResult = await _isciService.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv, izlemeden: true);

            var items = iscilerResult.Success
                ? iscilerResult.Data!.OrderBy(x => x.Sira).ThenBy(x => x.TamAd)
                    .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                    .ToList()
                : new List<SelectListItem>();

            ViewBag.IsciList = items;
        }

        private async Task FillEditViewBagsAsync(int? isciId = null)
        {
            var iscilerResult = await _isciService.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv, izlemeden: true);

            ViewBag.EvezEdenIsciList = iscilerResult.Success
                ? iscilerResult.Data!
                    .Where(x => isciId == null || x.Id != isciId)
                    .OrderBy(x => x.Sira).ThenBy(x => x.TamAd)
                    .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                    .ToList()
                : new List<SelectListItem>();
        }
    }
}
