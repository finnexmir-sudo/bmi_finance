using ClosedXML.Excel;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
public class BudceController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public BudceController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── GET /HR/Budce ───────────────────────────────────────
    public IActionResult Index()
    {
        ViewData["Title"] = "Büdcə Planlaması";
        return View();
    }

    // ── GET /HR/Budce/GetData?il=2026 ───────────────────────
    [HttpGet]
    public async Task<IActionResult> GetData(int il)
    {
        var budceler = await _unitOfWork.Repository<Budce>()
            .Query()
            .Where(x => !x.Silinib && x.Il == il)
            .Include(x => x.Departament)
            .ToListAsync();

        var departamentlar = await _unitOfWork.Repository<Departament>()
            .Query()
            .Where(x => !x.Silinib)
            .OrderBy(x => x.Ad)
            .ToListAsync();

        // Faktiki — real-time ödənilmiş xərclər
        var odenilenXercler = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib
                     && x.Status == XercStatus.Odenildi
                     && x.XercTarixi.Year == il
                     && x.DepartamentId.HasValue)
            .Select(x => new { x.DepartamentId, x.Mebleg, Month = x.XercTarixi.Month })
            .ToListAsync();

        var data = departamentlar.Select(d =>
        {
            var aylar = Enumerable.Range(1, 12).Select(ay =>
            {
                var b = budceler.FirstOrDefault(x => x.DepartamentId == d.Id && x.Ay == ay);
                var plan = b?.PlanMebleg ?? 0;
                var faktiki = odenilenXercler
                    .Where(x => x.DepartamentId == d.Id && x.Month == ay)
                    .Sum(x => x.Mebleg);
                var faiz = plan > 0 ? Math.Round(faktiki / plan * 100, 1) : 0m;
                return new { ay, plan, faktiki, faiz };
            }).ToList();

            return new
            {
                departamentId = d.Id,
                departamentAd = d.Ad,
                aylar,
                toplamPlan = aylar.Sum(a => a.plan),
                toplamFaktiki = aylar.Sum(a => a.faktiki)
            };
        }).ToList();

        var umumiPlan = data.Sum(x => x.toplamPlan);
        var umumiFaktiki = data.Sum(x => x.toplamFaktiki);

        return Json(new { departamentlar = data, umumiPlan, umumiFaktiki });
    }

    // ── GET /HR/Budce/GetLimitInfo ──────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetLimitInfo(int departamentId, int il, int ay)
    {
        var budce = await _unitOfWork.Repository<Budce>()
            .GetirAsync(x => !x.Silinib
                          && x.DepartamentId == departamentId
                          && x.Il == il && x.Ay == ay);

        var plan = budce?.PlanMebleg ?? 0;

        var faktiki = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib
                     && x.Status == XercStatus.Odenildi
                     && x.DepartamentId == departamentId
                     && x.XercTarixi.Year == il
                     && x.XercTarixi.Month == ay)
            .SumAsync(x => (decimal?)x.Mebleg) ?? 0;

        var qaliq = plan - faktiki;
        var faiz = plan > 0 ? Math.Round(faktiki / plan * 100, 1) : 0m;

        return Json(new { plan, faktiki, qaliq, faiz, yoxdur = plan == 0 });
    }

    // ── GET /HR/Budce/GetDetay ──────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetDetay(int departamentId, int il, int ay)
    {
        var xercler = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib
                     && x.DepartamentId == departamentId
                     && x.XercTarixi.Year == il
                     && x.XercTarixi.Month == ay)
            .Include(x => x.Kateqoriya)
            .Include(x => x.Isci)
            .OrderByDescending(x => x.XercTarixi)
            .ToListAsync();

        var data = xercler.Select(x => new
        {
            id = x.Id,
            tarix = x.XercTarixi.ToString("dd.MM.yyyy"),
            kateqoriya = x.Kateqoriya?.Ad ?? "—",
            tesvir = x.Tesvir,
            mebleg = x.Mebleg,
            status = (int)x.Status,
            statusAd = x.Status switch
            {
                XercStatus.Muraciet       => "Müraciət",
                XercStatus.SobeReisiTesdiq => "Şöbə rəisi təsdiqi",
                XercStatus.HrTesdiq       => "HR təsdiqi",
                XercStatus.Odenildi       => "Ödənildi",
                XercStatus.ImtinaEdildi   => "İmtina",
                _ => "—"
            },
            isci = x.Isci != null ? x.Isci.TamAd : (x.ManualGiris ? "Manual giriş" : "—"),
            manualGiris = x.ManualGiris,
            qebzYolu = x.QebzFaylYolu
        }).ToList();

        var toplam = xercler.Where(x => x.Status == XercStatus.Odenildi).Sum(x => x.Mebleg);

        return Json(new { xercler = data, odenilenToplam = toplam });
    }

    // ── POST /HR/Budce/Create ───────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] BudceCreateDto dto)
    {
        if (dto.DepartamentId <= 0 || dto.Ay < 1 || dto.Ay > 12 || dto.Il < 2020)
            return BadRequest(new { message = "Xəta: yanlış məlumat." });

        var existing = await _unitOfWork.Repository<Budce>()
            .GetirAsync(x => !x.Silinib
                && x.DepartamentId == dto.DepartamentId
                && x.Il == dto.Il
                && x.Ay == dto.Ay);

        if (existing != null)
        {
            existing.PlanMebleg = dto.PlanMebleg;
            existing.Qeyd = dto.Qeyd;
            await _unitOfWork.Repository<Budce>().YenileAsync(existing);
        }
        else
        {
            var budce = new Budce
            {
                DepartamentId = dto.DepartamentId,
                Il = dto.Il,
                Ay = dto.Ay,
                PlanMebleg = dto.PlanMebleg,
                FaktikiMebleg = 0,
                Qeyd = dto.Qeyd
            };
            await _unitOfWork.Repository<Budce>().YaratAsync(budce);
        }

        await _unitOfWork.YaddaSaxlaAsync();
        return Ok(new { message = "Uğurla yadda saxlanıldı." });
    }

    // ── GET /HR/Budce/ExportExcel?il=2026 ───────────────────
    [HttpGet]
    public async Task<IActionResult> ExportExcel(int il)
    {
        var budceler = await _unitOfWork.Repository<Budce>()
            .Query()
            .Where(x => !x.Silinib && x.Il == il)
            .Include(x => x.Departament)
            .ToListAsync();

        var departamentlar = await _unitOfWork.Repository<Departament>()
            .Query()
            .Where(x => !x.Silinib)
            .OrderBy(x => x.Ad)
            .ToListAsync();

        var odenilenXercler = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib
                     && x.Status == XercStatus.Odenildi
                     && x.XercTarixi.Year == il
                     && x.DepartamentId.HasValue)
            .Select(x => new { x.DepartamentId, x.Mebleg, Month = x.XercTarixi.Month })
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Budce {il}");

        string[] ayAdlari = { "Yan", "Fev", "Mar", "Apr", "May", "Iyn", "Iyl", "Avq", "Sen", "Okt", "Noy", "Dek" };

        ws.Cell(1, 1).Value = "Departament";
        for (int i = 0; i < 12; i++)
        {
            ws.Cell(1, 2 + i * 3).Value = $"{ayAdlari[i]} Plan";
            ws.Cell(1, 3 + i * 3).Value = $"{ayAdlari[i]} Faktiki";
            ws.Cell(1, 4 + i * 3).Value = $"{ayAdlari[i]} Fərq";
        }
        ws.Cell(1, 38).Value = "Toplam Plan";
        ws.Cell(1, 39).Value = "Toplam Faktiki";
        ws.Cell(1, 40).Value = "Toplam Fərq";

        var headerRange = ws.Range(1, 1, 1, 40);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e2a3b");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        decimal umumiPlan = 0, umumiFaktiki = 0;

        foreach (var d in departamentlar)
        {
            ws.Cell(row, 1).Value = d.Ad;
            decimal topPlan = 0, topFakt = 0;

            for (int ay = 1; ay <= 12; ay++)
            {
                var b = budceler.FirstOrDefault(x => x.DepartamentId == d.Id && x.Ay == ay);
                var plan = b?.PlanMebleg ?? 0;
                var fakt = odenilenXercler
                    .Where(x => x.DepartamentId == d.Id && x.Month == ay)
                    .Sum(x => x.Mebleg);
                var ferq = plan - fakt;

                int col = 2 + (ay - 1) * 3;
                ws.Cell(row, col).Value = plan;
                ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, col + 1).Value = fakt;
                ws.Cell(row, col + 1).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, col + 2).Value = ferq;
                ws.Cell(row, col + 2).Style.NumberFormat.Format = "#,##0.00";
                if (ferq < 0) ws.Cell(row, col + 2).Style.Font.FontColor = XLColor.Red;

                topPlan += plan;
                topFakt += fakt;
            }

            ws.Cell(row, 38).Value = topPlan;
            ws.Cell(row, 38).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 39).Value = topFakt;
            ws.Cell(row, 39).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 40).Value = topPlan - topFakt;
            ws.Cell(row, 40).Style.NumberFormat.Format = "#,##0.00";
            if (topPlan - topFakt < 0) ws.Cell(row, 40).Style.Font.FontColor = XLColor.Red;

            umumiPlan += topPlan;
            umumiFaktiki += topFakt;
            row++;
        }

        ws.Cell(row, 1).Value = "CƏMİ";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 38).Value = umumiPlan;
        ws.Cell(row, 38).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 38).Style.Font.Bold = true;
        ws.Cell(row, 39).Value = umumiFaktiki;
        ws.Cell(row, 39).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 39).Style.Font.Bold = true;
        ws.Cell(row, 40).Value = umumiPlan - umumiFaktiki;
        ws.Cell(row, 40).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 40).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Budce_Hesabat_{il}.xlsx");
    }
}

public class BudceCreateDto
{
    public int DepartamentId { get; set; }
    public int Il { get; set; }
    public int Ay { get; set; }
    public decimal PlanMebleg { get; set; }
    public string? Qeyd { get; set; }
}
