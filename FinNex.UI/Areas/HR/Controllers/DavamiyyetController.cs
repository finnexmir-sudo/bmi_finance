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

            // Giriş saatına görə sırala — qeydə alınanlar əvvəl, sonra digərləri
            list = list
                .OrderBy(x => x.GirisVaxti == null)
                .ThenBy(x => x.GirisVaxti)
                .ThenBy(x => x.IsciTamAd)
                .ToList();

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
            try
            {
                // KPI-lar filter-dən ƏVVƏL hesablanır ki, status filtri cədvələ təsir
                // etsə də, yuxarıdakı statistika sabit qalsın (istifadəçi filtrlə işləyəndə
                // bütün KPI-ların sıfra düşməsi pis UX-dir).
                var umumi = await GetFilteredData(tarix, baslangic, son, isciId, null);
                var result = status.HasValue
                    ? umumi.Where(x => (int)x.Status == status.Value).ToList()
                    : umumi;

                var data = result.Select(x => new
                {
                    id = x.Id,
                    isciTamAd = x.IsciTamAd ?? "-",
                    departamentAd = x.DepartamentAd ?? "-",
                    tarix = x.Tarix,
                    girisVaxti = x.GirisVaxti,
                    cixisVaxti = x.CixisVaxti,
                    status = (int)x.Status,
                    maasdanKes = x.MaasdanKes,
                    qayibSebebi = x.QayibSebebi ?? ""
                }).OrderByDescending(x => x.tarix).ThenBy(x => x.isciTamAd).ToList();

                // Stats — umumi üzərindən (filter olsa belə bütün KPI-lar görünsün)
                var gelib = umumi.Count(x => x.Status == DavamiyyetStatus.Isde || x.Status == DavamiyyetStatus.Gecikme);
                var gecikme = umumi.Count(x => x.Status == DavamiyyetStatus.Gecikme);
                var qayib = umumi.Count(x => x.Status == DavamiyyetStatus.Qayib);
                var icazeli = umumi.Count(x => x.Status == DavamiyyetStatus.Icazeli);
                var xestelik = umumi.Count(x => x.Status == DavamiyyetStatus.Xestelik);
                var ezamiyyet = umumi.Count(x => x.Status == DavamiyyetStatus.Ezamiyyet);

                var iseSaatleri = umumi
                    .Where(x => x.GirisVaxti.HasValue && x.CixisVaxti.HasValue)
                    .Select(x => (x.CixisVaxti!.Value - x.GirisVaxti!.Value).TotalHours)
                    .ToList();
                var ortaIsSaati = iseSaatleri.Any() ? Math.Round(iseSaatleri.Average(), 1) : 0;

                var enCoxGecikenDept = umumi
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
                        cemi = umumi.Count,
                        ortaIsSaati,
                        enCoxGecikenDept = enCoxGecikenDept?.ad ?? "-",
                        enCoxGecikenDeptSay = enCoxGecikenDept?.say ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                // Server-side log — prod-da ILogger istifadə olunmalıdır
                return StatusCode(500, new { error = ex.Message });
            }
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

        // ── Gözlənilən işçilər ──────────────────────────────────
        // Aktiv olan, amma göstərilən tarix üçün davamiyyət qeydi olmayan
        // işçilər. Bu gün üçün — hələ gəlməyənlər. Keçmiş tarix üçün — real
        // qayıblar (icazə/xəstəlik yoxdursa).
        [HttpGet]
        public async Task<IActionResult> GetGozlenilen(DateTime? tarix)
        {
            var hedef = (tarix ?? DateTime.Today).Date;

            // Aktiv işçilər
            var aktivIsciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .Include(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .ToListAsync();

            // Hədəf tarixdə qeydi olanların ID-ləri
            var qeydiOlanlar = await _unitOfWork.Repository<Davamiyyet>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Tarix.Date == hedef)
                .Select(x => x.IsciId)
                .ToListAsync();

            var gozlenilenler = aktivIsciler
                .Where(i => !qeydiOlanlar.Contains(i.Id))
                .Select(i =>
                {
                    var esasTeyinat = i.IsciTeyinatlari
                        .Where(t => t.Esasdir && !t.Silinib)
                        .FirstOrDefault()
                        ?? i.IsciTeyinatlari.FirstOrDefault(t => !t.Silinib);
                    return new
                    {
                        id = 0,
                        isciId = i.Id,
                        isciTamAd = i.Ad + " " + i.Soyad,
                        departamentAd = esasTeyinat?.Departament?.Ad ?? "-",
                        tarix = hedef,
                        girisVaxti = (DateTime?)null,
                        cixisVaxti = (DateTime?)null,
                        // Keçmiş gün üçün = Qayıb (3), bu gün üçün = Gözlənilir (0 virtual)
                        status = hedef < DateTime.Today ? 3 : 0
                    };
                })
                .OrderBy(x => x.isciTamAd)
                .ToList();

            return Json(new
            {
                records = gozlenilenler,
                count = gozlenilenler.Count,
                tarix = hedef
            });
        }

        [HttpPost]
        public async Task<IActionResult> QayibDuzelt([FromBody] QayibDuzeltRequest req)
        {
            if (req == null || req.Id <= 0)
                return BadRequest(new { error = "Məlumat natamamdır." });

            var entity = await _unitOfWork.Repository<Davamiyyet>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == req.Id && !x.Silinib && x.Status == DavamiyyetStatus.Qayib);

            if (entity == null)
                return BadRequest(new { error = "Qayıb qeydi tapılmadı." });

            var maasOdenilib = await _unitOfWork.Repository<Maas>()
                .Query()
                .AnyAsync(m => !m.Silinib && m.IsciId == entity.IsciId
                    && m.Il == entity.Tarix.Year && m.Ay == entity.Tarix.Month
                    && m.Status == MaasStatus.Odenildi);

            if (maasOdenilib)
                return BadRequest(new { error = "Bu ayın maaşı artıq ödənildiyi üçün dəyişiklik edilə bilməz." });

            entity.MaasdanKes = req.MaasdanKes;
            entity.QayibSebebi = req.QayibSebebi?.Trim();
            await _unitOfWork.YaddaSaxlaAsync();

            return Ok(new { message = "Yeniləndi." });
        }

        [HttpPost]
        public async Task<IActionResult> QayibYaz([FromBody] QayibYazRequest req)
        {
            if (req == null || req.IsciId <= 0)
                return BadRequest(new { error = "Məlumat natamamdır." });

            var tarix = req.Tarix.Date;

            var maasOdenilib = await _unitOfWork.Repository<Maas>()
                .Query()
                .AnyAsync(m => !m.Silinib && m.IsciId == req.IsciId
                    && m.Il == tarix.Year && m.Ay == tarix.Month
                    && m.Status == MaasStatus.Odenildi);

            if (maasOdenilib)
                return BadRequest(new { error = "Bu ayın maaşı artıq ödənildiyi üçün dəyişiklik edilə bilməz." });

            var movcut = await _davamiyyetService.BuGunMovcuddurmuAsync(req.IsciId, tarix);
            if (movcut)
                return BadRequest(new { error = "Bu tarix üçün davamiyyət qeydi artıq mövcuddur." });

            var dto = new Application.DTOs.HR.Davamiyyet.DavamiyyetCreateDto
            {
                IsciId = req.IsciId,
                Tarix = tarix,
                Status = DavamiyyetStatus.Qayib,
                MaasdanKes = req.MaasdanKes,
                QayibSebebi = req.QayibSebebi?.Trim()
            };

            var result = await _davamiyyetService.YaratAsync(dto);
            if (!result.Success)
                return BadRequest(new { error = result.Message });

            return Ok(new { message = "Qayıb uğurla qeyd edildi." });
        }

        [HttpPost]
        public async Task<IActionResult> QayibSil([FromBody] QayibSilRequest req)
        {
            if (req == null || req.Id <= 0)
                return BadRequest(new { error = "Məlumat natamamdır." });

            var entity = await _unitOfWork.Repository<Davamiyyet>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == req.Id && !x.Silinib && x.Status == DavamiyyetStatus.Qayib);

            if (entity == null)
                return BadRequest(new { error = "Qayıb qeydi tapılmadı." });

            var maasOdenilib = await _unitOfWork.Repository<Maas>()
                .Query()
                .AnyAsync(m => !m.Silinib && m.IsciId == entity.IsciId
                    && m.Il == entity.Tarix.Year && m.Ay == entity.Tarix.Month
                    && m.Status == MaasStatus.Odenildi);

            if (maasOdenilib)
                return BadRequest(new { error = "Bu ayın maaşı artıq ödənildiyi üçün silinmə edilə bilməz." });

            entity.Silinib = true;
            await _unitOfWork.YaddaSaxlaAsync();

            return Ok(new { message = "Qayıb qeydi silindi." });
        }

        [HttpGet]
        public async Task<IActionResult> IsciAxtar(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new List<object>());

            var isciler = await _isciService.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv &&
                     (x.Ad.StartsWith(q) || x.Soyad.StartsWith(q)),
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

    public class QayibDuzeltRequest
    {
        public int Id { get; set; }
        public bool MaasdanKes { get; set; }
        public string? QayibSebebi { get; set; }
    }

    public class QayibYazRequest
    {
        public int IsciId { get; set; }
        public DateTime Tarix { get; set; }
        public bool MaasdanKes { get; set; }
        public string? QayibSebebi { get; set; }
    }

    public class QayibSilRequest
    {
        public int Id { get; set; }
    }
}
