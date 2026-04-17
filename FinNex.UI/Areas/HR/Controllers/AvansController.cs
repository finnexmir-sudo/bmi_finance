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
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
    public class AvansController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IBildirisRouter _bildirisRouter;

        public AvansController(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IBildirisRouter bildirisRouter)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _bildirisRouter = bildirisRouter;
        }

        // ── GET /HR/Avans ────────────────────────────────────
        public async Task<IActionResult> Index(int? statusFilter = null)
        {
            var query = _unitOfWork.Repository<Avans>()
                .Query()
                .Where(x => !x.Silinib)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.Maliye)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Departament)
                .Include(x => x.Muhasib)
                .AsQueryable();

            if (statusFilter.HasValue)
                query = query.Where(x => (int)x.Status == statusFilter.Value);

            var list = await query
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            ViewBag.StatusFilter = statusFilter;
            ViewBag.GozlemedeSayi = list.Count(x => x.Status == AvansStatus.Gozlemede);
            ViewBag.TesdiqSayi = list.Count(x => x.Status == AvansStatus.Tesdiqlenib || x.Status == AvansStatus.Odenilib);
            ViewBag.ImtinaSayi = list.Count(x => x.Status == AvansStatus.ImtinaEdildi);

            ViewData["Title"] = "Avans Müraciətləri";
            return View(list);
        }

        // ── POST /HR/Avans/Tesdiq ────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Tesdiq(int id, bool tesdiq, string? imtinaSebebi)
        {
            var avans = await _unitOfWork.Repository<Avans>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

            if (avans == null)
            {
                TempData["Error"] = "Müraciət tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            if (avans.Status != AvansStatus.Gozlemede)
            {
                TempData["Error"] = "Yalnız gözləmədə olan müraciətləri təsdiq/rədd etmək olar.";
                return RedirectToAction(nameof(Index));
            }

            var appUser = await _userManager.GetUserAsync(User);
            var muhasibIsciId = appUser?.IsciId;

            if (tesdiq)
            {
                avans.Status = AvansStatus.Tesdiqlenib;
                avans.MuhasibId = muhasibIsciId;
                avans.TesdiqTarixi = DateTime.Now;
                TempData["Success"] = $"Avans təsdiqləndi — {avans.Mebleg:N2} ₼";
            }
            else
            {
                avans.Status = AvansStatus.ImtinaEdildi;
                avans.MuhasibId = muhasibIsciId;
                avans.TesdiqTarixi = DateTime.Now;
                avans.ImtinaSebebi = string.IsNullOrWhiteSpace(imtinaSebebi) ? null : imtinaSebebi.Trim();
                TempData["Success"] = "Müraciət rədd edildi.";
            }

            await _unitOfWork.YaddaSaxlaAsync();

            // İşçiyə bildiriş — qərar nəticəsi
            var redirectUrl = Url.Action("Index", "Avans", new { area = "User" });
            if (tesdiq)
            {
                await _bildirisRouter.NotifyIsciAsync(
                    avans.IsciId,
                    BildirisNovu.AvansTesdiq,
                    "Avansınız təsdiqləndi",
                    $"{avans.Il}/{avans.Ay:D2} ayı üçün {avans.Mebleg:N2} ₼ avans müraciətiniz təsdiqləndi.",
                    redirectUrl: redirectUrl);
            }
            else
            {
                var sebebMetn = string.IsNullOrWhiteSpace(avans.ImtinaSebebi) ? "" : $" Səbəb: {avans.ImtinaSebebi}";
                await _bildirisRouter.NotifyIsciAsync(
                    avans.IsciId,
                    BildirisNovu.AvansImtina,
                    "Avansınız rədd edildi",
                    $"{avans.Il}/{avans.Ay:D2} ayı üçün {avans.Mebleg:N2} ₼ avans müraciətiniz rədd edildi.{sebebMetn}",
                    redirectUrl: redirectUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
