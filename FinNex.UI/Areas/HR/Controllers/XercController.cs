using ClosedXML.Excel;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinNex.UI.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber + "," + RoleNames.SobeReisi)]
public class XercController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public XercController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── GET /HR/Xerc ────────────────────────────────────────
    public IActionResult Index()
    {
        ViewData["Title"] = "Xerc Idareetmesi";
        return View();
    }

    // ── GET /HR/Xerc/GetData ────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetData(int? status, string? axtaris)
    {
        var query = _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib)
            .Include(x => x.Isci)
            .Include(x => x.Kateqoriya)
            .Include(x => x.TesdiqleyenIsci)
            .AsQueryable();

        if (status.HasValue && status >= 0)
            query = query.Where(x => (int)x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(axtaris))
            query = query.Where(x =>
                x.Isci.Ad.Contains(axtaris) ||
                x.Isci.Soyad.Contains(axtaris) ||
                x.Tesvir.Contains(axtaris));

        var xercler = await query
            .OrderByDescending(x => x.XercTarixi)
            .ToListAsync();

        var data = xercler.Select(x => new
        {
            id = x.Id,
            isciAd = x.Isci != null ? $"{x.Isci.Ad} {x.Isci.Soyad}" : "-",
            kateqoriya = x.Kateqoriya?.Ad ?? "-",
            tesvir = x.Tesvir,
            mebleg = x.Mebleg,
            xercTarixi = x.XercTarixi.ToString("dd.MM.yyyy"),
            status = (int)x.Status,
            statusAd = x.Status switch
            {
                XercStatus.Muraciet => "Muraciet",
                XercStatus.SobeReisiTesdiq => "Sobe reisi tesdiq",
                XercStatus.HrTesdiq => "HR tesdiq",
                XercStatus.Odenildi => "Odenildi",
                XercStatus.ImtinaEdildi => "Imtina edildi",
                _ => "-"
            },
            tesdiqleyenAd = x.TesdiqleyenIsci != null ? $"{x.TesdiqleyenIsci.Ad} {x.TesdiqleyenIsci.Soyad}" : null,
            tesdiqTarixi = x.TesdiqTarixi?.ToString("dd.MM.yyyy"),
            imtinaSebebi = x.ImtinaSebebi,
            qebzFaylYolu = x.QebzFaylYolu
        });

        return Json(new { xercler = data });
    }

    // ── POST /HR/Xerc/Tesdiqle ──────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Tesdiqle(int id)
    {
        var xerc = await _unitOfWork.Repository<Xerc>().IdIleGetirAsync(id);
        if (xerc == null)
            return NotFound(new { message = "Xerc tapilmadi." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tesdiqIsci = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        // Advance status based on current
        if (xerc.Status == XercStatus.Muraciet)
            xerc.Status = XercStatus.SobeReisiTesdiq;
        else if (xerc.Status == XercStatus.SobeReisiTesdiq)
            xerc.Status = XercStatus.HrTesdiq;

        xerc.TesdiqleyenIsciId = tesdiqIsci?.Id;
        xerc.TesdiqTarixi = DateTime.Now;

        await _unitOfWork.Repository<Xerc>().YenileAsync(xerc);
        await _unitOfWork.YaddaSaxlaAsync();

        return Ok(new { message = "Xerc tesdiqlendi." });
    }

    // ── POST /HR/Xerc/Imtina ────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Imtina(int id, [FromBody] ImtinaDto dto)
    {
        var xerc = await _unitOfWork.Repository<Xerc>().IdIleGetirAsync(id);
        if (xerc == null)
            return NotFound(new { message = "Xerc tapilmadi." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tesdiqIsci = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        xerc.Status = XercStatus.ImtinaEdildi;
        xerc.ImtinaSebebi = dto.Sebeb;
        xerc.TesdiqleyenIsciId = tesdiqIsci?.Id;
        xerc.TesdiqTarixi = DateTime.Now;

        await _unitOfWork.Repository<Xerc>().YenileAsync(xerc);
        await _unitOfWork.YaddaSaxlaAsync();

        return Ok(new { message = "Xerc imtina edildi." });
    }

    // ── POST /HR/Xerc/Ode ───────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Ode(int id)
    {
        var xerc = await _unitOfWork.Repository<Xerc>().IdIleGetirAsync(id);
        if (xerc == null)
            return NotFound(new { message = "Xerc tapilmadi." });

        xerc.Status = XercStatus.Odenildi;
        xerc.TesdiqTarixi = DateTime.Now;

        await _unitOfWork.Repository<Xerc>().YenileAsync(xerc);
        await _unitOfWork.YaddaSaxlaAsync();

        return Ok(new { message = "Xerc odenildi kimi isharelendi." });
    }

    // ── GET /HR/Xerc/ExportExcel ────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ExportExcel(int? status)
    {
        var query = _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib)
            .Include(x => x.Isci)
            .Include(x => x.Kateqoriya)
            .AsQueryable();

        if (status.HasValue && status >= 0)
            query = query.Where(x => (int)x.Status == status.Value);

        var xercler = await query
            .OrderByDescending(x => x.XercTarixi)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Xercler");

        ws.Cell(1, 1).Value = "Isci";
        ws.Cell(1, 2).Value = "Kateqoriya";
        ws.Cell(1, 3).Value = "Tesvir";
        ws.Cell(1, 4).Value = "Mebleg (AZN)";
        ws.Cell(1, 5).Value = "Xerc Tarixi";
        ws.Cell(1, 6).Value = "Status";

        var headerRange = ws.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e2a3b");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var x in xercler)
        {
            ws.Cell(row, 1).Value = x.Isci != null ? $"{x.Isci.Ad} {x.Isci.Soyad}" : "-";
            ws.Cell(row, 2).Value = x.Kateqoriya?.Ad ?? "-";
            ws.Cell(row, 3).Value = x.Tesvir;
            ws.Cell(row, 4).Value = x.Mebleg;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Value = x.XercTarixi.ToString("dd.MM.yyyy");
            ws.Cell(row, 6).Value = x.Status.ToString();
            row++;
        }

        ws.Cell(row, 1).Value = "CEMI";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = xercler.Sum(x => x.Mebleg);
        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 4).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fileName = $"Xercler_Hesabat.xlsx";
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}

public class ImtinaDto
{
    public string Sebeb { get; set; } = null!;
}
