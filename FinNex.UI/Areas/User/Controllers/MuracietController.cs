
using FinNex.Application.Interfaces;
using FinNex.Application.Services.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.User.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = RoleNames.Operator)]
    public class MuracietController : Controller
    {
        private readonly IMezuniyyetService _mezuniyyetService;
        private readonly IIcazeService _icazeService;
        private readonly IEzamiyyetService _ezamiyyetService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public MuracietController(
            IMezuniyyetService mezuniyyetService,
            IIcazeService icazeService,
            IEzamiyyetService ezamiyyetService,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _mezuniyyetService = mezuniyyetService;
            _icazeService = icazeService;
            _ezamiyyetService = ezamiyyetService;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        // GET /User/Muraciet/Index?tab=mezuniyyet
        public async Task<IActionResult> Index(string tab = "mezuniyyet")
        {
            var isci = await GetCurrentIsciAsync();
            if (isci == null) return RedirectToLogin();

            var mezResult = await _mezuniyyetService.GetIsciMezuniyyetleriAsync(isci.Id);
            var icazeResult = await _icazeService.GetIsciIcazeleriAsync(isci.Id);
            var ezamList = await _ezamiyyetService.IsciMuracietleriAsync(isci.Id);

            var rehberdirmi = User.IsInRole(RoleNames.Rehber);
            var sobeReisidirmi = User.IsInRole(RoleNames.SobeReisi);
            var mezList = mezResult.Success ? mezResult.Data!.ToList() : new();
            var icazeList = icazeResult.Success ? icazeResult.Data!.ToList() : new();
            foreach (var m in mezList) { m.MuracietSahibiRehberdirmi = rehberdirmi; m.MuracietSahibiSobeReisidirmi = sobeReisidirmi; }
            foreach (var ic in icazeList) { ic.MuracietSahibiRehberdirmi = rehberdirmi; ic.MuracietSahibiSobeReisidirmi = sobeReisidirmi; }

            var model = new MuracietPortalViewModel
            {
                AktivTab = tab,
                Mezuniyyetler = mezList,
                Icazeler = icazeList,
                Ezamiyyetler = ezamList?.ToList() ?? new(),
            };

            return View(model);
        }

        // ── Köməkçi: cari işçini tap ─────────────────────────
        private async Task<Isci?> GetCurrentIsciAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return null;

            return await _unitOfWork.Repository<Isci>()
                .GetirAsync(x => x.Id == appUser.IsciId, izlemeden: true);
        }

        private IActionResult RedirectToLogin() =>
            RedirectToAction("Login", "Account", new { area = "" });
    }
}