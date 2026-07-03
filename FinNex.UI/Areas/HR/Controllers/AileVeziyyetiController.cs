using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// İşçi ailə vəziyyəti — ailə vəziyyəti, tək valideyn və uşaqlar (doğum tarixi ilə).
    /// Məzuniyyət balansı M.117 əlavəsini bu datadan hesablayır (yaş avtomatik).
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class AileVeziyyetiController : Controller
    {
        private readonly IUnitOfWork _uow;

        public AileVeziyyetiController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // GET /HR/AileVeziyyeti
        public async Task<IActionResult> Index(string? axtaris)
        {
            var q = _uow.Repository<Isci>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .Include(x => x.Usaqlar.Where(u => !u.Silinib))
                .Include(x => x.IsciTeyinatlari.Where(t => t.Aktivdir && !t.Silinib))
                    .ThenInclude(t => t.Departament);

            if (!string.IsNullOrWhiteSpace(axtaris))
            {
                var a = axtaris.Trim();
                q = q.Where(x => (x.Ad + " " + x.Soyad).Contains(a));
            }

            var list = await q.OrderBy(x => x.Soyad).ThenBy(x => x.Ad).ToListAsync();
            ViewBag.Axtaris = axtaris;
            ViewData["Title"] = "İşçi ailə vəziyyəti";
            return View(list);
        }

        // GET /HR/AileVeziyyeti/Detay/5
        public async Task<IActionResult> Detay(int id)
        {
            var isci = await _uow.Repository<Isci>()
                .Query()
                .AsNoTracking()
                .Include(x => x.Usaqlar.Where(u => !u.Silinib))
                .Include(x => x.IsciTeyinatlari.Where(t => t.Aktivdir && !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

            if (isci == null)
            {
                TempData["Error"] = "İşçi tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = $"Ailə vəziyyəti — {isci.Ad} {isci.Soyad}";
            return View(isci);
        }

        // POST /HR/AileVeziyyeti/SaveAile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAile(int id, AileVeziyyeti aileVeziyyeti, bool tekValideyn)
        {
            var isci = await _uow.Repository<Isci>().IdIleGetirAsync(id);
            if (isci == null)
            {
                TempData["Error"] = "İşçi tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            // Tam entity yükləndi — yalnız iki sahə dəyişir, qalanı toxunulmaz.
            isci.AileVeziyyeti = aileVeziyyeti;
            isci.TekValideyn = tekValideyn;
            await _uow.Repository<Isci>().YenileAsync(isci);
            await _uow.YaddaSaxlaAsync();

            TempData["Success"] = "Ailə vəziyyəti yeniləndi.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        // POST /HR/AileVeziyyeti/UsaqElaveEt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsaqElaveEt(int isciId, string? ad, DateTime dogumTarixi, bool elillidir)
        {
            if (dogumTarixi == default || dogumTarixi.Date > DateTime.Today)
            {
                TempData["Error"] = "Doğum tarixi düzgün deyil (gələcək ola bilməz).";
                return RedirectToAction(nameof(Detay), new { id = isciId });
            }

            await _uow.Repository<IsciUsaq>().YaratAsync(new IsciUsaq
            {
                IsciId      = isciId,
                Ad          = string.IsNullOrWhiteSpace(ad) ? null : ad.Trim(),
                DogumTarixi = dogumTarixi.Date,
                Elillidir   = elillidir
            });
            await _uow.YaddaSaxlaAsync();

            TempData["Success"] = "Uşaq əlavə olundu.";
            return RedirectToAction(nameof(Detay), new { id = isciId });
        }

        // POST /HR/AileVeziyyeti/UsaqSil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsaqSil(int id, int isciId)
        {
            await _uow.Repository<IsciUsaq>().YumshakSilAsync(id);
            await _uow.YaddaSaxlaAsync();

            TempData["Success"] = "Uşaq silindi.";
            return RedirectToAction(nameof(Detay), new { id = isciId });
        }
    }
}
