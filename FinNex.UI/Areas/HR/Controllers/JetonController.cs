using FinNex.Application.DTOs.HR.Jeton;
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
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class JetonController : Controller
    {
        private readonly IJetonService _jetonService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public JetonController(IJetonService jetonService, IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _jetonService = jetonService;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // ── GET /HR/Jeton ────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var teyinatlar = await _jetonService.JetonTeyinatlariGetirAsync();
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .OrderBy(x => x.Sira).ThenBy(x => x.Ad).ThenBy(x => x.Soyad)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Ad + " " + x.Soyad })
                .ToListAsync();

            ViewBag.Teyinatlar = teyinatlar;
            ViewBag.Isciler = isciler;
            ViewData["Title"] = "Jeton İdarəetməsi";
            return View();
        }

        // ── GET /HR/Jeton/GetEmeliyyatlar ────────────────────
        [HttpGet]
        public async Task<IActionResult> GetEmeliyyatlar(int? isciId)
        {
            var list = await _jetonService.JetonEmeliyyatlariGetirAsync(isciId);
            return Json(new { success = true, data = list });
        }

        // ── POST /HR/Jeton/JetonVer ──────────────────────────
        [HttpPost]
        public async Task<IActionResult> JetonVer([FromBody] IsciJetonuCreateDto dto)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return Json(new { success = false, message = "İstifadəçi tapılmadı." });

            var result = await _jetonService.JetonVerAsync(dto, appUser.Id);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── POST /HR/Jeton/JetonLegvet ───────────────────────
        [HttpPost]
        public async Task<IActionResult> JetonLegvet([FromBody] JetonLegvetRequestDto dto)
        {
            var result = await _jetonService.JetonLegvetAsync(dto.IsciJetonuId, dto.Sebeb);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── GET /HR/Jeton/GetRedimler ────────────────────────
        // Rehber → öz departamentinin baxılmamış sorğuları (rəhbər mərhələsi)
        // HR/Admin → rəhbər təsdiqindən keçmiş sorğular (HR mərhələsi)
        // Eyni şəxs həm rəhbər, həm admin/HR ola bilər → hər iki mərhələni görür (təsdiq per-sətir yönlənir).
        [HttpGet]
        public async Task<IActionResult> GetRedimler()
        {
            var isRehber = User.IsInRole(RoleNames.Rehber);
            var isAdmin  = User.IsInRole(RoleNames.Admin);
            var isHr     = User.IsInRole(RoleNames.HR);

            bool asRehber  = isRehber || isAdmin;   // rəhbər təsdiqi edə bilənlər
            bool asHrAdmin = isHr || isAdmin;       // HR final təsdiqi edə bilənlər
            // Admin bütün departamentləri görür; təmiz rəhbər yalnız öz departamentini
            int? departamentId = (isRehber && !isAdmin) ? await GetRehberDepartamentIdAsync() : null;

            var list = await _jetonService.GozleyenRedimlerGetirAsync(departamentId, asRehber, asHrAdmin);
            return Json(new { success = true, data = list });
        }

        // ── POST /HR/Jeton/RehberTesdiq ──────────────────────
        [HttpPost]
        [Authorize(Roles = RoleNames.Rehber + "," + RoleNames.Admin)]
        public async Task<IActionResult> RehberTesdiq([FromBody] RedimTesdiqRequestDto dto)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null) return Json(new { success = false, message = "İstifadəçi tapılmadı." });

            var result = await _jetonService.RedimRehberTesdiqleAsync(dto.RedimId, appUser.Id);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── POST /HR/Jeton/RehberRedd ────────────────────────
        [HttpPost]
        [Authorize(Roles = RoleNames.Rehber + "," + RoleNames.Admin)]
        public async Task<IActionResult> RehberRedd([FromBody] RedimReddRequestDto dto)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null) return Json(new { success = false, message = "İstifadəçi tapılmadı." });

            var result = await _jetonService.RedimRehberReddEtAsync(dto.RedimId, dto.Qeyd, appUser.Id);
            return Json(new { success = result.Success, message = result.Message });
        }

        // Rəhbər (yalnız Rəhbər rolu, HR/Admin deyil) üçün öz departamentini
        // tapır. HR/Admin null qaytarır → hamısını görür.
        private async Task<int?> GetRehberDepartamentIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return null;

            var teyinat = await _unitOfWork.Repository<IsciTeyinat>()
                .Query()
                .Where(t => t.IsciId == appUser.IsciId.Value && t.Aktivdir)
                .FirstOrDefaultAsync();

            return teyinat?.DepartamentId;
        }

        // ── GET /HR/Jeton/GetRedimTarixcesi ─────────────────
        [HttpGet]
        public async Task<IActionResult> GetRedimTarixcesi()
        {
            var list = await _jetonService.ButunRedimlerTarixcesiAsync();
            return Json(new { success = true, data = list });
        }

        // ── GET /HR/Jeton/GetIsciJetonlar ────────────────────
        [HttpGet]
        public async Task<IActionResult> GetIsciJetonlar(int isciId)
        {
            var list = await _jetonService.IsciAktivJetonlariniGetirAsync(isciId);
            var balans = await _jetonService.AktivSaatBalansiAsync(isciId);
            var qaraVar = await _jetonService.AktivQaraJetonuVarmiAsync(isciId);
            return Json(new { success = true, data = list, balans, qaraVar });
        }

        // ── POST /HR/Jeton/TesdiqleRedim ─────────────────────
        [HttpPost]
        public async Task<IActionResult> TesdiqleRedim([FromBody] RedimTesdiqRequestDto dto)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return Json(new { success = false, message = "İstifadəçi tapılmadı." });

            var result = await _jetonService.RedimTelebiTesdiqleAsync(dto.RedimId, appUser.Id);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── POST /HR/Jeton/ReddEtRedim ───────────────────────
        [HttpPost]
        public async Task<IActionResult> ReddEtRedim([FromBody] RedimReddRequestDto dto)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return Json(new { success = false, message = "İstifadəçi tapılmadı." });

            var result = await _jetonService.RedimTelebiReddEtAsync(dto.RedimId, dto.Qeyd, appUser.Id);
            return Json(new { success = result.Success, message = result.Message });
        }
    }

    // ── Request DTOs (local, controller-only) ─────────────
    public class JetonLegvetRequestDto
    {
        public int IsciJetonuId { get; set; }
        public string Sebeb { get; set; } = "";
    }

    public class RedimTesdiqRequestDto
    {
        public int RedimId { get; set; }
    }

    public class RedimReddRequestDto
    {
        public int RedimId { get; set; }
        public string Qeyd { get; set; } = "";
    }
}
