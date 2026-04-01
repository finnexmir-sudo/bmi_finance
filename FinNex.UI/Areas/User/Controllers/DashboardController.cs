// Areas/User/Controllers/DashboardController.cs
using FinNex.Application.DTOs.HR.IsciStrukturRolu;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Services.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.User.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Operator,Admin")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IBildirisService _bildirisService;
        private readonly IMesajService _mesajService;
        private readonly IXatirlatmaService _xatirlatmaService;
        private readonly ITapshiriqService _tapshiriqService;
        private readonly IIsciStrukturRoluService _strukturRoluService;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(
            IDashboardService dashboardService,
            IBildirisService bildirisService,
            IMesajService mesajService,
            IXatirlatmaService xatirlatmaService,
            ITapshiriqService tapshiriqService,
            IIsciStrukturRoluService strukturRoluService,
            UserManager<AppUser> userManager)
        {
            _dashboardService = dashboardService;
            _bildirisService = bildirisService;
            _mesajService = mesajService;
            _xatirlatmaService = xatirlatmaService;
            _tapshiriqService = tapshiriqService;
            _strukturRoluService = strukturRoluService;
            _userManager = userManager;
        }

        // ── GET /User/Dashboard ──────────────────────────────
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account", new { area = "" });

            var result = await _dashboardService.GetDashboardAsync(username);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(new UserDashboardViewModel());
            }

            var viewModel = UserDashboardViewModel.FromDto(result.Data!);
            ViewData["UserRole"] = viewModel.VezifeAdi;

            // İlk yükləmədə badge saylarını ViewBag-ə yaz (sonra AJAX yeniləyir)
            var isciId = result.Data!.IsciId;
            if (isciId > 0)
            {
                var badgeResult = await GetBadgeSaylariInternalAsync(isciId);
                ViewBag.BadgeSaylari = badgeResult;
            }

            return View(viewModel);
        }

        // ── GET /User/Dashboard/BadgeSaylari (AJAX, 30 san polling) ──
        // Bütün oxunmamış sayları tək endpoint-dən qaytarır.
        // Layout-dakı JS hər 30 saniyədə bir bu endpoint-i çağırır.
        [HttpGet]
        public async Task<IActionResult> BadgeSaylari()
        {
            var appUser = await _userManager.GetUserAsync(User);
            var isciId = appUser?.IsciId;

            if (isciId == null)
                return Json(BadgeSaylariDto.Bos());

            var dto = await GetBadgeSaylariInternalAsync(isciId.Value);
            return Json(dto);
        }

        // ── GET /User/Dashboard/RolBildirisler (AJAX) ────────
        // Tesdiq paneli üçün gözləyən müraciət sayları
        [HttpGet]
        public async Task<IActionResult> RolBildirisler()
        {
            var appUser = await _userManager.GetUserAsync(User);
            var isciId = appUser?.IsciId;

            if (isciId == null)
                return Json(new { sobeReisi = 0, rehber = 0, hr = 0 });

            var rolResult = await _strukturRoluService.GetByIsciIdAsync(isciId.Value);
            var roller = rolResult.Success ? rolResult.Data ?? new List<IsciStrukturRoluDto>()
                                          : new List<IsciStrukturRoluDto>();

            var sobeReisi = roller.Any(r => r.RolTipi == StrukturRolTipi.SobeReisi && r.Aktivdir) ? 1 : 0;
            var rehber = roller.Any(r => r.RolTipi == StrukturRolTipi.Rehber && r.Aktivdir) ? 1 : 0;
            var hr = roller.Any(r => r.RolTipi == StrukturRolTipi.Hr && r.Aktivdir) ? 1 : 0;

            // Burada real saylar lazımdırsa ayrıca servis metodları əlavə etmək lazımdır.
            // Hazırda rola sahib olub-olmadığını göstərir (0/1).
            return Json(new { sobeReisi, rehber, hr });
        }

        // ── Daxili köməkçi ────────────────────────────────────
        private async Task<BadgeSaylariDto> GetBadgeSaylariInternalAsync(int isciId)
        {
            var bildirisResult = await _bildirisService.OxunmamisSayiAsync(isciId);
            var mesajResult = await _mesajService.OxunmamisSayiAsync(isciId);
            var xatirlatmaResult = await _xatirlatmaService.OxunmamisSayiAsync(isciId);
            var tapshiriqResult = await _tapshiriqService.GetMenimTapshiriqlarimAsync(isciId);

            var bildiris = bildirisResult.Success ? bildirisResult.Data : 0;
            var mesaj = mesajResult.Success ? mesajResult.Data : 0;
            var xatirlatma = xatirlatmaResult.Success ? xatirlatmaResult.Data : 0;

            var gecikmisTapshiriq = tapshiriqResult.Success
                ? tapshiriqResult.Data!.Count(t =>
                    t.Status != TapshiriqStatus.Tamamlandi &&
                    t.Status != TapshiriqStatus.LegvEdildi &&
                    t.SonTarix < DateTime.Today)
                : 0;

            return new BadgeSaylariDto
            {
                Bildiris = bildiris,
                Mesaj = mesaj,
                Xatirlatma = xatirlatma,
                GecikmisTapshiriq = gecikmisTapshiriq,
                Cemi = bildiris + mesaj + xatirlatma + gecikmisTapshiriq
            };
        }
    }

    // ── DTO (yalnız bu controller-ə aiddir) ──────────────────
    public class BadgeSaylariDto
    {
        public int Bildiris { get; set; }
        public int Mesaj { get; set; }
        public int Xatirlatma { get; set; }
        public int GecikmisTapshiriq { get; set; }
        public int Cemi { get; set; }

        public static BadgeSaylariDto Bos() => new();
    }
}