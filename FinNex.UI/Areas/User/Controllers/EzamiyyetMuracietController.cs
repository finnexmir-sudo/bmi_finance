using FinNex.Application.DTOs.HR.Ezamiyyet;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Services.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class EzamiyyetMuracietController : Controller
    {
        private readonly IEzamiyyetService _service;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _config;
        private readonly IBildirisRouter _bildirisRouter;

        public EzamiyyetMuracietController(
            IEzamiyyetService service,
            UserManager<AppUser> userManager,
            IConfiguration config,
            IBildirisRouter bildirisRouter)
        {
            _service        = service;
            _userManager    = userManager;
            _config         = config;
            _bildirisRouter = bildirisRouter;
        }

        public async Task<IActionResult> Index()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToAction("Login", "Account", new { area = "" });
            var list = await _service.IsciMuracietleriAsync(isciId.Value);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create(DateTime? tarix, string? saat, string? baslig, string? mekan, string? qeyd)
        {
            var mekanlar = await _service.MekanlarAsync();
            ViewBag.Mekanlar = mekanlar;

            // Öncədən doldurma (məs. məhkəmə görüşü → ezamiyyət linki). Parametr yoxdursa boş gəlir.
            var model = new EzamiyyetMuracietCreateDto
            {
                Baslig        = baslig ?? "",
                BaslamaTarixi = tarix ?? DateTime.Today,
                BitmeTarixi   = tarix ?? DateTime.Today,
                BaslamaSaati  = saat,
                YeniMekanAd   = mekan,
                Qeyd          = qeyd
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EzamiyyetMuracietCreateDto dto)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return Unauthorized();

            var user    = await _userManager.GetUserAsync(User);
            var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
            var (ok, error, _) = await _service.YaratAsync(isciId.Value, dto, dmsRoot);
            if (!ok)
            {
                TempData["Error"] = error;
                ViewBag.Mekanlar  = await _service.MekanlarAsync();
                return View(dto);
            }

            var isciAd = user?.UserName ?? "İşçi";
            await _bildirisRouter.NotifyRoleAsync(
                RoleNames.Rehber,
                BildirisNovu.EzamiyyetMuraciet,
                "Yeni ezamiyyət müraciəti",
                $"{isciAd} {dto.BaslamaTarixi:dd.MM.yyyy} – {dto.BitmeTarixi:dd.MM.yyyy} tarixləri üçün ezamiyyət müraciəti göndərdi.",
                redirectUrl:  Url.Action("Index", "Ezamiyyet", new { area = "HR" }),
                exceptIsciId: isciId.Value);

            TempData["Success"] = "Ezamiyyət müraciətiniz göndərildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeriQeyd(int id, string? qeyd)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return Unauthorized();
            var (ok, error) = await _service.GeriQeydElavEtAsync(id, isciId.Value, qeyd);
            TempData[ok ? "Success" : "Error"] = ok ? "Geri dönüş notu saxlandı." : error;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Legvet(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return Unauthorized();
            var (ok, error) = await _service.LegvEtAsync(id, isciId.Value);
            TempData[ok ? "Success" : "Error"] = ok ? "Müraciət ləğv edildi." : error;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetMekanlar(string? q)
        {
            var mekanlar = await _service.MekanlarAsync();
            if (!string.IsNullOrWhiteSpace(q))
                mekanlar = mekanlar.Where(m => m.Ad.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            return Json(mekanlar.Select(m => new { m.Id, m.Ad }));
        }

        private async Task<int?> GetIsciIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.IsciId;
        }
    }
}
