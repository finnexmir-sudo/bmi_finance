using ClosedXML.Excel;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinNex.UI.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber + ","
                 + RoleNames.SobeReisi + "," + RoleNames.Muhasib)]
public class XercController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBildirisRouter _bildirisRouter;
    private readonly IWebHostEnvironment _env;

    public XercController(IUnitOfWork unitOfWork, IBildirisRouter bildirisRouter, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _bildirisRouter = bildirisRouter;
        _env = env;
    }

    // ── GET /HR/Xerc ─────────────────────────────────────────
    public IActionResult Index()
    {
        ViewData["Title"] = "Xərc İdarəetməsi";
        var isMuhasib = User.IsInRole(RoleNames.Muhasib) || User.IsInRole(RoleNames.Admin)
                     || User.IsInRole(RoleNames.HR);
        ViewBag.CanManualCreate = isMuhasib;
        return View();
    }

    // ── GET /HR/Xerc/GetData ─────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetData(int? status, string? axtaris)
    {
        var query = _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib)
            .Include(x => x.Isci)
            .Include(x => x.Kateqoriya)
            .Include(x => x.Departament)
            .Include(x => x.TesdiqleyenIsci)
            .AsQueryable();

        if (status.HasValue && status >= 0)
            query = query.Where(x => (int)x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(axtaris))
            query = query.Where(x =>
                (x.Isci != null && (x.Isci.Ad.Contains(axtaris) || x.Isci.Soyad.Contains(axtaris))) ||
                (x.Departament != null && x.Departament.Ad.Contains(axtaris)) ||
                x.Tesvir.Contains(axtaris));

        var xercler = await query
            .OrderByDescending(x => x.XercTarixi)
            .ToListAsync();

        var isMuhasibOrAdmin = User.IsInRole(RoleNames.Muhasib) || User.IsInRole(RoleNames.Admin);

        var data = xercler.Select(x => new
        {
            id = x.Id,
            isciAd = x.Isci != null ? $"{x.Isci.Ad} {x.Isci.Soyad}" : null,
            departamentAd = x.Departament?.Ad,
            manualGiris = x.ManualGiris,
            kateqoriya = x.Kateqoriya?.Ad ?? "-",
            tesvir = x.Tesvir,
            mebleg = x.Mebleg,
            xercTarixi = x.XercTarixi.ToString("dd.MM.yyyy"),
            status = (int)x.Status,
            statusAd = x.Status switch
            {
                XercStatus.Muraciet       => "Müraciət",
                XercStatus.SobeReisiTesdiq => "Şöbə reisi təsdiq",
                XercStatus.HrTesdiq       => "HR təsdiq",
                XercStatus.Odenildi       => "Ödənildi",
                XercStatus.ImtinaEdildi   => "İmtina edildi",
                _ => "-"
            },
            tesdiqleyenAd = x.TesdiqleyenIsci != null
                ? $"{x.TesdiqleyenIsci.Ad} {x.TesdiqleyenIsci.Soyad}" : null,
            tesdiqTarixi = x.TesdiqTarixi?.ToString("dd.MM.yyyy"),
            imtinaSebebi = x.ImtinaSebebi,
            qebzFaylYolu = x.QebzFaylYolu,
            canOde = isMuhasibOrAdmin
        });

        return Json(new { xercler = data });
    }

    // ── POST /HR/Xerc/Tesdiqle ───────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Tesdiqle(int id)
    {
        var xerc = await _unitOfWork.Repository<Xerc>().IdIleGetirAsync(id);
        if (xerc == null) return NotFound(new { message = "Xərc tapılmadı." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tesdiqIsci = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        var oncekiStatus = xerc.Status;
        if (xerc.Status == XercStatus.Muraciet)
            xerc.Status = XercStatus.SobeReisiTesdiq;
        else if (xerc.Status == XercStatus.SobeReisiTesdiq)
            xerc.Status = XercStatus.HrTesdiq;

        xerc.TesdiqleyenIsciId = tesdiqIsci?.Id;
        xerc.TesdiqTarixi = DateTime.Now;

        await _unitOfWork.Repository<Xerc>().YenileAsync(xerc);
        await _unitOfWork.YaddaSaxlaAsync();

        if (xerc.IsciId.HasValue)
        {
            var mərhələ = xerc.Status == XercStatus.SobeReisiTesdiq ? "Şöbə rəisi" :
                          xerc.Status == XercStatus.HrTesdiq ? "HR" : "Təsdiqçi";
            await _bildirisRouter.NotifyIsciAsync(
                xerc.IsciId.Value,
                BildirisNovu.XercTesdiq,
                $"Xərc müraciəti — {mərhələ} təsdiqi",
                $"{xerc.Mebleg:N2} ₼ xərc müraciətiniz {mərhələ} tərəfindən təsdiqləndi.",
                redirectUrl: Url.Action("Index", "Xerc", new { area = "User" }));
        }

        if (oncekiStatus == XercStatus.Muraciet && xerc.Status == XercStatus.SobeReisiTesdiq)
        {
            await _bildirisRouter.NotifyRolesAsync(
                new[] { RoleNames.HR, RoleNames.Admin },
                BildirisNovu.XercMuraciet,
                "Xərc müraciəti — HR təsdiqi gözləyir",
                $"{xerc.Mebleg:N2} ₼ xərc Şöbə rəisi tərəfindən təsdiqlənib, HR təsdiqi gözləyir.",
                redirectUrl: Url.Action("Index", "Xerc", new { area = "HR" }),
                exceptIsciId: xerc.IsciId);
        }
        else if (xerc.Status == XercStatus.HrTesdiq)
        {
            await _bildirisRouter.NotifyRolesAsync(
                new[] { RoleNames.Muhasib, RoleNames.Admin },
                BildirisNovu.XercTesdiq,
                "Xərc — ödəniş gözləyir",
                $"{xerc.Mebleg:N2} ₼ xərc HR tərəfindən təsdiqlənib, ödəniş gözləyir.",
                redirectUrl: Url.Action("Index", "Xerc", new { area = "HR" }),
                exceptIsciId: xerc.IsciId);
        }

        return Ok(new { message = "Xərc təsdiqləndi." });
    }

    // ── POST /HR/Xerc/Imtina ─────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Imtina(int id, [FromBody] ImtinaDto dto)
    {
        var xerc = await _unitOfWork.Repository<Xerc>().IdIleGetirAsync(id);
        if (xerc == null) return NotFound(new { message = "Xərc tapılmadı." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tesdiqIsci = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        xerc.Status = XercStatus.ImtinaEdildi;
        xerc.ImtinaSebebi = dto.Sebeb;
        xerc.TesdiqleyenIsciId = tesdiqIsci?.Id;
        xerc.TesdiqTarixi = DateTime.Now;

        await _unitOfWork.Repository<Xerc>().YenileAsync(xerc);
        await _unitOfWork.YaddaSaxlaAsync();

        if (xerc.IsciId.HasValue)
        {
            var sebebMetn = string.IsNullOrWhiteSpace(dto.Sebeb) ? "" : $" Səbəb: {dto.Sebeb}";
            await _bildirisRouter.NotifyIsciAsync(
                xerc.IsciId.Value,
                BildirisNovu.XercImtina,
                "Xərc müraciəti rədd edildi",
                $"{xerc.Mebleg:N2} ₼ xərc müraciətiniz rədd edildi.{sebebMetn}",
                redirectUrl: Url.Action("Index", "Xerc", new { area = "User" }));
        }

        return Ok(new { message = "Xərc imtina edildi." });
    }

    // ── POST /HR/Xerc/Ode ────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Ode(int id)
    {
        if (!User.IsInRole(RoleNames.Muhasib) && !User.IsInRole(RoleNames.Admin))
            return Forbid();

        var xerc = await _unitOfWork.Repository<Xerc>().IdIleGetirAsync(id);
        if (xerc == null) return NotFound(new { message = "Xərc tapılmadı." });

        xerc.Status = XercStatus.Odenildi;
        xerc.TesdiqTarixi = DateTime.Now;

        await _unitOfWork.Repository<Xerc>().YenileAsync(xerc);
        await _unitOfWork.YaddaSaxlaAsync();

        if (xerc.IsciId.HasValue)
        {
            await _bildirisRouter.NotifyIsciAsync(
                xerc.IsciId.Value,
                BildirisNovu.XercOdenis,
                "Xərc ödənişi edildi",
                $"{xerc.Mebleg:N2} ₼ xərc ödənişiniz Mühasibiyyat tərəfindən həyata keçirildi.",
                redirectUrl: Url.Action("Index", "Xerc", new { area = "User" }));
        }

        return Ok(new { message = "Xərc ödənildi kimi işarələndi." });
    }

    // ── GET /HR/Xerc/ManualCreate ────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ManualCreate()
    {
        if (!User.IsInRole(RoleNames.Muhasib) && !User.IsInRole(RoleNames.Admin)
            && !User.IsInRole(RoleNames.HR))
            return Forbid();

        return await ManualCreateViewAsync();
    }

    // ── POST /HR/Xerc/ManualCreate ───────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManualCreate(ManualXercCreateDto dto, IFormFile? faktura)
    {
        if (!User.IsInRole(RoleNames.Muhasib) && !User.IsInRole(RoleNames.Admin)
            && !User.IsInRole(RoleNames.HR))
            return Forbid();

        string? faylYolu = null;
        if (faktura != null && faktura.Length > 0)
        {
            var ext = Path.GetExtension(faktura.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            if (!allowed.Contains(ext))
                ModelState.AddModelError("", "Yalnız PDF, JPG, PNG faylları qəbul edilir.");
            else if (faktura.Length > 10 * 1024 * 1024)
                ModelState.AddModelError("", "Fayl ölçüsü 10MB-dan çox ola bilməz.");
            else
                faylYolu = await SaveFakturaAsync(faktura, ext);
        }

        if (!ModelState.IsValid)
            return await ManualCreateViewAsync(dto);

        var xerc = new Xerc
        {
            IsciId = null,
            DepartamentId = dto.DepartamentId,
            ManualGiris = true,
            KateqoriyaId = dto.KateqoriyaId,
            Tesvir = dto.Tesvir.Trim(),
            Mebleg = dto.Mebleg,
            XercTarixi = dto.XercTarixi,
            QebzFaylYolu = faylYolu,
            Status = XercStatus.Odenildi,
            TesdiqTarixi = DateTime.Now
        };

        await _unitOfWork.Repository<Xerc>().YaratAsync(xerc);
        await _unitOfWork.YaddaSaxlaAsync();

        TempData["Success"] = "Xərc uğurla qeydə alındı.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> SaveFakturaAsync(IFormFile faktura, string ext)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "fakturalar");
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid()}{ext}";
        using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
        await faktura.CopyToAsync(stream);
        return $"/uploads/fakturalar/{fileName}";
    }

    private async Task<IActionResult> ManualCreateViewAsync(ManualXercCreateDto? dto = null)
    {
        ViewData["Title"] = "Manual Xərc Girişi";
        ViewBag.Kateqoriyalar = await _unitOfWork.Repository<XercKateqoriyasi>()
            .Query().Where(x => x.Aktivdir && !x.Silinib).OrderBy(x => x.Ad).ToListAsync();
        ViewBag.Departamentler = await _unitOfWork.Repository<Departament>()
            .Query().Where(x => !x.Silinib).OrderBy(x => x.Ad).ToListAsync();
        return View("ManualCreate", dto);
    }

    // ── GET /HR/Xerc/ExportExcel ─────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ExportExcel(int? status)
    {
        var query = _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib)
            .Include(x => x.Isci)
            .Include(x => x.Kateqoriya)
            .Include(x => x.Departament)
            .AsQueryable();

        if (status.HasValue && status >= 0)
            query = query.Where(x => (int)x.Status == status.Value);

        var xercler = await query.OrderByDescending(x => x.XercTarixi).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Xercler");

        ws.Cell(1, 1).Value = "İşçi / Şöbə";
        ws.Cell(1, 2).Value = "Kateqoriya";
        ws.Cell(1, 3).Value = "Təsvir";
        ws.Cell(1, 4).Value = "Məbləğ (AZN)";
        ws.Cell(1, 5).Value = "Xərc tarixi";
        ws.Cell(1, 6).Value = "Status";
        ws.Cell(1, 7).Value = "Növ";

        var headerRange = ws.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e2a3b");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var x in xercler)
        {
            var kim = x.ManualGiris
                ? (x.Departament?.Ad ?? "—")
                : (x.Isci != null ? $"{x.Isci.Ad} {x.Isci.Soyad}" : "—");
            ws.Cell(row, 1).Value = kim;
            ws.Cell(row, 2).Value = x.Kateqoriya?.Ad ?? "-";
            ws.Cell(row, 3).Value = x.Tesvir;
            ws.Cell(row, 4).Value = x.Mebleg;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Value = x.XercTarixi.ToString("dd.MM.yyyy");
            ws.Cell(row, 6).Value = x.Status switch
            {
                XercStatus.Muraciet        => "Müraciət",
                XercStatus.SobeReisiTesdiq => "Şöbə reisi təsdiq",
                XercStatus.HrTesdiq        => "HR təsdiq",
                XercStatus.Odenildi        => "Ödənildi",
                XercStatus.ImtinaEdildi    => "İmtina edildi",
                _ => "-"
            };
            ws.Cell(row, 7).Value = x.ManualGiris ? "Manual" : "İşçi müraciəti";
            row++;
        }

        ws.Cell(row, 1).Value = "CƏMİ";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = xercler.Sum(x => x.Mebleg);
        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 4).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Xercler_Hesabat.xlsx");
    }
}

public class ImtinaDto
{
    public string Sebeb { get; set; } = null!;
}

public class ManualXercCreateDto
{
    public int KateqoriyaId { get; set; }
    public int? DepartamentId { get; set; }
    public string Tesvir { get; set; } = null!;
    public decimal Mebleg { get; set; }
    public DateTime XercTarixi { get; set; } = DateTime.Today;
}
