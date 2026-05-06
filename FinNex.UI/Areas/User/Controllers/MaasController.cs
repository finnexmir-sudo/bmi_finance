using FinNex.Application.DTOs.HR.Maas;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class MaasController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public MaasController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // ── GET /User/Maas (köhnə bildiriş linklər üçün) ───────
        public IActionResult Index() => RedirectToAction(nameof(Tarixce));

        // ── GET /User/Maas/Tarixce ──────────────────────────
        public IActionResult Tarixce()
        {
            ViewData["Title"] = "Əmək haqqı tarixçəsi";
            return View();
        }

        // ── GET /User/Maas/GetTarixceData ───────────────────
        [HttpGet]
        public async Task<IActionResult> GetTarixceData()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
                return Json(new { success = false, message = "İşçi tapılmadı." });

            var isciId = appUser.IsciId.Value;

            // Son 12 ay
            var now = DateTime.Now;
            var son12Ay = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
            var sonIl = son12Ay.Year;
            var sonAy = son12Ay.Month;

            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => !x.Silinib &&
                            x.IsciId == isciId &&
                            (x.Il > sonIl || (x.Il == sonIl && x.Ay >= sonAy)))
                .OrderBy(x => x.Il)
                .ThenBy(x => x.Ay)
                .ToListAsync();

            var ayAdlar = new[]
            {
                "", "Yan", "Fev", "Mar", "Apr", "May", "İyn",
                "İyl", "Avq", "Sen", "Okt", "Noy", "Dek"
            };

            var data = maaslar.Select(m => new
            {
                etiket = ayAdlar[m.Ay] + " " + m.Il,
                brut = m.BrutMebleg,
                net = m.NetMebleg,
                il = m.Il,
                ay = m.Ay
            }).ToList();

            return Json(new { success = true, data });
        }

        // ── GET /User/Maas/GetDetay?il=2026&ay=4 ────────────
        [HttpGet]
        public async Task<IActionResult> GetDetay(int il, int ay)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
                return Json(new { success = false });

            var maas = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => !x.Silinib && x.IsciId == appUser.IsciId.Value && x.Il == il && x.Ay == ay)
                .FirstOrDefaultAsync();

            if (maas == null)
                return Json(new { success = false, message = "Məlumat tapılmadı." });

            var addimlar = string.IsNullOrEmpty(maas.HesablamaIzahi)
                ? new List<object>()
                : (JsonSerializer.Deserialize<List<HesablamaIzahiDto>>(maas.HesablamaIzahi)
                    ?? new List<HesablamaIzahiDto>())
                    .Select(x => (object)new { addim = x.Addim, izah = x.Izah, mebleg = x.Mebleg, tip = x.Tip })
                    .ToList();

            return Json(new { success = true, il, ay, brut = maas.BrutMebleg, net = maas.NetMebleg, addimlar });
        }

        // ── GET /User/Maas/HYS ─────────────────────────────────
        // İşçi öz HYS təyinatlarını görür (read-only)
        public async Task<IActionResult> HYS()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
            {
                TempData["Error"] = "İşçi məlumatı tapılmadı.";
                return RedirectToAction("Index", "Dashboard");
            }

            var isciId = appUser.IsciId.Value;
            var today = DateTime.Today;

            var hysList = await _unitOfWork.Repository<IsciHYS>()
                .Query()
                .Where(x => !x.Silinib && x.IsciId == isciId)
                .OrderByDescending(x => x.BaslamaTarixi)
                .ToListAsync();

            ViewData["Title"] = "Həyat Yığım Sığortası (HYS)";
            return View(hysList);
        }
    }
}
