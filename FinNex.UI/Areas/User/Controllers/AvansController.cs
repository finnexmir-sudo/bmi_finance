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
    public class AvansController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public AvansController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // ── GET /User/Avans ──────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToAction("Index", "Dashboard");

            var avanslar = await _unitOfWork.Repository<Avans>()
                .Query()
                .Where(x => !x.Silinib && x.IsciId == isciId.Value)
                .OrderByDescending(x => x.Il * 12 + x.Ay)
                .ToListAsync();

            var maliye = await _unitOfWork.Repository<IsciMaliye>()
                .Query()
                .FirstOrDefaultAsync(x => x.IsciId == isciId.Value && !x.Silinib);

            decimal cariMaas = maliye?.CariMaas ?? 0;
            decimal maxFaiz = await GetAvansMaxFaiziAsync();

            ViewBag.CariMaas = cariMaas;
            ViewBag.MaxAvans = Math.Round(cariMaas * (maxFaiz / 100m), 2);
            ViewBag.MaxFaiz = maxFaiz;

            ViewData["Title"] = "Avans";
            return View(avanslar);
        }

        // ── GET /User/Avans/Create ───────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToAction("Index", "Dashboard");

            // Bu ay artiq muraciet varmi?
            var buAy = DateTime.Now;
            var mevcud = await _unitOfWork.Repository<Avans>()
                .Query()
                .AnyAsync(x =>
                    !x.Silinib &&
                    x.IsciId == isciId.Value &&
                    x.Il == buAy.Year &&
                    x.Ay == buAy.Month &&
                    x.Status != AvansStatus.ImtinaEdildi);

            if (mevcud)
            {
                TempData["Error"] = "Bu ay üçün artıq avans müraciətiniz var.";
                return RedirectToAction(nameof(Index));
            }

            var maliye = await _unitOfWork.Repository<IsciMaliye>()
                .Query()
                .FirstOrDefaultAsync(x => x.IsciId == isciId.Value && !x.Silinib);

            decimal cariMaas = maliye?.CariMaas ?? 0;
            decimal maxFaiz = await GetAvansMaxFaiziAsync();

            ViewBag.CariMaas = cariMaas;
            ViewBag.MaxAvans = Math.Round(cariMaas * (maxFaiz / 100m), 2);
            ViewBag.MaxFaiz = maxFaiz;

            ViewData["Title"] = "Avans müraciəti";
            return View();
        }

        // ── POST /User/Avans/Create ──────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(decimal mebleg, string? sebeb)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToAction("Index", "Dashboard");

            var buAy = DateTime.Now;

            // Tekrar yoxla
            var mevcud = await _unitOfWork.Repository<Avans>()
                .Query()
                .AnyAsync(x =>
                    !x.Silinib &&
                    x.IsciId == isciId.Value &&
                    x.Il == buAy.Year &&
                    x.Ay == buAy.Month &&
                    x.Status != AvansStatus.ImtinaEdildi);

            if (mevcud)
            {
                TempData["Error"] = "Bu ay üçün artıq avans müraciətiniz var.";
                return RedirectToAction(nameof(Index));
            }

            var maliye = await _unitOfWork.Repository<IsciMaliye>()
                .Query()
                .FirstOrDefaultAsync(x => x.IsciId == isciId.Value && !x.Silinib);

            decimal cariMaas = maliye?.CariMaas ?? 0;
            decimal maxFaiz = await GetAvansMaxFaiziAsync();
            decimal maxAvans = Math.Round(cariMaas * (maxFaiz / 100m), 2);

            if (mebleg <= 0 || mebleg > maxAvans)
            {
                TempData["Error"] = $"Avans məbləği 0-dan böyük və max {maxAvans:N2} ₼ olmalıdır.";
                return RedirectToAction(nameof(Create));
            }

            var entity = new Avans
            {
                IsciId = isciId.Value,
                Il = buAy.Year,
                Ay = buAy.Month,
                Mebleg = mebleg,
                Sebeb = string.IsNullOrWhiteSpace(sebeb) ? null : sebeb.Trim(),
                Status = AvansStatus.Gozlemede
            };

            await _unitOfWork.Repository<Avans>().YaratAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = $"Avans müraciəti göndərildi — {mebleg:N2} ₼";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /User/Avans/Legv ────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Legv(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToAction("Index", "Dashboard");

            var avans = await _unitOfWork.Repository<Avans>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsciId == isciId.Value && !x.Silinib);

            if (avans == null || avans.Status != AvansStatus.Gozlemede)
            {
                TempData["Error"] = "Yalnız gözləmədə olan müraciəti ləğv etmək olar.";
                return RedirectToAction(nameof(Index));
            }

            avans.Silinib = true;
            avans.SilinmeTarixi = DateTime.Now;
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = "Avans müraciəti ləğv edildi.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<int?> GetIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private async Task<decimal> GetAvansMaxFaiziAsync()
        {
            var param = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .Where(x => x.Aktivdir && !x.Silinib && x.Nov == MaasParametrNovu.AvansMaxFaizi)
                .OrderByDescending(x => x.BaslamaTarixi)
                .FirstOrDefaultAsync();
            return param?.Deyer ?? 30m;
        }
    }
}
