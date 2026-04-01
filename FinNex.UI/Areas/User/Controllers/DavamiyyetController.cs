using FinNex.Application.DTOs.HR.Davamiyyet;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
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
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var list = await _davamiyyetService.IsciUzreAsync(isciId.Value);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyRecords(DateTime? baslangic, DateTime? son, int? status)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null)
                return Unauthorized();

            IList<DavamiyyetListDto> result;

            if (baslangic.HasValue && son.HasValue)
            {
                var all = await _davamiyyetService.AraliqUzreAsync(baslangic.Value, son.Value);
                result = all.Where(x => x.Id != 0).ToList();
                // Filter by employee - need to get by employee then filter by date
                var isciData = await _davamiyyetService.IsciUzreAsync(isciId.Value);
                result = isciData
                    .Where(x => x.Tarix.Date >= baslangic.Value.Date && x.Tarix.Date <= son.Value.Date)
                    .ToList();
            }
            else
            {
                result = await _davamiyyetService.IsciUzreAsync(isciId.Value);
            }

            if (status.HasValue)
            {
                result = result.Where(x => (int)x.Status == status.Value).ToList();
            }

            var records = result.Select(x => new
            {
                id = x.Id,
                tarix = x.Tarix,
                girisVaxti = x.GirisVaxti,
                cixisVaxti = x.CixisVaxti,
                status = (int)x.Status
            }).OrderByDescending(x => x.tarix).ToList();

            var isde = result.Count(x => x.Status == DavamiyyetStatus.Isde);
            var gecikme = result.Count(x => x.Status == DavamiyyetStatus.Gecikme);
            var qayib = result.Count(x => x.Status == DavamiyyetStatus.Qayib);
            var icazeli = result.Count(x => x.Status == DavamiyyetStatus.Icazeli);

            return Json(new
            {
                records,
                stats = new { isde, gecikme, qayib, icazeli, cemi = result.Count }
            });
        }

        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }
    }
}
