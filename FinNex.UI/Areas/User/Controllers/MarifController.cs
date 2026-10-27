using FinNex.Application.DTOs.Marif;
using FinNex.Application.Interfaces.Marif;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinNex.UI.Areas.User.Controllers;

/// <summary>Daxili Maarifləndirmə — işçinin özünə göndərilən sual dəstlərini görüb cavablaması (27.10.2026, pilot).</summary>
[Area("User")]
[Authorize]
public class MarifController : Controller
{
    private readonly IMarifService _marif;
    private readonly IUnitOfWork _unitOfWork;

    public MarifController(IMarifService marif, IUnitOfWork unitOfWork)
    {
        _marif = marif;
        _unitOfWork = unitOfWork;
    }

    private async Task<Isci?> CariIsciAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _unitOfWork.Repository<Isci>().GetirAsync(x => x.AppUserId == userId && !x.Silinib);
    }

    // ── GET /User/Marif ────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Maarifləndirmə";
        var isci = await CariIsciAsync();
        if (isci == null) return View(new List<MarifIsciTesyinatListDto>());

        var list = await _marif.IsciTesyinatlariAsync(isci.Id);
        return View(list);
    }

    // ── GET /User/Marif/Sual/5 (tesyinatId) ────────────────────────────
    public async Task<IActionResult> Sual(int id)
    {
        var isci = await CariIsciAsync();
        if (isci == null)
        {
            TempData["Error"] = "İşçi tapılmadı.";
            return RedirectToAction(nameof(Index));
        }

        var set = await _marif.IsciSualSetiGetirAsync(id, isci.Id);
        if (set == null)
        {
            TempData["Error"] = "Test tapılmadı və ya sizə aid deyil.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = set.Basliq;
        return View(set);
    }

    // ── POST /User/Marif/CavabVer (AJAX) ───────────────────────────────
    [HttpPost]
    public async Task<IActionResult> CavabVer([FromBody] MarifCavabVerDto dto)
    {
        var isci = await CariIsciAsync();
        if (isci == null) return Json(new { ok = false, xeta = "İşçi tapılmadı." });

        var netice = await _marif.CavabVerAsync(dto, isci.Id);
        if (!netice.Success) return Json(new { ok = false, xeta = netice.Message });

        return Json(new { ok = true, netice = netice.Data });
    }
}
