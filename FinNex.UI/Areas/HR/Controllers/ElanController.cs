using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinNex.UI.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
public class ElanController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] SekilTipler = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] FaylTipler = { ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
    private const long MaxFaylOlcusu = 10 * 1024 * 1024; // 10 MB

    public ElanController(IUnitOfWork unitOfWork, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _env = env;
    }

    // ── GET /HR/Elan ────────────────────────────────────────
    public IActionResult Index()
    {
        ViewData["Title"] = "Elan İdarəetməsi";
        return View();
    }

    // ── GET /HR/Elan/GetData ────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetData()
    {
        var elanlar = await _unitOfWork.Repository<Elan>()
            .Query()
            .Where(x => !x.Silinib)
            .Include(x => x.GonderenIsci)
            .OrderByDescending(x => x.YaradilmaTarixi)
            .ToListAsync();

        var data = elanlar.Select(e => new
        {
            id = e.Id,
            bashliq = e.Bashliq,
            metn = e.Metn,
            gonderenAd = e.GonderenIsci != null ? $"{e.GonderenIsci.Ad} {e.GonderenIsci.Soyad}" : "-",
            vacibdir = e.Vacibdir,
            bitirmeTarixi = e.BitirmeTarixi?.ToString("dd.MM.yyyy"),
            aktivdir = e.Aktivdir,
            sekilVar = !string.IsNullOrEmpty(e.SekilYolu),
            faylVar = !string.IsNullOrEmpty(e.FaylAdi),
            yaradilma = e.YaradilmaTarixi.ToString("dd.MM.yyyy HH:mm")
        });

        return Json(new { elanlar = data });
    }

    // ── GET /HR/Elan/Create ─────────────────────────────────
    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Yeni Elan";
        return View();
    }

    // ── POST /HR/Elan/Create ────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string Bashliq, string Metn, bool Vacibdir,
        DateTime? BitirmeTarixi, IFormFile? sekil, IFormFile? fayl)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isci = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        if (isci == null)
        {
            TempData["Error"] = "İşçi tapılmadı.";
            return RedirectToAction("Index");
        }

        var elan = new Elan
        {
            Bashliq = Bashliq,
            Metn = Metn,
            GonderenIsciId = isci.Id,
            Vacibdir = Vacibdir,
            BitirmeTarixi = BitirmeTarixi,
            Aktivdir = true
        };

        // Şəkil yüklə
        if (sekil != null && sekil.Length > 0)
        {
            var ext = Path.GetExtension(sekil.FileName).ToLowerInvariant();
            if (SekilTipler.Contains(ext) && sekil.Length <= MaxFaylOlcusu)
            {
                var dir = Path.Combine(_env.WebRootPath, "uploads", "elan");
                Directory.CreateDirectory(dir);
                var name = $"{Guid.NewGuid()}{ext}";
                using (var s = new FileStream(Path.Combine(dir, name), FileMode.Create))
                    await sekil.CopyToAsync(s);
                elan.SekilYolu = $"/uploads/elan/{name}";
            }
            else
            {
                TempData["Error"] = "Şəkil formatı düzgün deyil və ya 10MB-dan böyükdür.";
                return View();
            }
        }

        // Sənəd yüklə
        if (fayl != null && fayl.Length > 0)
        {
            var ext = Path.GetExtension(fayl.FileName).ToLowerInvariant();
            if (FaylTipler.Contains(ext) && fayl.Length <= MaxFaylOlcusu)
            {
                var dir = Path.Combine(_env.WebRootPath, "uploads", "elan");
                Directory.CreateDirectory(dir);
                var name = $"{Guid.NewGuid()}{ext}";
                using (var s = new FileStream(Path.Combine(dir, name), FileMode.Create))
                    await fayl.CopyToAsync(s);
                elan.FaylAdi = fayl.FileName;
                elan.FaylYolu = $"/uploads/elan/{name}";
                elan.FaylTipi = ext.TrimStart('.');
                elan.FaylOlcusu = fayl.Length;
            }
            else
            {
                TempData["Error"] = "Sənəd formatı düzgün deyil və ya 10MB-dan böyükdür.";
                return View();
            }
        }

        await _unitOfWork.Repository<Elan>().YaratAsync(elan);
        await _unitOfWork.YaddaSaxlaAsync();

        TempData["Success"] = "Elan uğurla yaradıldı.";
        return RedirectToAction("Index");
    }

    // ── POST /HR/Elan/Sil ───────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Sil(int id)
    {
        var elan = await _unitOfWork.Repository<Elan>().IdIleGetirAsync(id);
        if (elan == null)
            return NotFound(new { message = "Elan tapılmadı." });

        elan.Aktivdir = false;
        await _unitOfWork.Repository<Elan>().YenileAsync(elan);
        await _unitOfWork.YaddaSaxlaAsync();

        return Ok(new { message = "Elan deaktiv edildi." });
    }
}
