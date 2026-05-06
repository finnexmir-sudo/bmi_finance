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

    public BudceController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

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
            .Query().Where(x => !x.Silinib && x.Il == il)
            .Include(x => x.Departament).ToListAsync();

        var departamentlar = await _unitOfWork.Repository<Departament>()
            .Query().Where(x => !x.Silinib).OrderBy(x => x.Ad).ToListAsync();

        // Ödənilmiş xərclər — Faktiki
        var odenilenXercler = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib && x.Status == XercStatus.Odenildi
                     && x.XercTarixi.Year == il && x.DepartamentId.HasValue)
            .Select(x => new { x.DepartamentId, x.Mebleg, Month = x.XercTarixi.Month })
            .ToListAsync();

        // Təsdiqlənmiş amma ödənilməmiş — Rezerv
        var rezervXercler = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib
                     && (x.Status == XercStatus.SobeReisiTesdiq || x.Status == XercStatus.HrTesdiq)
                     && x.XercTarixi.Year == il && x.DepartamentId.HasValue)
            .Select(x => new { x.DepartamentId, x.Mebleg, Month = x.XercTarixi.Month })
            .ToListAsync();

        var data = departamentlar.Select(d =>
        {
            var aylar = Enumerable.Range(1, 12).Select(ay =>
            {
                var b       = budceler.FirstOrDefault(x => x.DepartamentId == d.Id && x.Ay == ay);
                var plan    = b?.PlanMebleg ?? 0;
                var faktiki = odenilenXercler.Where(x => x.DepartamentId == d.Id && x.Month == ay).Sum(x => x.Mebleg);
                var rezerv  = rezervXercler.Where(x => x.DepartamentId == d.Id && x.Month == ay).Sum(x => x.Mebleg);
                var azad    = plan - faktiki - rezerv;
                var faiz    = plan > 0 ? Math.Round((faktiki + rezerv) / plan * 100, 1) : 0m;
                return new { ay, plan, faktiki, rezerv, azad, faiz };
            }).ToList();

            return new
            {
                departamentId  = d.Id,
                departamentAd  = d.Ad,
                aylar,
                toplamPlan     = aylar.Sum(a => a.plan),
                toplamFaktiki  = aylar.Sum(a => a.faktiki),
                toplamRezerv   = aylar.Sum(a => a.rezerv),
                toplamAzad     = aylar.Sum(a => a.azad)
            };
        }).ToList();

        return Json(new
        {
            departamentlar = data,
            umumiPlan      = data.Sum(x => x.toplamPlan),
            umumiFaktiki   = data.Sum(x => x.toplamFaktiki),
            umumiRezerv    = data.Sum(x => x.toplamRezerv),
            umumiAzad      = data.Sum(x => x.toplamAzad)
        });
    }

    // ── GET /HR/Budce/GetLimitInfo ──────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetLimitInfo(int departamentId, int il, int ay)
    {
        var budce = await _unitOfWork.Repository<Budce>()
            .GetirAsync(x => !x.Silinib && x.DepartamentId == departamentId && x.Il == il && x.Ay == ay);

        var plan = budce?.PlanMebleg ?? 0;

        var faktiki = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib && x.Status == XercStatus.Odenildi
                     && x.DepartamentId == departamentId
                     && x.XercTarixi.Year == il && x.XercTarixi.Month == ay)
            .SumAsync(x => (decimal?)x.Mebleg) ?? 0;

        var rezerv = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib
                     && (x.Status == XercStatus.SobeReisiTesdiq || x.Status == XercStatus.HrTesdiq)
                     && x.DepartamentId == departamentId
                     && x.XercTarixi.Year == il && x.XercTarixi.Month == ay)
            .SumAsync(x => (decimal?)x.Mebleg) ?? 0;

        var azad = plan - faktiki - rezerv;
        var faiz = plan > 0 ? Math.Round((faktiki + rezerv) / plan * 100, 1) : 0m;

        return Json(new { plan, faktiki, rezerv, azad, faiz, yoxdur = plan == 0 });
    }

    // ── GET /HR/Budce/GetDetay ──────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetDetay(int departamentId, int il, int ay)
    {
        var xercler = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib && x.DepartamentId == departamentId
                     && x.XercTarixi.Year == il && x.XercTarixi.Month == ay)
            .Include(x => x.Kateqoriya)
            .Include(x => x.Isci)
            .OrderByDescending(x => x.XercTarixi)
            .ToListAsync();

        var data = xercler.Select(x => new
        {
            id       = x.Id,
            tarix    = x.XercTarixi.ToString("dd.MM.yyyy"),
            kateqoriya = x.Kateqoriya?.Ad ?? "—",
            tesvir   = x.Tesvir,
            mebleg   = x.Mebleg,
            status   = (int)x.Status,
            statusAd = x.Status switch
            {
                XercStatus.Muraciet        => "Müraciət",
                XercStatus.SobeReisiTesdiq => "Şöbə rəisi təsdiqi",
                XercStatus.HrTesdiq        => "HR təsdiqi",
                XercStatus.Odenildi        => "Ödənildi",
                XercStatus.ImtinaEdildi    => "İmtina",
                _ => "—"
            },
            isRezerv    = x.Status == XercStatus.SobeReisiTesdiq || x.Status == XercStatus.HrTesdiq,
            isci     = x.Isci != null ? x.Isci.TamAd : (x.ManualGiris ? "Manual giriş" : "—"),
            manualGiris = x.ManualGiris,
            qebzYolu = x.QebzFaylYolu
        }).ToList();

        return Json(new
        {
            xercler        = data,
            odenilenToplam = xercler.Where(x => x.Status == XercStatus.Odenildi).Sum(x => x.Mebleg),
            rezervToplam   = xercler.Where(x => x.Status == XercStatus.SobeReisiTesdiq || x.Status == XercStatus.HrTesdiq).Sum(x => x.Mebleg)
        });
    }

    // ── POST /HR/Budce/Create ───────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] BudceCreateDto dto)
    {
        if (dto.DepartamentId <= 0 || dto.Il < 2020)
            return BadRequest(new { message = "Xəta: yanlış məlumat." });

        var aylar = dto.TopluTetbiq
            ? Enumerable.Range(1, 12)
            : Enumerable.Range(dto.Ay, 1);

        foreach (var ay in aylar)
        {
            if (ay < 1 || ay > 12) continue;

            var existing = await _unitOfWork.Repository<Budce>()
                .GetirAsync(x => !x.Silinib
                    && x.DepartamentId == dto.DepartamentId
                    && x.Il == dto.Il && x.Ay == ay);

            if (existing != null)
            {
                existing.PlanMebleg = dto.PlanMebleg;
                existing.Qeyd       = dto.Qeyd;
                await _unitOfWork.Repository<Budce>().YenileAsync(existing);
            }
            else
            {
                await _unitOfWork.Repository<Budce>().YaratAsync(new Budce
                {
                    DepartamentId = dto.DepartamentId,
                    Il            = dto.Il,
                    Ay            = ay,
                    PlanMebleg    = dto.PlanMebleg,
                    FaktikiMebleg = 0,
                    Qeyd          = dto.Qeyd
                });
            }
        }

        await _unitOfWork.YaddaSaxlaAsync();
        var msg = dto.TopluTetbiq
            ? $"Bütün 12 ay üçün {dto.PlanMebleg:N2} ₼ plan yadda saxlanıldı."
            : "Uğurla yadda saxlanıldı.";
        return Ok(new { message = msg });
    }

    // ── GET /HR/Budce/GetSirketBudce?il=2026 ───────────────────
    [HttpGet]
    public async Task<IActionResult> GetSirketBudce(int il)
    {
        var sb = await _unitOfWork.Repository<SirketBudcesi>()
            .GetirAsync(x => !x.Silinib && x.Il == il);

        var umumiMebleg = sb?.Mebleg ?? 0;

        var bolusdurulub = await _unitOfWork.Repository<Budce>()
            .Query()
            .Where(x => !x.Silinib && x.Il == il)
            .SumAsync(x => (decimal?)x.PlanMebleg) ?? 0;

        var qaliq = umumiMebleg - bolusdurulub;

        return Json(new
        {
            yoxdur       = umumiMebleg == 0,
            mebleg       = umumiMebleg,
            bolusdurulub,
            qaliq,
            faiz         = umumiMebleg > 0 ? Math.Round(bolusdurulub / umumiMebleg * 100, 1) : 0m,
            qeyd         = sb?.Qeyd
        });
    }

    // ── POST /HR/Budce/SetSirketBudce ───────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetSirketBudce([FromBody] SirketBudcesiDto dto)
    {
        if (dto.Il < 2020 || dto.Mebleg < 0)
            return BadRequest(new { message = "Yanlış məlumat." });

        var existing = await _unitOfWork.Repository<SirketBudcesi>()
            .GetirAsync(x => !x.Silinib && x.Il == dto.Il);

        if (existing != null)
        {
            existing.Mebleg = dto.Mebleg;
            existing.Qeyd   = dto.Qeyd;
            await _unitOfWork.Repository<SirketBudcesi>().YenileAsync(existing);
        }
        else
        {
            await _unitOfWork.Repository<SirketBudcesi>().YaratAsync(new SirketBudcesi
            {
                Il     = dto.Il,
                Mebleg = dto.Mebleg,
                Qeyd   = dto.Qeyd
            });
        }

        await _unitOfWork.YaddaSaxlaAsync();
        return Ok(new { message = "Şirkət büdcəsi yadda saxlanıldı." });
    }

    // ── POST /HR/Budce/BereberBol ───────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BereberBol([FromBody] BereberBolDto dto)
    {
        if (dto.Il < 2020 || dto.Ay < 1 || dto.Ay > 12 || dto.Mebleg <= 0)
            return BadRequest(new { message = "Yanlış məlumat." });

        var departamentlar = await _unitOfWork.Repository<Departament>()
            .Query().Where(x => !x.Silinib).ToListAsync();

        if (departamentlar.Count == 0)
            return Ok(new { message = "Departament tapılmadı." });

        var pay = Math.Round(dto.Mebleg / departamentlar.Count, 2);

        foreach (var d in departamentlar)
        {
            var existing = await _unitOfWork.Repository<Budce>()
                .GetirAsync(x => !x.Silinib && x.DepartamentId == d.Id && x.Il == dto.Il && x.Ay == dto.Ay);

            if (existing != null)
            {
                existing.PlanMebleg = pay;
                await _unitOfWork.Repository<Budce>().YenileAsync(existing);
            }
            else
            {
                await _unitOfWork.Repository<Budce>().YaratAsync(new Budce
                {
                    DepartamentId = d.Id,
                    Il            = dto.Il,
                    Ay            = dto.Ay,
                    PlanMebleg    = pay,
                    FaktikiMebleg = 0
                });
            }
        }

        await _unitOfWork.YaddaSaxlaAsync();
        return Ok(new { message = $"{departamentlar.Count} şöbəyə {pay:N2} ₼ paylandı." });
    }

    // ── POST /HR/Budce/SifirlaPlans ─────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SifirlaPlans([FromBody] SifirlaDto dto)
    {
        if (dto.Il < 2020) return BadRequest();

        var planlar = await _unitOfWork.Repository<Budce>()
            .Query().Where(x => !x.Silinib && x.Il == dto.Il).ToListAsync();

        foreach (var p in planlar)
        {
            p.PlanMebleg = 0;
            await _unitOfWork.Repository<Budce>().YenileAsync(p);
        }

        await _unitOfWork.YaddaSaxlaAsync();
        return Ok(new { message = $"{planlar.Count} plan sıfırlandı." });
    }

    // ── POST /HR/Budce/RestorePlans ──────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestorePlans([FromBody] List<RestorePlanItem> items)
    {
        if (items == null || items.Count == 0)
            return BadRequest(new { message = "Bərpa məlumatı yoxdur." });

        foreach (var item in items)
        {
            if (item.DepartamentId <= 0 || item.Il < 2020 || item.Ay < 1 || item.Ay > 12) continue;

            var existing = await _unitOfWork.Repository<Budce>()
                .GetirAsync(x => !x.Silinib
                    && x.DepartamentId == item.DepartamentId
                    && x.Il == item.Il && x.Ay == item.Ay);

            if (existing != null)
            {
                existing.PlanMebleg = item.PlanMebleg;
                await _unitOfWork.Repository<Budce>().YenileAsync(existing);
            }
            else if (item.PlanMebleg > 0)
            {
                await _unitOfWork.Repository<Budce>().YaratAsync(new Budce
                {
                    DepartamentId = item.DepartamentId,
                    Il            = item.Il,
                    Ay            = item.Ay,
                    PlanMebleg    = item.PlanMebleg,
                    FaktikiMebleg = 0
                });
            }
        }

        await _unitOfWork.YaddaSaxlaAsync();
        return Ok(new { message = "Planlar bərpa edildi." });
    }

    // ── GET /HR/Budce/ExportExcel?il=2026 ───────────────────
    [HttpGet]
    public async Task<IActionResult> ExportExcel(int il)
    {
        var budceler = await _unitOfWork.Repository<Budce>()
            .Query().Where(x => !x.Silinib && x.Il == il)
            .Include(x => x.Departament).ToListAsync();

        var departamentlar = await _unitOfWork.Repository<Departament>()
            .Query().Where(x => !x.Silinib).OrderBy(x => x.Ad).ToListAsync();

        var odenilenXercler = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib && x.Status == XercStatus.Odenildi
                     && x.XercTarixi.Year == il && x.DepartamentId.HasValue)
            .Select(x => new { x.DepartamentId, x.Mebleg, Month = x.XercTarixi.Month })
            .ToListAsync();

        var rezervXercler = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib
                     && (x.Status == XercStatus.SobeReisiTesdiq || x.Status == XercStatus.HrTesdiq)
                     && x.XercTarixi.Year == il && x.DepartamentId.HasValue)
            .Select(x => new { x.DepartamentId, x.Mebleg, Month = x.XercTarixi.Month })
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Büdcə {il}");

        string[] ayAdlari = { "Yan", "Fev", "Mar", "Apr", "May", "İyn", "İyl", "Avq", "Sen", "Okt", "Noy", "Dek" };

        // Header
        ws.Cell(1, 1).Value = "Departament";
        for (int i = 0; i < 12; i++)
        {
            ws.Cell(1, 2 + i * 4).Value = $"{ayAdlari[i]} Plan";
            ws.Cell(1, 3 + i * 4).Value = $"{ayAdlari[i]} Faktiki";
            ws.Cell(1, 4 + i * 4).Value = $"{ayAdlari[i]} Rezerv";
            ws.Cell(1, 5 + i * 4).Value = $"{ayAdlari[i]} Azad";
        }
        int lastCol = 50;
        ws.Cell(1, lastCol).Value     = "Cəmi Plan";
        ws.Cell(1, lastCol + 1).Value = "Cəmi Faktiki";
        ws.Cell(1, lastCol + 2).Value = "Cəmi Rezerv";
        ws.Cell(1, lastCol + 3).Value = "Cəmi Azad";

        var hRange = ws.Range(1, 1, 1, lastCol + 3);
        hRange.Style.Font.Bold = true;
        hRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e2a3b");
        hRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        decimal gtPlan = 0, gtFakt = 0, gtRezерv = 0;

        foreach (var d in departamentlar)
        {
            ws.Cell(row, 1).Value = d.Ad;
            decimal topPlan = 0, topFakt = 0, topRez = 0;

            for (int ay = 1; ay <= 12; ay++)
            {
                var b       = budceler.FirstOrDefault(x => x.DepartamentId == d.Id && x.Ay == ay);
                var plan    = b?.PlanMebleg ?? 0;
                var fakt    = odenilenXercler.Where(x => x.DepartamentId == d.Id && x.Month == ay).Sum(x => x.Mebleg);
                var rez     = rezervXercler.Where(x => x.DepartamentId == d.Id && x.Month == ay).Sum(x => x.Mebleg);
                var azad    = plan - fakt - rez;
                int col     = 2 + (ay - 1) * 4;

                SetNum(ws, row, col,     plan);
                SetNum(ws, row, col + 1, fakt);
                SetNum(ws, row, col + 2, rez);
                SetNum(ws, row, col + 3, azad);

                // Şərti rəngləmə
                if (plan > 0)
                {
                    var pct = (fakt + rez) / plan;
                    if (pct >= 1m)      ws.Cell(row, col + 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
                    else if (pct >= 0.8m) ws.Cell(row, col + 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF9C3");
                }
                if (azad < 0) ws.Cell(row, col + 3).Style.Font.FontColor = XLColor.Red;

                topPlan += plan; topFakt += fakt; topRez += rez;
            }

            SetNum(ws, row, lastCol,     topPlan);
            SetNum(ws, row, lastCol + 1, topFakt);
            SetNum(ws, row, lastCol + 2, topRez);
            var topAzad = topPlan - topFakt - topRez;
            SetNum(ws, row, lastCol + 3, topAzad);
            if (topAzad < 0) ws.Cell(row, lastCol + 3).Style.Font.FontColor = XLColor.Red;

            gtPlan += topPlan; gtFakt += topFakt; gtRezерv += topRez;
            row++;
        }

        // Cəm sətri
        ws.Cell(row, 1).Value = "CƏMİ";
        ws.Cell(row, 1).Style.Font.Bold = true;
        SetNum(ws, row, lastCol,     gtPlan,   bold: true);
        SetNum(ws, row, lastCol + 1, gtFakt,   bold: true);
        SetNum(ws, row, lastCol + 2, gtRezерv, bold: true);
        SetNum(ws, row, lastCol + 3, gtPlan - gtFakt - gtRezерv, bold: true);

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Budce_{il}.xlsx");
    }

    private static void SetNum(IXLWorksheet ws, int row, int col, decimal val, bool bold = false)
    {
        ws.Cell(row, col).Value = val;
        ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
        if (bold) ws.Cell(row, col).Style.Font.Bold = true;
    }
}

public class BudceCreateDto
{
    public int     DepartamentId { get; set; }
    public int     Il            { get; set; }
    public int     Ay            { get; set; }
    public decimal PlanMebleg    { get; set; }
    public string? Qeyd          { get; set; }
    public bool    TopluTetbiq   { get; set; }
}

public class SirketBudcesiDto
{
    public int     Il     { get; set; }
    public decimal Mebleg { get; set; }
    public string? Qeyd   { get; set; }
}

public class BereberBolDto
{
    public int     Il     { get; set; }
    public int     Ay     { get; set; }
    public decimal Mebleg { get; set; }
}

public class SifirlaDto { public int Il { get; set; } }

public class RestorePlanItem
{
    public int     DepartamentId { get; set; }
    public int     Il            { get; set; }
    public int     Ay            { get; set; }
    public decimal PlanMebleg    { get; set; }
}
