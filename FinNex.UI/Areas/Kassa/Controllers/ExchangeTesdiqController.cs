using FinNex.Application.Interfaces.Kassa;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Kassa.Controllers
{
    /// <summary>
    /// Kassa valyuta kursunu təsdiqləmə/imtina ekranı — BMI "frmExchangeTesdiq"
    /// modulunun köçürülməsi (09.10.2026). İcazə ROL ilə YOX,
    /// <see cref="IKassaTesdiqEdiciService"/> siyahısı ilə yoxlanır (Admin
    /// tərəfindən idarə olunur) — BMI-dəki "heç bir yoxlama yoxdur" bugu
    /// burada bağlanıb.
    /// </summary>
    [Area("Kassa")]
    [Authorize]
    public class ExchangeTesdiqController : Controller
    {
        private readonly IKassaKursService _kassaKursService;
        private readonly IKassaTesdiqEdiciService _tesdiqEdiciService;
        private readonly UserManager<AppUser> _userManager;

        public ExchangeTesdiqController(
            IKassaKursService kassaKursService,
            IKassaTesdiqEdiciService tesdiqEdiciService,
            UserManager<AppUser> userManager)
        {
            _kassaKursService = kassaKursService;
            _tesdiqEdiciService = tesdiqEdiciService;
            _userManager = userManager;
        }

        // GET /Kassa/ExchangeTesdiq
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Kassa — Kurs təsdiqi";

            var isciId = await GetCurrentIsciIdAsync();
            var tesdiqEdeBiler = isciId != null && await _tesdiqEdiciService.TesdiqEdeBilerMiAsync(isciId.Value);
            ViewBag.TesdiqEdeBiler = tesdiqEdeBiler;

            if (!tesdiqEdeBiler)
            {
                ViewBag.Gozleyenler = new List<FinNex.Application.DTOs.Kassa.KassaKursBeyannameOzetDto>();
                ViewBag.Neticelenmisler = new List<FinNex.Application.DTOs.Kassa.KassaKursBeyannameOzetDto>();
                return View();
            }

            ViewBag.Gozleyenler = await _kassaKursService.GozleyenleriGetirAsync();
            ViewBag.Neticelenmisler = await _kassaKursService.SonNeticelenmisleriGetirAsync();
            return View();
        }

        // POST /Kassa/ExchangeTesdiq/Tesdiq/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tesdiq(int id)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null || !await _tesdiqEdiciService.TesdiqEdeBilerMiAsync(isciId.Value))
            {
                TempData["Error"] = "Bu əməliyyat üçün icazəniz yoxdur.";
                return RedirectToAction(nameof(Index));
            }

            var sonuc = await _kassaKursService.TesdiqEtAsync(id, isciId.Value);
            // User-area ortaq layout TempData["Success"]/["Error"] gözləyir.
            TempData[sonuc.Success ? "Success" : "Error"] = sonuc.Message;
            return RedirectToAction(nameof(Index));
        }

        // POST /Kassa/ExchangeTesdiq/Imtina/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Imtina(int id, string sebeb)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null || !await _tesdiqEdiciService.TesdiqEdeBilerMiAsync(isciId.Value))
            {
                TempData["Error"] = "Bu əməliyyat üçün icazəniz yoxdur.";
                return RedirectToAction(nameof(Index));
            }

            var sonuc = await _kassaKursService.ImtinaEtAsync(id, isciId.Value, sebeb);
            TempData[sonuc.Success ? "Success" : "Error"] = sonuc.Message;
            return RedirectToAction(nameof(Index));
        }

        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }
    }
}
