using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
public class XercKateqoriyaController : Controller
{
    private readonly IUnitOfWork _uow;

    public XercKateqoriyaController(IUnitOfWork uow) => _uow = uow;

    // GET /HR/XercKateqoriya
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Xərc Kateqoriyaları";
        var list = await _uow.Repository<XercKateqoriyasi>()
            .Query().Where(x => !x.Silinib)
            .Include(x => x.Xercler)
            .OrderBy(x => x.Ad).ToListAsync();
        return View(list);
    }

    // POST /HR/XercKateqoriya/Yarat
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(string ad, string? ikon)
    {
        if (string.IsNullOrWhiteSpace(ad))
        {
            TempData["Error"] = "Ad boş ola bilməz.";
            return RedirectToAction(nameof(Index));
        }

        var movcud = await _uow.Repository<XercKateqoriyasi>()
            .GetirAsync(x => !x.Silinib && x.Ad.ToLower() == ad.Trim().ToLower());
        if (movcud != null)
        {
            TempData["Error"] = $"\"{ad}\" adlı kateqoriya artıq mövcuddur.";
            return RedirectToAction(nameof(Index));
        }

        await _uow.Repository<XercKateqoriyasi>().YaratAsync(new XercKateqoriyasi
        {
            Ad = ad.Trim(),
            Ikon = string.IsNullOrWhiteSpace(ikon) ? "bi-tag" : ikon.Trim(),
            Aktivdir = true
        });
        await _uow.YaddaSaxlaAsync();

        TempData["Success"] = $"\"{ad}\" kateqoriyası əlavə edildi.";
        return RedirectToAction(nameof(Index));
    }

    // POST /HR/XercKateqoriya/Yenile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yenile(int id, string ad, string? ikon, bool aktivdir)
    {
        var kat = await _uow.Repository<XercKateqoriyasi>().IdIleGetirAsync(id);
        if (kat == null || kat.Silinib)
        {
            TempData["Error"] = "Kateqoriya tapılmadı.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(ad))
        {
            TempData["Error"] = "Ad boş ola bilməz.";
            return RedirectToAction(nameof(Index));
        }

        var dublikat = await _uow.Repository<XercKateqoriyasi>()
            .GetirAsync(x => !x.Silinib && x.Id != id && x.Ad.ToLower() == ad.Trim().ToLower());
        if (dublikat != null)
        {
            TempData["Error"] = $"\"{ad}\" adlı kateqoriya artıq mövcuddur.";
            return RedirectToAction(nameof(Index));
        }

        kat.Ad = ad.Trim();
        kat.Ikon = string.IsNullOrWhiteSpace(ikon) ? "bi-tag" : ikon.Trim();
        kat.Aktivdir = aktivdir;

        await _uow.Repository<XercKateqoriyasi>().YenileAsync(kat);
        await _uow.YaddaSaxlaAsync();

        TempData["Success"] = $"\"{kat.Ad}\" kateqoriyası yeniləndi.";
        return RedirectToAction(nameof(Index));
    }

    // POST /HR/XercKateqoriya/Sil
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var kat = await _uow.Repository<XercKateqoriyasi>()
            .Query().Include(x => x.Xercler)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

        if (kat == null)
        {
            TempData["Error"] = "Kateqoriya tapılmadı.";
            return RedirectToAction(nameof(Index));
        }

        if (kat.Xercler.Any(x => !x.Silinib))
        {
            TempData["Error"] = $"\"{kat.Ad}\" kateqoriyasına aid xərclər mövcuddur, silinə bilməz.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _uow.Repository<XercKateqoriyasi>().YumshakSilAsync(id);
        if (result) await _uow.YaddaSaxlaAsync();

        TempData["Success"] = $"\"{kat.Ad}\" kateqoriyası silindi.";
        return RedirectToAction(nameof(Index));
    }
}
