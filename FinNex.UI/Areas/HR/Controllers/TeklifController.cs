using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber + "," + RoleNames.SobeReisi)]
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
            var items = await _unitOfWork.Repository<Teklif>()
                .Query()
                .Where(x => !x.Silinib)
                .Include(x => x.Isci)
                .Include(x => x.CavabVeren)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            ViewData["Title"] = "Təkliflər";
            return View(items);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Saxla(int id, TeklifStatus status, TeklifPrioritet prioritet, string? cavab)
        {
            var teklif = await _unitOfWork.Repository<Teklif>()
                .Query()
                .Include(x => x.Isci)
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

            if (teklif == null)
            {
                TempData["Error"] = "Teklif tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var eskiStatus    = teklif.Status;
            var eskiPrioritet = teklif.Prioritet;
            teklif.Status    = status;
            teklif.Prioritet = prioritet;

            bool cavabVerildi = !string.IsNullOrWhiteSpace(cavab);
            if (cavabVerildi)
            {
                var appUser = await _userManager.GetUserAsync(User);
                teklif.Cavab             = cavab!.Trim();
                teklif.CavabVerenIsciId  = appUser?.IsciId;
                teklif.CavabTarixi       = DateTime.Now;
            }

            await _unitOfWork.YaddaSaxlaAsync();

            // İşçiyə bildiriş — cavab varsa və ya status dəyişibsə
            bool statusDeyisdi = eskiStatus != status;
            if (cavabVerildi || statusDeyisdi)
            {
                var statusMetn = status switch
                {
                    TeklifStatus.Baxilir    => "Baxılır",
                    TeklifStatus.Tamamlandi => "Tamamlandı",
                    TeklifStatus.ReddEdildi => "Rədd edildi",
                    _                       => "Gözlənilir"
                };
                var metn = cavabVerildi
                    ? $"«{teklif.Bashliq}» — {statusMetn}. Cavab: {cavab!.Trim()}"
                    : $"«{teklif.Bashliq}» statusu dəyişdi: {statusMetn}";

                await _bildirisRouter.NotifyIsciAsync(
                    teklif.IsciId,
                    BildirisNovu.TeklifCavab,
                    "Təklifinizə yenilənmə",
                    metn,
                    redirectUrl: Url.Action("Index", "Teklif", new { area = "User" }));
            }

            TempData["Success"] = "Teklif yeniləndi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
