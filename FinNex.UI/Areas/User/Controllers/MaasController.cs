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
    [Authorize(Roles = RoleNames.Operator + "," + RoleNames.Admin)]
    public class MaasController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public MaasController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

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

            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => !x.Silinib &&
                            x.IsciId == isciId &&
                            new DateTime(x.Il, x.Ay, 1) >= son12Ay)
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
                net = m.NetMebleg
            }).ToList();

            return Json(new { success = true, data });
        }
    }
}
