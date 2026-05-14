using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
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
    public class TeklifController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IBildirisRouter _bildirisRouter;

        public TeklifController(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IBildirisRouter bildirisRouter)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _bildirisRouter = bildirisRouter;
        }

        public async Task<IActionResult> Index()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToAction("Index", "Dashboard");

            var items = await _unitOfWork.Repository<Teklif>()
                .Query()
                .Where(x => !x.Silinib && x.IsciId == isciId.Value)
                .Include(x => x.CavabVeren)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            ViewData["Title"] = "Mənim Təkliflərim";
            return View(items);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Yarat(TeklifNov nov, string bashliq, string mezmun)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToAction("Index", "Dashboard");

            if (string.IsNullOrWhiteSpace(bashliq) || string.IsNullOrWhiteSpace(mezmun))
            {
                TempData["Error"] = "Başlıq və məzmun mütləq doldurulmalıdır.";
                return RedirectToAction(nameof(Index));
            }

            var entity = new Teklif
            {
                IsciId   = isciId.Value,
                Nov      = nov,
                Bashliq  = bashliq.Trim(),
                Mezmun   = mezmun.Trim(),
                Status   = TeklifStatus.Gozlemede,
                Prioritet = TeklifPrioritet.Orta
            };

            await _unitOfWork.Repository<Teklif>().YaratAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();

            var isci = await _unitOfWork.Repository<Isci>()
                .GetirAsync(x => x.Id == isciId.Value, izlemeden: true);
            var isciAd = isci?.TamAd ?? $"İşçi #{isciId.Value}";

            var novMetn = nov switch
            {
                TeklifNov.BosluqBildirisi => "boşluq bildirişi",
                TeklifNov.Sikayat         => "şikayət",
                _                         => "teklif"
            };

            await _bildirisRouter.NotifyRolesAsync(
                new[] { RoleNames.HR, RoleNames.Admin },
                BildirisNovu.TeklifGonderildi,
                $"Yeni {novMetn}",
                $"{isciAd}: {entity.Bashliq}",
                redirectUrl: Url.Action("Index", "Teklif", new { area = "HR" }),
                exceptIsciId: isciId.Value);

            TempData["Success"] = "Təklifiniz göndərildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Legv(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToAction("Index", "Dashboard");

            var teklif = await _unitOfWork.Repository<Teklif>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsciId == isciId.Value && !x.Silinib);

            if (teklif == null || teklif.Status != TeklifStatus.Gozlemede)
            {
                TempData["Error"] = "Yalnız gözləmədə olan təklifləri silmək olar.";
                return RedirectToAction(nameof(Index));
            }

            teklif.Silinib = true;
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = "Teklif silindi.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<int?> GetIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }
    }
}
