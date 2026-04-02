using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public ProfileController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // ── GET /User/Profile ──────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var isci = await _unitOfWork.Repository<Isci>()
                .GetirAsync(
                    x => x.Id == isciId.Value,
                    include: q => q
                        .Include(i => i.Maliye)
                        .Include(i => i.IsciTeyinatlari)
                            .ThenInclude(t => t.Departament)
                        .Include(i => i.IsciTeyinatlari)
                            .ThenInclude(t => t.Vezife)
                        .Include(i => i.MezuniyyetBalanslari));

            if (isci == null)
            {
                TempData["Error"] = "Profil məlumatları tapılmadı.";
                return RedirectToAction("Index", "Dashboard", new { area = "User" });
            }

            return View(isci);
        }

        // ── POST /User/Profile/UpdateContact (AJAX) ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateContact(string? telefon, string? email, string? unvan)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null)
                return Json(new { success = false, message = "İstifadəçi tapılmadı." });

            var isci = await _unitOfWork.Repository<Isci>().IdIleGetirAsync(isciId.Value);
            if (isci == null)
                return Json(new { success = false, message = "İşçi məlumatları tapılmadı." });

            isci.Telefon = telefon?.Trim();
            isci.Email = email?.Trim();
            isci.Unvan = unvan?.Trim();

            await _unitOfWork.Repository<Isci>().YenileAsync(isci);
            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "Əlaqə məlumatları yeniləndi." });
        }

        // ══ Köməkçi metodlar ══════════════════════════════════

        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private IActionResult RedirectToLogin() =>
            RedirectToAction("Login", "Account", new { area = "" });
    }
}
