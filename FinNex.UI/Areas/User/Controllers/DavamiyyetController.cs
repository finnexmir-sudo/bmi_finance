using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class DavamiyyetController : Controller
    {
        private readonly IDavamiyyetService _davamiyyetService;
        private readonly UserManager<AppUser> _userManager;

        public DavamiyyetController(
            IDavamiyyetService davamiyyetService,
            UserManager<AppUser> userManager)
        {
            _davamiyyetService = davamiyyetService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var isciId = appUser.IsciId.Value;
            var list = await _davamiyyetService.IsciUzreAsync(isciId);

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetByTarix(DateTime? baslangic, DateTime? son)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
                return Unauthorized();

            var isciId = appUser.IsciId.Value;

            if (baslangic.HasValue && son.HasValue)
            {
                var userRecords = (await _davamiyyetService.IsciUzreAsync(isciId))
                    .Where(x => x.Tarix.Date >= baslangic.Value.Date && x.Tarix.Date <= son.Value.Date)
                    .ToList();
                return Json(userRecords);
            }

            var result = await _davamiyyetService.IsciUzreAsync(isciId);
            return Json(result);
        }
    }
}
