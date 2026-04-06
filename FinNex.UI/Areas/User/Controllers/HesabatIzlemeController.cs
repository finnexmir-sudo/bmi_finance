using FinNex.Application.DTOs.HR.HesabatIzleme;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class HesabatIzlemeController : Controller
    {
        private readonly IHesabatIzlemeService _service;
        private readonly IUnitOfWork _uow;
        private readonly UserManager<AppUser> _userManager;

        public HesabatIzlemeController(
            IHesabatIzlemeService service,
            IUnitOfWork uow,
            UserManager<AppUser> userManager)
        {
            _service = service;
            _uow = uow;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var departamentId = await GetUserDepartamentIdAsync();

            // Əvvəlcə bu dövrün tapşırıqlarını yarat və gecikənləri yoxla
            await _service.DovrTapshiriqlariniYaratAsync();
            await _service.GecikenleriYoxlaAsync();

            var result = await _service.GetDashboardAsync(departamentId);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(new HesabatIzlemeDashboardDto());
            }

            // Departament siyahısı (filtr üçün)
            ViewBag.Departamentler = await _uow.Repository<Departament>()
                .Query()
                .Where(x => !x.Silinib)
                .OrderBy(x => x.Ad)
                .Select(x => new { x.Id, x.Ad })
                .ToListAsync();

            // İşçi siyahısı (şablon yaratmaq üçün)
            ViewBag.Isciler = await _uow.Repository<Isci>()
                .Query()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .OrderBy(x => x.Ad)
                .Select(x => new { x.Id, TamAd = x.Ad + " " + x.Soyad })
                .ToListAsync();

            ViewBag.UserDeptId = departamentId;
            ViewData["Title"] = "Hesabat İzləmə";
            ViewData["UserRole"] = "Mühasib";
            return View(result.Data);
        }

        // ── Şablon Yarat ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SablonYarat(HesabatSablonCreateDto dto)
        {
            var result = await _service.SablonYaratAsync(dto);
            if (result.Success)
            {
                // Yeni şablon üçün dərhal tapşırıq yarat
                await _service.DovrTapshiriqlariniYaratAsync();
                TempData["Success"] = "Hesabat şablonu yaradıldı.";
            }
            else
                TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        // ── Şablon Sil ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SablonSil(int id)
        {
            var result = await _service.SablonSilAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── Tamamla ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tamamla(int tapshiriqId, string? qeyd)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null)
            {
                TempData["Error"] = "İşçi tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _service.TamamlaAsync(tapshiriqId, isciId.Value, qeyd);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── İcraya Başla ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IcrayaBasla(int tapshiriqId)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null)
            {
                TempData["Error"] = "İşçi tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _service.IcrayaBaslaAsync(tapshiriqId, isciId.Value);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ──
        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private async Task<int?> GetUserDepartamentIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return null;

            var teyinat = await _uow.Repository<IsciTeyinat>()
                .Query()
                .Where(x => x.IsciId == appUser.IsciId && x.Aktivdir && !x.Silinib)
                .Select(x => x.DepartamentId)
                .FirstOrDefaultAsync();

            return teyinat > 0 ? teyinat : null;
        }
    }
}
