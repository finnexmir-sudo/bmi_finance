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

    public ElanController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── GET /HR/Elan ────────────────────────────────────────
    public IActionResult Index()
    {
        ViewData["Title"] = "Elan Idareetmesi";
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
    public async Task<IActionResult> Create(string Bashliq, string Metn, bool Vacibdir, DateTime? BitirmeTarixi)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isci = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        if (isci == null)
        {
            TempData["Error"] = "Isci tapilmadi.";
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

        await _unitOfWork.Repository<Elan>().YaratAsync(elan);
        await _unitOfWork.YaddaSaxlaAsync();

        TempData["Success"] = "Elan ugurla yaradildi.";
        return RedirectToAction("Index");
    }

    // ── POST /HR/Elan/Sil ───────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Sil(int id)
    {
        var elan = await _unitOfWork.Repository<Elan>().IdIleGetirAsync(id);
        if (elan == null)
            return NotFound(new { message = "Elan tapilmadi." });

        elan.Aktivdir = false;
        await _unitOfWork.Repository<Elan>().YenileAsync(elan);
        await _unitOfWork.YaddaSaxlaAsync();

        return Ok(new { message = "Elan deaktiv edildi." });
    }
}
