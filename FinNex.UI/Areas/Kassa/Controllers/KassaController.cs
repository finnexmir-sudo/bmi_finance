using FinNex.Application.Interfaces.Kassa;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Kassa.Controllers
{
    /// <summary>
    /// Top-nav "Departamentlər → Kassa" girişinin HƏDƏFİ — rola görə düzgün
    /// səhifəyə yönləndirir (09.10.2026, istifadəçi tələbi: "kurs tesdiqini
    /// departamentlerde niye saldin, kassaya girende orda tesdiq etsin").
    ///
    /// Əvvəl top-nav-da "Kassa" (→ Exchange/Index, rol-məhdud) VƏ ayrıca
    /// "Kurs təsdiqi" (→ ExchangeTesdiq/Index, rolsuz) İKİ sıra idi — bu,
    /// istifadəçiyə qarışıq göründü. İndi TƏK "Kassa" sırası var, bu
    /// controller giriş anında qərar verir:
    ///   Kassa/Admin rolu  → Exchange/Index (kurs daxiletmə)
    ///   təsdiqedici siyahısında (rolu olmasa belə) → ExchangeTesdiq/Index
    ///   heç biri deyilsə  → Exchange/Index (öz rol-məhdudiyyətinə düşür,
    ///                       tanış "İcazə yoxdur" mesajını göstərir)
    /// </summary>
    [Area("Kassa")]
    [Authorize]
    public class KassaController : Controller
    {
        private readonly IKassaTesdiqEdiciService _tesdiqEdiciService;
        private readonly UserManager<AppUser> _userManager;

        public KassaController(
            IKassaTesdiqEdiciService tesdiqEdiciService,
            UserManager<AppUser> userManager)
        {
            _tesdiqEdiciService = tesdiqEdiciService;
            _userManager = userManager;
        }

        // GET /Kassa
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole(RoleNames.Kassa) || User.IsInRole(RoleNames.Admin))
                return RedirectToAction("Index", "Exchange");

            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId is int isciId && await _tesdiqEdiciService.TesdiqEdeBilerMiAsync(isciId))
                return RedirectToAction("Index", "ExchangeTesdiq");

            return RedirectToAction("Index", "Exchange");
        }
    }
}
