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

            var cariIl = DateTime.Today.Year;
            var all = await _davamiyyetService.IsciUzreAsync(isciId.Value);
            var list = all.Where(x => x.Tarix.Year == cariIl).ToList();

            var ip = await GetIsParametriEntity();
            ViewBag.GirisVaxti = ip.StandartGirisVaxti.ToString(@"hh\:mm");
            ViewBag.CixisVaxti = ip.StandartCixisVaxti.ToString(@"hh\:mm");
            ViewBag.GecikmeTolerans = ip.GecikmeToleransDeqiqe;
            ViewBag.TezCixmaTolerans = ip.TezCixmaToleransDeqiqe;

            try
            {
                var mezuniyyetler = await _unitOfWork.Repository<Mezuniyyet>()
                    .HamisiniGetirAsync(
                        x => x.IsciId == isciId.Value &&
                             x.BaslamaTarixi.Year == cariIl &&
                             x.Status == MezuniyyetStatus.Tesdiqlenib,
                        izlemeden: true);
                ViewBag.MezuniyyetGun = mezuniyyetler.Sum(x => x.EfektivGunSayi);
            }
            catch
            {
                ViewBag.MezuniyyetGun = 0;
            }

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyRecords(DateTime? baslangic, DateTime? son, int? status)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null)
                return Unauthorized();

            IList<DavamiyyetListDto> result;

            var allRecords = await _davamiyyetService.IsciUzreAsync(isciId.Value);

            if (baslangic.HasValue && son.HasValue)
            {
                result = allRecords
                    .Where(x => x.Tarix.Date >= baslangic.Value.Date && x.Tarix.Date <= son.Value.Date)
                    .ToList();
            }
            else
            {
                var cariIl = DateTime.Today.Year;
                result = allRecords.Where(x => x.Tarix.Year == cariIl).ToList();
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

            var ip = await GetIsParametriEntity();
            var tezCixanHeddi = ip.StandartCixisVaxti - TimeSpan.FromMinutes(ip.TezCixmaToleransDeqiqe);

            // İntizamsızlıq statistikası yalnız 01.06.2026-dan sayılır (sistemin canlı başlama tarixi)
            var intizamBaslangic = new DateTime(2026, 6, 1);
            var intizamResult = result.Where(x => x.Tarix.Date >= intizamBaslangic).ToList();

            var isde      = result.Count(x => x.Status == DavamiyyetStatus.Isde);
            var gecikme   = intizamResult.Count(x => x.Status == DavamiyyetStatus.Gecikme);
            var qayib     = intizamResult.Count(x => x.Status == DavamiyyetStatus.Qayib);
            var icazeli   = result.Count(x => x.Status == DavamiyyetStatus.Icazeli);
            var xestelik  = result.Count(x => x.Status == DavamiyyetStatus.Xestelik);
            var ezamiyyet = result.Count(x => x.Status == DavamiyyetStatus.Ezamiyyet);
            var tezCixan  = intizamResult.Count(x => x.CixisVaxti.HasValue && x.CixisVaxti.Value.TimeOfDay < tezCixanHeddi);
            var cixisYox  = intizamResult.Count(x => x.GirisVaxti.HasValue && !x.CixisVaxti.HasValue);

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
