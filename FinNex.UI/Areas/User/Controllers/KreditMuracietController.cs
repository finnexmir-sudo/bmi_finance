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

    private async Task<Isci?> GetCurrentIsciAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);
    }

    // ══════════════════════════════════════════════════════
    // İŞÇİ (Kredit Mütəxəssisi) — müraciətlərə baxır, yoxlayır, komitəyə göndərir
    // ══════════════════════════════════════════════════════

    // ── GET /User/KreditMuraciet ────────────────────────────
    public async Task<IActionResult> Index(int? status)
    {
        ViewData["Title"] = "Kredit Müraciətləri";

        var hamisi = await _unitOfWork.Repository<KreditMuraciet>()
            .Query().Where(x => !x.Silinib).ToListAsync();

        var muracietler = status.HasValue
            ? hamisi.Where(x => (int)x.Status == status.Value).ToList()
            : hamisi;

        ViewBag.SecilmisStatus = status;
        ViewBag.StatusSaylari = new Dictionary<int, int>
        {
            [0] = hamisi.Count(x => x.Status == KreditMuracietStatus.Yeni),
            [1] = hamisi.Count(x => x.Status == KreditMuracietStatus.Yoxlanilir),
            [2] = hamisi.Count(x => x.Status == KreditMuracietStatus.KomiteyeGonderildi),
            [3] = hamisi.Count(x => x.Status == KreditMuracietStatus.Tesdiqlenib),
            [4] = hamisi.Count(x => x.Status == KreditMuracietStatus.ReddEdilib)
        };

        return View(muracietler.OrderByDescending(x => x.MuracietTarixi).ToList());
    }

    // ── GET /User/KreditMuraciet/Detail/5 ───────────────────
    // İşçi baxışı — yoxlama + komitəyə göndərmə
    public async Task<IActionResult> Detail(int id)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .Query()
            .Include(x => x.BaxanIsci)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

        if (muraciet == null) return NotFound();

        // FİN tarixçəsi
        ViewBag.Tarixce = !string.IsNullOrEmpty(muraciet.FIN)
            ? await _unitOfWork.Repository<KreditMuraciet>()
                .Query()
                .Where(x => x.FIN == muraciet.FIN && x.Id != id && !x.Silinib)
                .OrderByDescending(x => x.MuracietTarixi)
                .ToListAsync()
            : new List<KreditMuraciet>();

        ViewData["Title"] = "Müraciət #" + id;
        return View(muraciet);
    }

    // ── POST /User/KreditMuraciet/IsciQiymetlendir ──────────
    // İşçi: yalnız Yeni → Yoxlanılır → Komitəyə göndər
    [HttpPost]
    public async Task<IActionResult> IsciQiymetlendir(int id, int yeniStatus, string? qeyd)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .GetirAsync(x => x.Id == id && !x.Silinib);
        if (muraciet == null) return NotFound();

        // İşçi yalnız bu statuslara dəyişə bilər
        if (yeniStatus > (int)KreditMuracietStatus.KomiteyeGonderildi)
        {
            TempData["Error"] = "Bu əməliyyat yalnız Kredit Komitəsinə aiddir.";
            return RedirectToAction("Detail", new { id });
        }

        var isci = await GetCurrentIsciAsync();

        muraciet.Status = (KreditMuracietStatus)yeniStatus;
        muraciet.Qeyd = qeyd;
        muraciet.BaxanIsciId = isci?.Id;
        muraciet.BaxilmaTarixi = DateTime.Now;

        await _unitOfWork.Repository<KreditMuraciet>().YenileAsync(muraciet);
        await _unitOfWork.YaddaSaxlaAsync();

        TempData["Success"] = yeniStatus == (int)KreditMuracietStatus.KomiteyeGonderildi
            ? "Müraciət Kredit Komitəsinə göndərildi."
            : "Status yeniləndi.";
        return RedirectToAction("Detail", new { id });
    }

    // ══════════════════════════════════════════════════════
    // KREDİT KOMİTƏSİ — qərar verir
    // ══════════════════════════════════════════════════════

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

    // ── GET /User/KreditMuraciet/KomiteDetail/5 ─────────────
    // Komitə baxışı — tam dosye + qərar formu
    public async Task<IActionResult> KomiteDetail(int id)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .Query()
            .Include(x => x.BaxanIsci)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

        if (muraciet == null) return NotFound();

        // FİN tarixçəsi
        ViewBag.Tarixce = !string.IsNullOrEmpty(muraciet.FIN)
            ? await _unitOfWork.Repository<KreditMuraciet>()
                .Query()
                .Where(x => x.FIN == muraciet.FIN && x.Id != id && !x.Silinib)
                .OrderByDescending(x => x.MuracietTarixi)
                .ToListAsync()
            : new List<KreditMuraciet>();

        ViewData["Title"] = "Komitə — Müraciət #" + id;
        return View(muraciet);
    }

    // ── POST /User/KreditMuraciet/KomiteQerar ───────────────
    // Komitə: yalnız Təsdiq / Rədd
    [HttpPost]
    public async Task<IActionResult> KomiteQerar(int id, int yeniStatus, string? qeyd,
        string? komiteProtokolNo)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .GetirAsync(x => x.Id == id && !x.Silinib);
        if (muraciet == null) return NotFound();

        // Komitə yalnız Təsdiq və ya Rədd edə bilər
        if (yeniStatus != (int)KreditMuracietStatus.Tesdiqlenib &&
            yeniStatus != (int)KreditMuracietStatus.ReddEdilib)
        {
            TempData["Error"] = "Komitə yalnız təsdiq və ya rədd edə bilər.";
            return RedirectToAction("KomiteDetail", new { id });
        }

        muraciet.Status = (KreditMuracietStatus)yeniStatus;
        muraciet.KomiteQerari = yeniStatus == (int)KreditMuracietStatus.Tesdiqlenib ? "Təsdiqlənib" : "Rədd edilib";
        muraciet.KomiteProtokolNo = komiteProtokolNo;
        muraciet.KomiteQerarTarixi = DateTime.Now;
        muraciet.Qeyd = qeyd;

        await _unitOfWork.Repository<KreditMuraciet>().YenileAsync(muraciet);
        await _unitOfWork.YaddaSaxlaAsync();

        TempData["Success"] = "Komitə qərarı qeydə alındı.";
        return RedirectToAction("Komite");
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
