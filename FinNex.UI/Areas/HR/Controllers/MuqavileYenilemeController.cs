using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.HR.ViewModels.MuqavileYenileme;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
    public class MuqavileYenilemeController : Controller
    {
        private readonly AppDbContext _db;

        public MuqavileYenilemeController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Yenile(int isciId)
        {
            var vm = await BuildVmAsync(isciId);
            if (vm == null)
            {
                TempData["Error"] = "İşçi tapılmadı.";
                return RedirectToAction("Index", "MuqavileBitme");
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yenile(MuqavileYenilemeVM vm)
        {
            if (!ModelState.IsValid)
            {
                var fresh = await BuildVmAsync(vm.IsciId);
                if (fresh != null)
                {
                    vm.IsciTamAd     = fresh.IsciTamAd;
                    vm.DepartamentAd = fresh.DepartamentAd;
                    vm.VezifeAd      = fresh.VezifeAd;
                    vm.KohneBitmeTarixi = fresh.KohneBitmeTarixi;
                    vm.Tarixce       = fresh.Tarixce;
                }
                return View(vm);
            }

            var teyinat = await _db.IsciTeyinatlari
                .FirstOrDefaultAsync(t => t.IsciId == vm.IsciId && t.Aktivdir && !t.Silinib);

            if (teyinat == null)
            {
                ModelState.AddModelError("", "Aktiv təyinat tapılmadı.");
                var fresh = await BuildVmAsync(vm.IsciId);
                if (fresh != null) { vm.IsciTamAd = fresh.IsciTamAd; vm.Tarixce = fresh.Tarixce; }
                return View(vm);
            }

            var yenileme = new MuqavileYenileme
            {
                IsciId           = vm.IsciId,
                KohneBitmeTarixi = teyinat.BitmeTarixi,
                YeniBitmeTarixi  = vm.YeniBitmeTarixi!.Value,
                YenilemeTarixi   = DateTime.Now,
                Qeyd             = vm.Qeyd?.Trim()
            };

            teyinat.BitmeTarixi     = vm.YeniBitmeTarixi!.Value;
            teyinat.YenilenmeTarixi = DateTime.Now;

            _db.MuqavileYenilemeleri.Add(yenileme);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Müqavilə {vm.YeniBitmeTarixi.Value:dd.MM.yyyy} tarixinə qədər uzadıldı.";
            return RedirectToAction(nameof(Yenile), new { isciId = vm.IsciId });
        }

        private async Task<MuqavileYenilemeVM?> BuildVmAsync(int isciId)
        {
            var teyinat = await _db.IsciTeyinatlari
                .Include(t => t.Isci)
                .Include(t => t.Departament)
                .Include(t => t.Vezife)
                .FirstOrDefaultAsync(t => t.IsciId == isciId && t.Aktivdir && !t.Silinib);

            if (teyinat?.Isci == null) return null;

            var tarixce = await _db.MuqavileYenilemeleri
                .AsNoTracking()
                .Where(y => y.IsciId == isciId && !y.Silinib)
                .OrderByDescending(y => y.YenilemeTarixi)
                .Select(y => new MuqavileYenilemeRowVM
                {
                    Id               = y.Id,
                    KohneBitmeTarixi = y.KohneBitmeTarixi,
                    YeniBitmeTarixi  = y.YeniBitmeTarixi,
                    YenilemeTarixi   = y.YenilemeTarixi,
                    Qeyd             = y.Qeyd
                })
                .ToListAsync();

            return new MuqavileYenilemeVM
            {
                IsciId          = isciId,
                IsciTamAd       = $"{teyinat.Isci.Ad} {teyinat.Isci.Soyad} {teyinat.Isci.AtaAdi}".Trim(),
                DepartamentAd   = teyinat.Departament?.Ad ?? "—",
                VezifeAd        = teyinat.Vezife?.Ad ?? "—",
                KohneBitmeTarixi = teyinat.BitmeTarixi,
                Tarixce         = tarixce
            };
        }
    }
}
