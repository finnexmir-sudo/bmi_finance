using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class PerformansController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public PerformansController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null)
            {
                ViewBag.IsciId = 0;
                return View(new List<PerformansQiymetlendirme>());
            }
            var isciId = appUser.IsciId.Value;

            var list = await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query()
                .Where(x => !x.Silinib && (
                    x.IsciId == isciId ||
                    x.SobeReisiId == isciId ||
                    x.QiymetlendirenIsciId == isciId ||
                    x.Rehber2Id == isciId))
                .Include(x => x.Isci)
                .Include(x => x.QiymetlendirenIsci)
                .Include(x => x.Rehber2)
                .Include(x => x.SobeReisi)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            ViewBag.IsciId = isciId;
            ViewData["Title"] = "Performans Qiymətləndirmə";
            return View(list);
        }

        public async Task<IActionResult> Detal(int id)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return Forbid();
            var isciId = appUser.IsciId.Value;

            var performans = await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query()
                .Where(x => x.Id == id && !x.Silinib && (
                    x.IsciId == isciId ||
                    x.SobeReisiId == isciId ||
                    x.QiymetlendirenIsciId == isciId ||
                    x.Rehber2Id == isciId))
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Departament)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Vezife)
                .Include(x => x.QiymetlendirenIsci)
                .Include(x => x.Rehber2)
                .Include(x => x.SobeReisi)
                .Include(x => x.Kriteriyalar)
                .FirstOrDefaultAsync();

            if (performans == null) return NotFound();

            ViewBag.CurrentIsciId = isciId;
            ViewBag.IsIsci = performans.IsciId == isciId;
            ViewBag.IsSobeReisi = performans.SobeReisiId == isciId;
            ViewBag.IsRehber1 = performans.QiymetlendirenIsciId == isciId;
            ViewBag.IsRehber2 = performans.Rehber2Id == isciId;
            ViewData["Title"] = $"Performans — {performans.Isci.Ad} {performans.Isci.Soyad}";
            return View(performans);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> IsciQiymetlendir(int performansId,
            string? isciSherhi, string? inkisafPlani,
            List<int> kriteriyaIds, List<decimal> isciQiymetleri, List<string?> isciSherhler)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return Forbid();

            var p = await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query().Where(x => x.Id == performansId && !x.Silinib && x.IsciId == appUser.IsciId.Value)
                .Include(x => x.Kriteriyalar).FirstOrDefaultAsync();

            if (p == null) return NotFound();
            if (p.Status != PerformansStatus.Gozlemede)
            { TempData["Error"] = "Bu mərhələ artıq tamamlanıb."; return RedirectToAction(nameof(Detal), new { id = performansId }); }

            for (int i = 0; i < kriteriyaIds.Count; i++)
            {
                var krit = p.Kriteriyalar.FirstOrDefault(k => k.Id == kriteriyaIds[i]);
                if (krit == null) continue;
                krit.IsciQiymeti = isciQiymetleri[i];
                krit.IsciSherhi = isciSherhler.Count > i ? isciSherhler[i] : null;
                await _unitOfWork.Repository<PerformansKriteriya>().YenileAsync(krit);
            }

            decimal toplamCeki = p.Kriteriyalar.Sum(k => k.Ceki);
            p.IsciOrtalamaQiymet = toplamCeki > 0
                ? Math.Round(p.Kriteriyalar.Where(k => k.IsciQiymeti.HasValue)
                    .Sum(k => k.IsciQiymeti!.Value * k.Ceki) / toplamCeki, 2) : 0;
            p.IsciSherhi = isciSherhi;
            p.InkisafPlani = inkisafPlani;
            p.IsciQiymetlendirmeTarixi = DateTime.Now;
            p.Status = PerformansStatus.SobeReisiGozleyir;

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(p);
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = "Qiymətləndirməniz qeydə alındı.";
            return RedirectToAction(nameof(Detal), new { id = performansId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SobeReisiQiymetlendir(int performansId,
            string? sobeReisiSherhi,
            List<int> kriteriyaIds, List<decimal> sobeReisiQiymetleri, List<string?> sobeReisiSherhler)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return Forbid();

            var p = await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query().Where(x => x.Id == performansId && !x.Silinib && x.SobeReisiId == appUser.IsciId.Value)
                .Include(x => x.Kriteriyalar).FirstOrDefaultAsync();

            if (p == null) return NotFound();
            if (p.Status != PerformansStatus.SobeReisiGozleyir)
            { TempData["Error"] = "Bu mərhələ mövcud deyil."; return RedirectToAction(nameof(Detal), new { id = performansId }); }

            for (int i = 0; i < kriteriyaIds.Count; i++)
            {
                var krit = p.Kriteriyalar.FirstOrDefault(k => k.Id == kriteriyaIds[i]);
                if (krit == null) continue;
                krit.SobeReisiQiymeti = sobeReisiQiymetleri[i];
                krit.SobeReisiSherhi = sobeReisiSherhler.Count > i ? sobeReisiSherhler[i] : null;
                await _unitOfWork.Repository<PerformansKriteriya>().YenileAsync(krit);
            }

            decimal toplamCeki = p.Kriteriyalar.Sum(k => k.Ceki);
            p.SobeReisiOrtalamaQiymet = toplamCeki > 0
                ? Math.Round(p.Kriteriyalar.Where(k => k.SobeReisiQiymeti.HasValue)
                    .Sum(k => k.SobeReisiQiymeti!.Value * k.Ceki) / toplamCeki, 2) : 0;
            p.SobeReisiSherhi = sobeReisiSherhi;
            p.SobeReisiQiymetlendirmeTarixi = DateTime.Now;
            p.Status = PerformansStatus.RehberGozleyir;

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(p);
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = "Şöbə rəisi qiymətləndirməsi qeydə alındı.";
            return RedirectToAction(nameof(Detal), new { id = performansId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RehberQiymetlendir(int performansId,
            string? mudirSherhi, string? inkisafPlani,
            List<int> kriteriyaIds, List<decimal> mudirQiymetleri, List<string?> mudirSherhler)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return Forbid();

            var p = await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query().Where(x => x.Id == performansId && !x.Silinib && x.QiymetlendirenIsciId == appUser.IsciId.Value)
                .Include(x => x.Kriteriyalar).FirstOrDefaultAsync();

            if (p == null) return NotFound();
            var validStatuses = new[] { PerformansStatus.SobeReisiGozleyir, PerformansStatus.RehberGozleyir };
            if (!validStatuses.Contains(p.Status))
            { TempData["Error"] = "Bu mərhələ mövcud deyil."; return RedirectToAction(nameof(Detal), new { id = performansId }); }

            for (int i = 0; i < kriteriyaIds.Count; i++)
            {
                var krit = p.Kriteriyalar.FirstOrDefault(k => k.Id == kriteriyaIds[i]);
                if (krit == null) continue;
                krit.MudirQiymeti = mudirQiymetleri[i];
                krit.MudirSherhi = mudirSherhler.Count > i ? mudirSherhler[i] : null;
                await _unitOfWork.Repository<PerformansKriteriya>().YenileAsync(krit);
            }

            decimal toplamCeki = p.Kriteriyalar.Sum(k => k.Ceki);
            p.MudirOrtalamaQiymet = toplamCeki > 0
                ? Math.Round(p.Kriteriyalar.Where(k => k.MudirQiymeti.HasValue)
                    .Sum(k => k.MudirQiymeti!.Value * k.Ceki) / toplamCeki, 2) : 0;
            p.MudirSherhi = mudirSherhi;
            if (!string.IsNullOrEmpty(inkisafPlani)) p.InkisafPlani = inkisafPlani;
            p.MudirQiymetlendirmeTarixi = DateTime.Now;
            p.Status = p.Rehber2Id.HasValue ? PerformansStatus.Rehber2Gozleyir : PerformansStatus.Tamamlandi;

            decimal isci = p.IsciOrtalamaQiymet, sobe = p.SobeReisiOrtalamaQiymet;
            decimal r1 = p.MudirOrtalamaQiymet;
            bool hasSobe = p.SobeReisiId.HasValue && sobe > 0;
            p.YekunQiymet = hasSobe
                ? Math.Round((isci + sobe + r1) / 3, 2)
                : Math.Round((isci + r1) / 2, 2);

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(p);
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = p.Rehber2Id.HasValue
                ? "Qiymətləndirməniz qeydə alındı. Rəhbər 2 gözlənilir."
                : "Qiymətləndirmə tamamlandı.";
            return RedirectToAction(nameof(Detal), new { id = performansId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Rehber2Qiymetlendir(int performansId,
            string? rehber2Sherhi, string? inkisafPlani,
            List<int> kriteriyaIds, List<decimal> rehber2Qiymetleri, List<string?> rehber2Sherhler)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return Forbid();

            var p = await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query().Where(x => x.Id == performansId && !x.Silinib && x.Rehber2Id == appUser.IsciId.Value)
                .Include(x => x.Kriteriyalar).FirstOrDefaultAsync();

            if (p == null) return NotFound();
            if (p.Status != PerformansStatus.Rehber2Gozleyir)
            { TempData["Error"] = "Bu mərhələ mövcud deyil."; return RedirectToAction(nameof(Detal), new { id = performansId }); }

            for (int i = 0; i < kriteriyaIds.Count; i++)
            {
                var krit = p.Kriteriyalar.FirstOrDefault(k => k.Id == kriteriyaIds[i]);
                if (krit == null) continue;
                krit.Rehber2Qiymeti = rehber2Qiymetleri[i];
                krit.Rehber2Sherhi = rehber2Sherhler.Count > i ? rehber2Sherhler[i] : null;
                await _unitOfWork.Repository<PerformansKriteriya>().YenileAsync(krit);
            }

            decimal toplamCeki = p.Kriteriyalar.Sum(k => k.Ceki);
            p.Rehber2OrtalamaQiymet = toplamCeki > 0
                ? Math.Round(p.Kriteriyalar.Where(k => k.Rehber2Qiymeti.HasValue)
                    .Sum(k => k.Rehber2Qiymeti!.Value * k.Ceki) / toplamCeki, 2) : 0;
            p.Rehber2Sherhi = rehber2Sherhi;
            if (!string.IsNullOrEmpty(inkisafPlani)) p.InkisafPlani = inkisafPlani;
            p.Rehber2QiymetlendirmeTarixi = DateTime.Now;
            p.Status = PerformansStatus.Tamamlandi;

            bool hasSobe = p.SobeReisiId.HasValue && p.SobeReisiOrtalamaQiymet > 0;
            bool hasR2 = p.Rehber2OrtalamaQiymet > 0;
            decimal sum = p.IsciOrtalamaQiymet + p.MudirOrtalamaQiymet;
            int cnt = 2;
            if (hasSobe) { sum += p.SobeReisiOrtalamaQiymet; cnt++; }
            if (hasR2) { sum += p.Rehber2OrtalamaQiymet; cnt++; }
            p.YekunQiymet = Math.Round(sum / cnt, 2);

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(p);
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = "Yekun qiymətləndirmə tamamlandı.";
            return RedirectToAction(nameof(Detal), new { id = performansId });
        }
    }
}
