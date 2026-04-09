using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class XercController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public XercController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── GET /User/Xerc ──────────────────────────────────────
    public IActionResult Index()
    {
        ViewData["Title"] = "Xerclerim";
        return View();
    }

    // ── GET /User/Xerc/Create ───────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "Yeni Xerc Muraciet";

        var kateqoriyalar = await _unitOfWork.Repository<XercKateqoriyasi>()
            .Query()
            .Where(x => !x.Silinib && x.Aktivdir)
            .OrderBy(x => x.Ad)
            .ToListAsync();

        ViewBag.Kateqoriyalar = kateqoriyalar;
        return View();
    }

    // ── POST /User/Xerc/Create ──────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(XercCreateDto dto, IFormFile? qebzFayl)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isci = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        if (isci == null)
            return RedirectToAction("Index");

        string? faylYolu = null;
        if (qebzFayl != null && qebzFayl.Length > 0)
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "xercler");
            Directory.CreateDirectory(uploadsDir);
            var faylAd = $"{Guid.NewGuid()}{Path.GetExtension(qebzFayl.FileName)}";
            var tam = Path.Combine(uploadsDir, faylAd);
            using var stream = new FileStream(tam, FileMode.Create);
            await qebzFayl.CopyToAsync(stream);
            faylYolu = $"/uploads/xercler/{faylAd}";
        }

        var xerc = new Xerc
        {
            IsciId = isci.Id,
            KateqoriyaId = dto.KateqoriyaId,
            Tesvir = dto.Tesvir,
            Mebleg = dto.Mebleg,
            XercTarixi = dto.XercTarixi,
            QebzFaylYolu = faylYolu,
            Status = XercStatus.Muraciet
        };

        await _unitOfWork.Repository<Xerc>().YaratAsync(xerc);
        await _unitOfWork.YaddaSaxlaAsync();

        TempData["Success"] = "Xerc muracietiniz ugurla gonderildi.";
        return RedirectToAction("Index");
    }

    // ── GET /User/Xerc/GetMyXercler ─────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetMyXercler()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isci = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        if (isci == null)
            return Json(new { xercler = Array.Empty<object>() });

        var xercler = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib && x.IsciId == isci.Id)
            .Include(x => x.Kateqoriya)
            .OrderByDescending(x => x.XercTarixi)
            .ToListAsync();

        var data = xercler.Select(x => new
        {
            id = x.Id,
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
            qebzFaylYolu = x.QebzFaylYolu,
            imtinaSebebi = x.ImtinaSebebi
        });

        return Json(new { xercler = data });
    }
}

// ── DTO ─────────────────────────────────────────────────────
public class XercCreateDto
{
    public int KateqoriyaId { get; set; }
    public string Tesvir { get; set; } = null!;
    public decimal Mebleg { get; set; }
    public DateTime XercTarixi { get; set; }
}
