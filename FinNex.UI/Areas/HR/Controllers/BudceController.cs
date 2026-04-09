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
        ViewData["Title"] = "Budce Planlamasi";
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

        var data = departamentlar.Select(d =>
        {
            var aylar = Enumerable.Range(1, 12).Select(ay =>
            {
                var b = budceler.FirstOrDefault(x => x.DepartamentId == d.Id && x.Ay == ay);
                return new
                {
                    ay,
                    plan = b?.PlanMebleg ?? 0,
                    faktiki = b?.FaktikiMebleg ?? 0
                };
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

    // ── POST /HR/Budce/Create ───────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BudceCreateDto dto)
    {
        if (dto.DepartamentId <= 0 || dto.Ay < 1 || dto.Ay > 12 || dto.Il < 2020)
            return BadRequest(new { message = "Xeta: yanlish melumat." });

        var existing = await _unitOfWork.Repository<Budce>()
            .GetirAsync(x => !x.Silinib
                && x.DepartamentId == dto.DepartamentId
                && x.Il == dto.Il
                && x.Ay == dto.Ay);

        if (existing != null)
        {
            existing.PlanMebleg = dto.PlanMebleg;
            existing.FaktikiMebleg = dto.FaktikiMebleg;
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
                FaktikiMebleg = dto.FaktikiMebleg,
                Qeyd = dto.Qeyd
            };
            await _unitOfWork.Repository<Budce>().YaratAsync(budce);
        }

        await _unitOfWork.YaddaSaxlaAsync();
        return Ok(new { message = "Ugurla yadda saxlanildi." });
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

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Budce {il}");

        // Header
        ws.Cell(1, 1).Value = "Departament";
        string[] ayAdlari = { "Yan", "Fev", "Mar", "Apr", "May", "Iyn", "Iyl", "Avq", "Sen", "Okt", "Noy", "Dek" };
        for (int i = 0; i < 12; i++)
        {
            ws.Cell(1, 2 + i * 2).Value = $"{ayAdlari[i]} Plan";
            ws.Cell(1, 3 + i * 2).Value = $"{ayAdlari[i]} Faktiki";
        }
        ws.Cell(1, 26).Value = "Toplam Plan";
        ws.Cell(1, 27).Value = "Toplam Faktiki";

        var headerRange = ws.Range(1, 1, 1, 27);
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
                var fakt = b?.FaktikiMebleg ?? 0;
                ws.Cell(row, 2 + (ay - 1) * 2).Value = plan;
                ws.Cell(row, 2 + (ay - 1) * 2).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 3 + (ay - 1) * 2).Value = fakt;
                ws.Cell(row, 3 + (ay - 1) * 2).Style.NumberFormat.Format = "#,##0.00";
                topPlan += plan;
                topFakt += fakt;
            }

            ws.Cell(row, 26).Value = topPlan;
            ws.Cell(row, 26).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 27).Value = topFakt;
            ws.Cell(row, 27).Style.NumberFormat.Format = "#,##0.00";

            umumiPlan += topPlan;
            umumiFaktiki += topFakt;
            row++;
        }

        // Summary row
        ws.Cell(row, 1).Value = "CEMI";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 26).Value = umumiPlan;
        ws.Cell(row, 26).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 26).Style.Font.Bold = true;
        ws.Cell(row, 27).Value = umumiFaktiki;
        ws.Cell(row, 27).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 27).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fileName = $"Budce_Hesabat_{il}.xlsx";
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}

// ── DTO ─────────────────────────────────────────────────────
public class BudceCreateDto
{
    public int DepartamentId { get; set; }
    public int Il { get; set; }
    public int Ay { get; set; }
    public decimal PlanMebleg { get; set; }
    public decimal FaktikiMebleg { get; set; }
    public string? Qeyd { get; set; }
}
