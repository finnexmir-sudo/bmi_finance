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
                    x.QiymetlendirenIsciId == isciId))
                .Include(x => x.Isci)
                .Include(x => x.QiymetlendirenIsci)
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
                    x.QiymetlendirenIsciId == isciId))
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Departament)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Vezife)
                .Include(x => x.QiymetlendirenIsci)
                .Include(x => x.SobeReisi)
                .Include(x => x.Kriteriyalar)
                .FirstOrDefaultAsync();

            if (performans == null) return NotFound();

            ViewBag.CurrentIsciId = isciId;
            ViewBag.IsIsci = performans.IsciId == isciId;
            ViewBag.IsSobeReisi = performans.SobeReisiId == isciId;
            ViewBag.IsRehber = performans.QiymetlendirenIsciId == isciId;
            ViewData["Title"] = $"Performans — {performans.Isci.Ad} {performans.Isci.Soyad}";
            return View(performans);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IsciQiymetlendir(int performansId,
            string? isciSherhi, string? inkisafPlani,
            List<int> kriteriyaIds, List<decimal> isciQiymetleri, List<string?> isciSherhler)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return Forbid();

            var performans = await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query().Where(x => x.Id == performansId && !x.Silinib && x.IsciId == appUser.IsciId.Value)
                .Include(x => x.Kriteriyalar).FirstOrDefaultAsync();

            if (performans == null) return NotFound();
            if (performans.Status != PerformansStatus.Gozlemede)
            {
                TempData["Error"] = "Bu mərhələ artıq tamamlanıb.";
                return RedirectToAction(nameof(Detal), new { id = performansId });
            }

            for (int i = 0; i < kriteriyaIds.Count; i++)
            {
                var krit = performans.Kriteriyalar.FirstOrDefault(k => k.Id == kriteriyaIds[i]);
                if (krit == null) continue;
                krit.IsciQiymeti = isciQiymetleri[i];
                krit.IsciSherhi = isciSherhler.Count > i ? isciSherhler[i] : null;
                await _unitOfWork.Repository<PerformansKriteriya>().YenileAsync(krit);
            }

            decimal toplamCeki = performans.Kriteriyalar.Sum(k => k.Ceki);
            performans.IsciOrtalamaQiymet = toplamCeki > 0
                ? Math.Round(performans.Kriteriyalar.Where(k => k.IsciQiymeti.HasValue)
                    .Sum(k => k.IsciQiymeti!.Value * k.Ceki) / toplamCeki, 2) : 0;
            performans.IsciSherhi = isciSherhi;
            performans.InkisafPlani = inkisafPlani;
            performans.IsciQiymetlendirmeTarixi = DateTime.Now;
            performans.Status = PerformansStatus.IsciQiymetlendirdi;

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(performans);
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = "Qiymətləndirməniz qeydə alındı.";
            return RedirectToAction(nameof(Detal), new { id = performansId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SobeReisiQiymetlendir(int performansId,
            string? sobeReisiSherhi,
            List<int> kriteriyaIds, List<decimal> sobeReisiQiymetleri, List<string?> sobeReisiSherhler)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return Forbid();

            var performans = await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query().Where(x => x.Id == performansId && !x.Silinib && x.SobeReisiId == appUser.IsciId.Value)
                .Include(x => x.Kriteriyalar).FirstOrDefaultAsync();

            if (performans == null) return NotFound();
            if (performans.Status != PerformansStatus.IsciQiymetlendirdi)
            {
                TempData["Error"] = "Bu mərhələ mövcud deyil.";
                return RedirectToAction(nameof(Detal), new { id = performansId });
            }

            for (int i = 0; i < kriteriyaIds.Count; i++)
            {
                var krit = performans.Kriteriyalar.FirstOrDefault(k => k.Id == kriteriyaIds[i]);
                if (krit == null) continue;
                krit.SobeReisiQiymeti = sobeReisiQiymetleri[i];
                krit.SobeReisiSherhi = sobeReisiSherhler.Count > i ? sobeReisiSherhler[i] : null;
                await _unitOfWork.Repository<PerformansKriteriya>().YenileAsync(krit);
            }

            decimal toplamCeki = performans.Kriteriyalar.Sum(k => k.Ceki);
            performans.SobeReisiOrtalamaQiymet = toplamCeki > 0
                ? Math.Round(performans.Kriteriyalar.Where(k => k.SobeReisiQiymeti.HasValue)
                    .Sum(k => k.SobeReisiQiymeti!.Value * k.Ceki) / toplamCeki, 2) : 0;
            performans.SobeReisiSherhi = sobeReisiSherhi;
            performans.SobeReisiQiymetlendirmeTarixi = DateTime.Now;
            performans.Status = PerformansStatus.SobeReisiQiymetlendirdi;

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(performans);
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = "Şöbə rəisi qiymətləndirməsi qeydə alındı.";
            return RedirectToAction(nameof(Detal), new { id = performansId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RehberQiymetlendir(int performansId,
            string? mudirSherhi, string? inkisafPlani,
            List<int> kriteriyaIds, List<decimal> mudirQiymetleri, List<string?> mudirSherhler)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return Forbid();

            var performans = await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query().Where(x => x.Id == performansId && !x.Silinib && x.QiymetlendirenIsciId == appUser.IsciId.Value)
                .Include(x => x.Kriteriyalar).FirstOrDefaultAsync();

            if (performans == null) return NotFound();
            var validStatuses = new[] { PerformansStatus.IsciQiymetlendirdi, PerformansStatus.SobeReisiQiymetlendirdi };
            if (!validStatuses.Contains(performans.Status))
            {
                TempData["Error"] = "Bu mərhələ mövcud deyil.";
                return RedirectToAction(nameof(Detal), new { id = performansId });
            }

            for (int i = 0; i < kriteriyaIds.Count; i++)
            {
                var krit = performans.Kriteriyalar.FirstOrDefault(k => k.Id == kriteriyaIds[i]);
                if (krit == null) continue;
                krit.MudirQiymeti = mudirQiymetleri[i];
                krit.MudirSherhi = mudirSherhler.Count > i ? mudirSherhler[i] : null;
                await _unitOfWork.Repository<PerformansKriteriya>().YenileAsync(krit);
            }

            decimal toplamCeki = performans.Kriteriyalar.Sum(k => k.Ceki);
            performans.MudirOrtalamaQiymet = toplamCeki > 0
                ? Math.Round(performans.Kriteriyalar.Where(k => k.MudirQiymeti.HasValue)
                    .Sum(k => k.MudirQiymeti!.Value * k.Ceki) / toplamCeki, 2) : 0;
            performans.MudirSherhi = mudirSherhi;
            if (!string.IsNullOrEmpty(inkisafPlani)) performans.InkisafPlani = inkisafPlani;
            performans.MudirQiymetlendirmeTarixi = DateTime.Now;
            performans.Status = PerformansStatus.Tamamlandi;

            decimal isci = performans.IsciOrtalamaQiymet;
            decimal sobe = performans.SobeReisiOrtalamaQiymet;
            decimal rehber = performans.MudirOrtalamaQiymet;
            bool hasSobe = performans.SobeReisiId.HasValue && sobe > 0;
            performans.YekunQiymet = hasSobe
                ? Math.Round((isci + sobe + rehber) / 3, 2)
                : Math.Round((isci + rehber) / 2, 2);

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(performans);
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = "Qiymətləndirmə tamamlandı.";
            return RedirectToAction(nameof(Detal), new { id = performansId });
        }
    }
}
