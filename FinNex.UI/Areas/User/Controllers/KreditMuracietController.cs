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
        ViewBag.StatusSaylari = new Dictionary<int, int>
        {
            [0] = muracietler.Count(x => x.Status == KreditMuracietStatus.Yeni),
            [1] = muracietler.Count(x => x.Status == KreditMuracietStatus.Yoxlanilir),
            [2] = muracietler.Count(x => x.Status == KreditMuracietStatus.KomiteyeGonderildi),
            [3] = muracietler.Count(x => x.Status == KreditMuracietStatus.Tesdiqlenib),
            [4] = muracietler.Count(x => x.Status == KreditMuracietStatus.ReddEdilib)
        };

        // Status filter olduqda yenidən query et
        if (status.HasValue)
        {
            // saylar üçün ümumi siyahı lazımdır
            var hamisi = await _unitOfWork.Repository<KreditMuraciet>()
                .Query().Where(x => !x.Silinib).ToListAsync();
            ViewBag.StatusSaylari = new Dictionary<int, int>
            {
                [0] = hamisi.Count(x => x.Status == KreditMuracietStatus.Yeni),
                [1] = hamisi.Count(x => x.Status == KreditMuracietStatus.Yoxlanilir),
                [2] = hamisi.Count(x => x.Status == KreditMuracietStatus.KomiteyeGonderildi),
                [3] = hamisi.Count(x => x.Status == KreditMuracietStatus.Tesdiqlenib),
                [4] = hamisi.Count(x => x.Status == KreditMuracietStatus.ReddEdilib)
            };
        }

        return View(muracietler);
    }

    // ── GET /User/KreditMuraciet/Detail/5 ───────────────────
    public async Task<IActionResult> Detail(int id)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .Query()
            .Include(x => x.BaxanIsci)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

        if (muraciet == null) return NotFound();

        // FİN ilə əvvəlki müraciətləri tap
        var tarixce = new List<KreditMuraciet>();
        if (!string.IsNullOrEmpty(muraciet.FIN))
        {
            tarixce = await _unitOfWork.Repository<KreditMuraciet>()
                .Query()
                .Where(x => x.FIN == muraciet.FIN && x.Id != id && !x.Silinib)
                .OrderByDescending(x => x.MuracietTarixi)
                .ToListAsync();
        }

        ViewData["Title"] = "Müraciət #" + id;
        ViewBag.Tarixce = tarixce;
        return View(muraciet);
    }

    // ── POST /User/KreditMuraciet/ChangeStatus ──────────────
    [HttpPost]
    public async Task<IActionResult> ChangeStatus(int id, int yeniStatus, string? qeyd,
        string? komiteProtokolNo)
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

        // Komitə qərarı
        if (yeniStatus == (int)KreditMuracietStatus.Tesdiqlenib ||
            yeniStatus == (int)KreditMuracietStatus.ReddEdilib)
        {
            muraciet.KomiteQerari = yeniStatus == (int)KreditMuracietStatus.Tesdiqlenib ? "Təsdiqlənib" : "Rədd edilib";
            muraciet.KomiteProtokolNo = komiteProtokolNo;
            muraciet.KomiteQerarTarixi = DateTime.Now;
        }

        await _unitOfWork.Repository<KreditMuraciet>().YenileAsync(muraciet);
        await _unitOfWork.YaddaSaxlaAsync();

        TempData["Success"] = "Status yeniləndi.";
        return RedirectToAction("Detail", new { id });
    }

    // ── GET /User/KreditMuraciet/Komite ─────────────────────
    public async Task<IActionResult> Komite()
    {
        ViewData["Title"] = "Kredit Komitəsi";

        var muracietler = await _unitOfWork.Repository<KreditMuraciet>()
            .Query()
            .Where(x => !x.Silinib && x.Status == KreditMuracietStatus.KomiteyeGonderildi)
            .Include(x => x.BaxanIsci)
            .OrderByDescending(x => x.BaxilmaTarixi)
            .ToListAsync();

        return View(muracietler);
    }

    // ── GET /User/KreditMuraciet/FinSearch?fin=ABC ──────────
    [HttpGet]
    public async Task<IActionResult> FinSearch(string fin)
    {
        if (string.IsNullOrWhiteSpace(fin))
            return Json(new { tarixce = Array.Empty<object>() });

        var muracietler = await _unitOfWork.Repository<KreditMuraciet>()
            .Query()
            .Where(x => x.FIN == fin.Trim() && !x.Silinib)
            .OrderByDescending(x => x.MuracietTarixi)
            .ToListAsync();

        var data = muracietler.Select(m => new
        {
            id = m.Id,
            tarix = m.MuracietTarixi.ToString("dd.MM.yyyy"),
            mebleg = m.KreditMeblegi.ToString("N0") + " " + m.Valyuta,
            muddet = m.KreditMuddeti,
            status = m.Status.ToString(),
            qeyd = m.Qeyd
        });

        return Json(new { tarixce = data, say = muracietler.Count });
    }

    // ── POST /User/KreditMuraciet/Delete/5 ──────────────────
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .GetirAsync(x => x.Id == id);
        if (muraciet != null)
        {
            await _unitOfWork.Repository<KreditMuraciet>().DeleteAsync(muraciet.Id);
            await _unitOfWork.YaddaSaxlaAsync();
        }
        TempData["Success"] = "Müraciət silindi.";
        return RedirectToAction("Index");
    }
}
