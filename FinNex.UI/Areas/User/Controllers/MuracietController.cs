
using FinNex.Application.Interfaces;
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
    [Authorize(Roles = "Operator")]
    public class MuracietController : Controller
    {
        private readonly IMezuniyyetService _mezuniyyetService;
        private readonly IIcazeService _icazeService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public MuracietController(
            IMezuniyyetService mezuniyyetService,
            IIcazeService icazeService,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _mezuniyyetService = mezuniyyetService;
            _icazeService = icazeService;
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

            var model = new MuracietPortalViewModel
            {
                AktivTab = tab,
                Mezuniyyetler = mezResult.Success ? mezResult.Data!.ToList() : new(),
                Icazeler = icazeResult.Success ? icazeResult.Data!.ToList() : new(),
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