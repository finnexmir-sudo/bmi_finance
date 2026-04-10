using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Kredit;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class KreditMuracietController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public KreditMuracietController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── GET /User/KreditMuraciet ────────────────────────────
    public async Task<IActionResult> Index(int? status)
    {
        ViewData["Title"] = "Kredit Müraciətləri";

        var query = _unitOfWork.Repository<KreditMuraciet>()
            .Query()
            .Where(x => !x.Silinib);

        if (status.HasValue)
            query = query.Where(x => (int)x.Status == status.Value);

        var muracietler = await query
            .OrderByDescending(x => x.MuracietTarixi)
            .ToListAsync();

        ViewBag.SecilmisStatus = status;
        return View(muracietler);
    }

    // ── GET /User/KreditMuraciet/Detail/5 ───────────────────
    public async Task<IActionResult> Detail(int id)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .GetirAsync(x => x.Id == id && !x.Silinib);

        if (muraciet == null) return NotFound();

        ViewData["Title"] = "Müraciət #" + id;
        return View(muraciet);
    }

    // ── POST /User/KreditMuraciet/ChangeStatus ──────────────
    [HttpPost]
    public async Task<IActionResult> ChangeStatus(int id, int yeniStatus, string? qeyd)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .GetirAsync(x => x.Id == id && !x.Silinib);

        if (muraciet == null) return NotFound();

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isci = await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);

        muraciet.Status = (KreditMuracietStatus)yeniStatus;
        muraciet.Qeyd = qeyd;
        muraciet.BaxanIsciId = isci?.Id;
        muraciet.BaxilmaTarixi = DateTime.Now;

        await _unitOfWork.Repository<KreditMuraciet>().YenileAsync(muraciet);
        await _unitOfWork.YaddaSaxlaAsync();

        TempData["Success"] = "Status yeniləndi.";
        return RedirectToAction("Detail", new { id });
    }
}
