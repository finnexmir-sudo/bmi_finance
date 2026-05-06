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
public class XercHesabatController : Controller
{
    private readonly IUnitOfWork _uow;

    public XercHesabatController(IUnitOfWork uow) => _uow = uow;

    // ── GET /HR/XercHesabat ─────────────────────────────────
    public async Task<IActionResult> Index(int? departamentId, int? ay, int? il)
    {
        ViewData["Title"] = "Xərc Hesabatı";
        ViewBag.Departamentler = await _uow.Repository<Departament>()
            .Query().Where(x => !x.Silinib).OrderBy(x => x.Ad).ToListAsync();
        ViewBag.Kateqoriyalar = await _uow.Repository<XercKateqoriyasi>()
            .Query().Where(x => !x.Silinib).OrderBy(x => x.Ad).ToListAsync();

        // Pre-fill filters from query string (drill-down from budget)
        ViewBag.FilterDepartamentId = departamentId;
        ViewBag.FilterAy = ay;
        ViewBag.FilterIl = il ?? DateTime.Now.Year;

        return View();
    }

    // ── GET /HR/XercHesabat/GetData ─────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetData(
        string? basTarix, string? sonTarix,
        int? departamentId, int? kateqoriyaId,
        int? status, string? axtaris)
    {
        var query = _uow.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib)
            .Include(x => x.Kateqoriya)
            .Include(x => x.Isci)
            .Include(x => x.Departament)
            .AsQueryable();

        // Tarix filtri
        if (DateTime.TryParse(basTarix, out var bt))
            query = query.Where(x => x.XercTarixi >= bt);
        if (DateTime.TryParse(sonTarix, out var st))
            query = query.Where(x => x.XercTarixi <= st.AddDays(1).AddTicks(-1));

        // Şöbə filtri
        if (departamentId.HasValue && departamentId > 0)
            query = query.Where(x => x.DepartamentId == departamentId);

        // Kateqoriya filtri
        if (kateqoriyaId.HasValue && kateqoriyaId > 0)
            query = query.Where(x => x.KateqoriyaId == kateqoriyaId);

        // Status filtri
        if (status.HasValue && status >= 0)
            query = query.Where(x => (int)x.Status == status);

        // Axtarış
        if (!string.IsNullOrWhiteSpace(axtaris))
        {
            var s = axtaris.ToLower();
            query = query.Where(x =>
                (x.Isci != null && (x.Isci.Ad + " " + x.Isci.Soyad).ToLower().Contains(s)) ||
                (x.Departament != null && x.Departament.Ad.ToLower().Contains(s)) ||
                x.Tesvir.ToLower().Contains(s) ||
                (x.Kateqoriya != null && x.Kateqoriya.Ad.ToLower().Contains(s)));
        }

        var list = await query.OrderByDescending(x => x.XercTarixi).ToListAsync();

        var data = list.Select(x => new
        {
            id = x.Id,
            tarix = x.XercTarixi.ToString("dd.MM.yyyy"),
            isciSobe = x.ManualGiris
                ? (x.Departament?.Ad ?? "—")
                : (x.Isci?.TamAd ?? "—"),
            sobeAd = x.Departament?.Ad ?? "—",
            kateqoriya = x.Kateqoriya?.Ad ?? "—",
            tesvir = x.Tesvir,
            mebleg = x.Mebleg,
            status = (int)x.Status,
            statusAd = x.Status switch
            {
                XercStatus.Muraciet        => "Müraciət",
                XercStatus.SobeReisiTesdiq => "Şöbə rəisi",
                XercStatus.HrTesdiq        => "HR təsdiqi",
                XercStatus.Odenildi        => "Ödənildi",
                XercStatus.ImtinaEdildi    => "İmtina",
                _ => "—"
            },
            manualGiris = x.ManualGiris,
            qebzYolu = x.QebzFaylYolu,
            departamentId = x.DepartamentId
        }).ToList();

        // Xülasə
        var odenilenCem = list.Where(x => x.Status == XercStatus.Odenildi).Sum(x => x.Mebleg);
        var gozlemecdeCem = list.Where(x => x.Status == XercStatus.Muraciet
                                         || x.Status == XercStatus.SobeReisiTesdiq
                                         || x.Status == XercStatus.HrTesdiq).Sum(x => x.Mebleg);
        var imtinaEdilen = list.Count(x => x.Status == XercStatus.ImtinaEdildi);

        // Kateqoriya üzrə breakdown
        var kateqoriyaBreakdown = list
            .Where(x => x.Status == XercStatus.Odenildi)
            .GroupBy(x => x.Kateqoriya?.Ad ?? "—")
            .Select(g => new { ad = g.Key, mebleg = g.Sum(x => x.Mebleg), say = g.Count() })
            .OrderByDescending(x => x.mebleg)
            .ToList();

        // Şöbə üzrə breakdown
        var sobeBreakdown = list
            .Where(x => x.Status == XercStatus.Odenildi && x.DepartamentId.HasValue)
            .GroupBy(x => new { x.DepartamentId, ad = x.Departament?.Ad ?? "—" })
            .Select(g => new { departamentId = g.Key.DepartamentId, ad = g.Key.ad, mebleg = g.Sum(x => x.Mebleg), say = g.Count() })
            .OrderByDescending(x => x.mebleg)
            .ToList();

        return Json(new
        {
            xercler = data,
            umumi = new
            {
                say = list.Count,
                odenilenCem,
                gozlemecdeCem,
                imtinaEdilen
            },
            kateqoriyaBreakdown,
            sobeBreakdown
        });
    }

    // ── GET /HR/XercHesabat/ExportExcel ─────────────────────
    [HttpGet]
    public async Task<IActionResult> ExportExcel(
        string? basTarix, string? sonTarix,
        int? departamentId, int? kateqoriyaId, int? status)
    {
        IQueryable<Xerc> query = _uow.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib)
            .Include(x => x.Kateqoriya)
            .Include(x => x.Isci)
            .Include(x => x.Departament);

        if (DateTime.TryParse(basTarix, out var bt))
            query = query.Where(x => x.XercTarixi >= bt);
        if (DateTime.TryParse(sonTarix, out var st))
            query = query.Where(x => x.XercTarixi <= st.AddDays(1).AddTicks(-1));
        if (departamentId > 0)
            query = query.Where(x => x.DepartamentId == departamentId);
        if (kateqoriyaId > 0)
            query = query.Where(x => x.KateqoriyaId == kateqoriyaId);
        if (status.HasValue && status >= 0)
            query = query.Where(x => (int)x.Status == status);

        var list = await query.OrderByDescending(x => x.XercTarixi).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Xərc Hesabatı");

        // Header
        string[] headers = { "Tarix", "İşçi / Şöbə", "Şöbə", "Kateqoriya", "Təsvir", "Məbləğ (AZN)", "Status", "Növ" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        var hRange = ws.Range(1, 1, 1, headers.Length);
        hRange.Style.Font.Bold = true;
        hRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e2a3b");
        hRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        decimal odenilenCem = 0;

        foreach (var x in list)
        {
            var isciSobe = x.ManualGiris ? (x.Departament?.Ad ?? "—") : (x.Isci?.TamAd ?? "—");
            var statusAd = x.Status switch
            {
                XercStatus.Muraciet        => "Müraciət",
                XercStatus.SobeReisiTesdiq => "Şöbə rəisi",
                XercStatus.HrTesdiq        => "HR təsdiqi",
                XercStatus.Odenildi        => "Ödənildi",
                XercStatus.ImtinaEdildi    => "İmtina",
                _ => "—"
            };

            ws.Cell(row, 1).Value = x.XercTarixi.ToString("dd.MM.yyyy");
            ws.Cell(row, 2).Value = isciSobe;
            ws.Cell(row, 3).Value = x.Departament?.Ad ?? "—";
            ws.Cell(row, 4).Value = x.Kateqoriya?.Ad ?? "—";
            ws.Cell(row, 5).Value = x.Tesvir;
            ws.Cell(row, 6).Value = x.Mebleg;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = statusAd;
            ws.Cell(row, 8).Value = x.ManualGiris ? "Manual" : "İşçi müraciəti";

            if (x.Status == XercStatus.Odenildi) odenilenCem += x.Mebleg;
            row++;
        }

        // Cəm sətri
        ws.Cell(row, 5).Value = "Ödənildi cəmi:";
        ws.Cell(row, 5).Style.Font.Bold = true;
        ws.Cell(row, 6).Value = odenilenCem;
        ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 6).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var ad = $"Xerc_Hesabat_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ad);
    }
}
