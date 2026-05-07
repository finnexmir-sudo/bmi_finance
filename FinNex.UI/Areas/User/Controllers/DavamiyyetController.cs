using FinNex.Application.DTOs.HR.Davamiyyet;
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
    public class DavamiyyetController : Controller
    {
        private readonly IDavamiyyetService _davamiyyetService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public DavamiyyetController(
            IDavamiyyetService davamiyyetService,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _davamiyyetService = davamiyyetService;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var list = await _davamiyyetService.IsciUzreAsync(isciId.Value);

            var ip = await GetIsParametriEntity();
            ViewBag.GirisVaxti = ip.GirisVaxti ?? "09:00";
            ViewBag.CixisVaxti = ip.CixisVaxti ?? "17:45";
            ViewBag.GecikmeTolerans = ip.GecikmeTolerans > 0 ? ip.GecikmeTolerans : 5;
            ViewBag.TezCixmaTolerans = ip.TezCixmaTolerans > 0 ? ip.TezCixmaTolerans : 15;

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
            var xestelik = result.Count(x => x.Status == DavamiyyetStatus.Xestelik);
            var ezamiyyet = result.Count(x => x.Status == DavamiyyetStatus.Ezamiyyet);

            var ip = await GetIsParametriEntity();
            int cixisThresholdMin = 17 * 60 + 45;
            try
            {
                var parts = (ip.CixisVaxti ?? "17:45").Split(':');
                cixisThresholdMin = int.Parse(parts[0]) * 60 + int.Parse(parts[1]);
            }
            catch { }
            var tezCixanHedd = cixisThresholdMin - (ip.TezCixmaTolerans > 0 ? ip.TezCixmaTolerans : 15);
            var tezCixan = result.Count(x => x.CixisVaxti.HasValue &&
                (x.CixisVaxti.Value.Hour * 60 + x.CixisVaxti.Value.Minute) < tezCixanHedd);
            var cixisYox = result.Count(x => x.GirisVaxti.HasValue && !x.CixisVaxti.HasValue);

            return Json(new
            {
                records,
                stats = new { isde, gecikme, qayib, icazeli, xestelik, ezamiyyet, tezCixan, cixisYox, cemi = result.Count }
            });
        }

        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private async Task<IsParametri> GetIsParametriEntity()
        {
            try
            {
                var entity = await _unitOfWork.Repository<IsParametri>()
                    .Query().AsNoTracking().Where(x => !x.Silinib).FirstOrDefaultAsync();
                return entity ?? new IsParametri();
            }
            catch { return new IsParametri(); }
        }
    }
}
