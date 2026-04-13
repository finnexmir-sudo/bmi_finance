using ClosedXML.Excel;
using FinNex.Application.Interfaces;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class DavamiyyetController : Controller
    {
        private readonly IDavamiyyetService _davamiyyetService;
        private readonly IIsciService _isciService;
        private readonly IUnitOfWork _unitOfWork;

        public DavamiyyetController(
            IDavamiyyetService davamiyyetService,
            IIsciService isciService,
            IUnitOfWork unitOfWork)
        {
            _davamiyyetService = davamiyyetService;
            _isciService = isciService;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var bugun = DateTime.Today;
            var list = await _davamiyyetService.TarixUzreAsync(bugun);

            // Aktiv işçi sayı — gözlənilir hesablanması üçün
            var aktivIsciSayi = await _unitOfWork.Repository<Isci>()
                .Query()
                .CountAsync(x => !x.Silinib && x.Status == IsciStatus.Aktiv);
            ViewBag.AktivIsciSayi = aktivIsciSayi;

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetByTarix(DateTime? tarix, DateTime? baslangic, DateTime? son, int? isciId, int? status)
        {
            var result = await GetFilteredData(tarix, baslangic, son, isciId, status);

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

            var gelib = result.Count(x => x.Status == DavamiyyetStatus.Isde || x.Status == DavamiyyetStatus.Gecikme);
            var gecikme = result.Count(x => x.Status == DavamiyyetStatus.Gecikme);
            var qayib = result.Count(x => x.Status == DavamiyyetStatus.Qayib);
            var icazeli = result.Count(x => x.Status == DavamiyyetStatus.Icazeli);
            var xestelik = result.Count(x => x.Status == DavamiyyetStatus.Xestelik);
            var ezamiyyet = result.Count(x => x.Status == DavamiyyetStatus.Ezamiyyet);

            // Orta iş saatı
            var iseSaatleri = result
                .Where(x => x.GirisVaxti.HasValue && x.CixisVaxti.HasValue)
                .Select(x => (x.CixisVaxti!.Value - x.GirisVaxti!.Value).TotalHours)
                .ToList();
            var ortaIsSaati = iseSaatleri.Any() ? Math.Round(iseSaatleri.Average(), 1) : 0;

            // Ən çox gecikən departament
            var enCoxGecikenDept = result
                .Where(x => x.Status == DavamiyyetStatus.Gecikme)
                .GroupBy(x => x.DepartamentAd ?? "-")
                .OrderByDescending(g => g.Count())
                .Select(g => new { ad = g.Key, say = g.Count() })
                .FirstOrDefault();

            return Json(new
            {
                records = data,
                stats = new
                {
                    gelib,
                    gecikme,
                    qayib,
                    icazeli,
                    xestelik,
                    ezamiyyet,
                    cemi = result.Count,
                    ortaIsSaati,
                    enCoxGecikenDept = enCoxGecikenDept?.ad ?? "-",
                    enCoxGecikenDeptSay = enCoxGecikenDept?.say ?? 0
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(DateTime? tarix, DateTime? baslangic, DateTime? son, int? isciId, int? status)
        {
            var result = await GetFilteredData(tarix, baslangic, son, isciId, status);
            var sorted = result.OrderByDescending(x => x.Tarix).ThenBy(x => x.IsciTamAd).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Davamiyyət");

            // Başlıq
            ws.Cell(1, 1).Value = "İşçi";
            ws.Cell(1, 2).Value = "Departament";
            ws.Cell(1, 3).Value = "Tarix";
            ws.Cell(1, 4).Value = "Giriş";
            ws.Cell(1, 5).Value = "Çıxış";
            ws.Cell(1, 6).Value = "İş saatı";
            ws.Cell(1, 7).Value = "Status";

            var headerRange = ws.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e2a3b");
            headerRange.Style.Font.FontColor = XLColor.White;

            for (int i = 0; i < sorted.Count; i++)
            {
                var r = sorted[i];
                var row = i + 2;

                ws.Cell(row, 1).Value = r.IsciTamAd;
                ws.Cell(row, 2).Value = r.DepartamentAd ?? "-";
                ws.Cell(row, 3).Value = r.Tarix.ToString("dd.MM.yyyy");
                ws.Cell(row, 4).Value = r.GirisVaxti?.ToString("HH:mm") ?? "--:--";
                ws.Cell(row, 5).Value = r.CixisVaxti?.ToString("HH:mm") ?? "--:--";

                if (r.GirisVaxti.HasValue && r.CixisVaxti.HasValue)
                {
                    var dur = r.CixisVaxti.Value - r.GirisVaxti.Value;
                    ws.Cell(row, 6).Value = $"{dur.Hours} s {dur.Minutes} d";
                }
                else
                    ws.Cell(row, 6).Value = "---";

                ws.Cell(row, 7).Value = r.Status switch
                {
                    DavamiyyetStatus.Isde => "İşdə",
                    DavamiyyetStatus.Gecikme => "Gecikmə",
                    DavamiyyetStatus.Qayib => "Qayıb",
                    DavamiyyetStatus.Icazeli => "İcazəli",
                    DavamiyyetStatus.Xestelik => "Xəstəlik",
                    DavamiyyetStatus.Ezamiyyet => "Ezamiyyət",
                    _ => "-"
                };
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var fileName = $"Davamiyyet_{DateTime.Now:yyyy-MM-dd_HHmm}.xlsx";
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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

        // ── Helper: shared filtering logic ──
        private async Task<IList<Application.DTOs.HR.Davamiyyet.DavamiyyetListDto>> GetFilteredData(
            DateTime? tarix, DateTime? baslangic, DateTime? son, int? isciId, int? status)
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

            return result;
        }
    }
}
