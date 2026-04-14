using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
    public class XestelikController : Controller
    {
        private readonly IXestelikService _xestelikService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public XestelikController(
            IXestelikService xestelikService,
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager)
        {
            _xestelikService = xestelikService;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // ── GET /HR/Xestelik ─────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var result = await _xestelikService.GetListAsync();
            ViewBag.List = result.Success ? result.Data : new List<XestelikDto>();
            ViewData["Title"] = "Xəstəlik Bülletənləri";
            return View();
        }

        // ── GET /HR/Xestelik/Create ──────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await DoldurIsciSiyahisiAsync();
            ViewData["Title"] = "Yeni Xəstəlik Bülletəni";
            return View(new XestelikCreateDto
            {
                BaslamaTarixi = DateTime.Today,
                BitmeTarixi = DateTime.Today
            });
        }

        // ── POST /HR/Xestelik/Create ─────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(XestelikCreateDto input)
        {
            if (!ModelState.IsValid)
            {
                await DoldurIsciSiyahisiAsync();
                return View(input);
            }

            var hrUser = await _userManager.GetUserAsync(User);
            var result = await _xestelikService.CreateAsync(input, hrUser?.IsciId);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                await DoldurIsciSiyahisiAsync();
                return View(input);
            }

            TempData["Success"] = "Xəstəlik bülletəni uğurla əlavə edildi.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /HR/Xestelik/Delete ─────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _xestelikService.DeleteAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── GET /HR/Xestelik/Preview?isciId=5&baslama=...&bitme=... ──
        // Real-time preview üçün AJAX endpoint
        [HttpGet]
        public async Task<IActionResult> Preview(int isciId, DateTime baslama, DateTime bitme)
        {
            if (isciId <= 0)
                return Json(new { success = false, message = "İşçi seçilməyib." });

            var result = await _xestelikService.PreviewAsync(isciId, baslama, bitme);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new { success = true, data = result.Data });
        }

        private async Task DoldurIsciSiyahisiAsync()
        {
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib)
                .OrderBy(x => x.Soyad).ThenBy(x => x.Ad)
                .Select(x => new { x.Id, x.Ad, x.Soyad })
                .ToListAsync();

            ViewBag.Isciler = isciler.Select(x =>
                new SelectListItem($"{x.Soyad} {x.Ad}", x.Id.ToString())).ToList();
        }
    }
}
