using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = "HR,Admin")]
    public class DavamiyyetController : Controller
    {
        private readonly IDavamiyyetService _davamiyyetService;
        private readonly IIsciService _isciService;

        public DavamiyyetController(
            IDavamiyyetService davamiyyetService,
            IIsciService isciService)
        {
            _davamiyyetService = davamiyyetService;
            _isciService = isciService;
        }

        public async Task<IActionResult> Index()
        {
            var bugun = DateTime.Today;
            var list = await _davamiyyetService.TarixUzreAsync(bugun);

            ViewBag.BugunList = list;
            ViewBag.BugunTarix = bugun;

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetByTarix(DateTime? tarix, DateTime? baslangic, DateTime? son, int? isciId, int? status)
        {
            IList<Application.DTOs.HR.Davamiyyet.DavamiyyetListDto> result;

            if (isciId.HasValue)
            {
                result = await _davamiyyetService.IsciUzreAsync(isciId.Value);
                if (baslangic.HasValue && son.HasValue)
                {
                    result = result
                        .Where(x => x.Tarix.Date >= baslangic.Value.Date && x.Tarix.Date <= son.Value.Date)
                        .ToList();
                }
            }
            else if (baslangic.HasValue && son.HasValue)
            {
                result = await _davamiyyetService.AraliqUzreAsync(baslangic.Value, son.Value);
            }
            else if (tarix.HasValue)
            {
                result = await _davamiyyetService.TarixUzreAsync(tarix.Value);
            }
            else
            {
                result = await _davamiyyetService.TarixUzreAsync(DateTime.Today);
            }

            if (status.HasValue)
            {
                result = result.Where(x => (int)x.Status == status.Value).ToList();
            }

            var data = result.Select(x => new
            {
                id = x.Id,
                isciTamAd = x.IsciTamAd,
                departamentAd = x.DepartamentAd ?? "-",
                tarix = x.Tarix,
                girisVaxti = x.GirisVaxti,
                cixisVaxti = x.CixisVaxti,
                status = (int)x.Status
            }).OrderByDescending(x => x.tarix).ThenBy(x => x.isciTamAd).ToList();

            var isde = result.Count(x => x.Status == DavamiyyetStatus.Isde);
            var gecikme = result.Count(x => x.Status == DavamiyyetStatus.Gecikme);
            var qayib = result.Count(x => x.Status == DavamiyyetStatus.Qayib);
            var icazeli = result.Count(x => x.Status == DavamiyyetStatus.Icazeli);

            return Json(new
            {
                records = data,
                stats = new { isde, gecikme, qayib, icazeli, cemi = result.Count }
            });
        }

        [HttpGet]
        public async Task<IActionResult> IsciAxtar(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(new List<object>());

            var isciler = await _isciService.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv &&
                     (x.Ad.Contains(q) || x.Soyad.Contains(q)),
                izlemeden: true);

            var result = isciler.Success
                ? isciler.Data!.Take(10).Select(x => new { id = x.Id, tamAd = x.TamAd, sobe = x.SobeAdi ?? "-" })
                : Enumerable.Empty<object>();

            return Json(result);
        }
    }
}
