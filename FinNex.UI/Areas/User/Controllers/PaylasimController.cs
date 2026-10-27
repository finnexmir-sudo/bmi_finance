using FinNex.Application.Interfaces.Paylasim;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

/// <summary>
/// "Paylaşımlar" — işçinin oxuduğu bilik lenti (27.10.2026, pilot). Elan-dan AYRI —
/// əməliyyat xəbərdarlığı deyil, sadəcə oxuma məqsədli məlumatdır.
/// </summary>
[Area("User")]
[Authorize]
public class PaylasimController : Controller
{
    private readonly IPaylasimService _paylasim;
    private readonly UserManager<AppUser> _userManager;

    public PaylasimController(IPaylasimService paylasim, UserManager<AppUser> userManager)
    {
        _paylasim = paylasim;
        _userManager = userManager;
    }

    // ── GET /User/Paylasim ──────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Paylaşımlar";

        // IsciId boşdursa (işçi profilinə bağlı olmayan hesab) 0 keçirik — heç bir
        // hədəflənmiş posta uyğun gəlmir, yalnız "Hamısına" postlar görünür.
        var appUser = await _userManager.GetUserAsync(User);
        var isciId = appUser?.IsciId ?? 0;

        var lenti = await _paylasim.AktivLentAsync(isciId);

        // Lenti gördü — sayğacı sıfırlamaq üçün son-baxış tarixini indiyə yeniləyir
        // (27.10.2026, sidebar-dakı "yeni paylaşım" nişanı).
        if (appUser != null)
            await _paylasim.GorulduIsaretleAsync(appUser.Id);

        return View(lenti);
    }

    // ── GET /User/Paylasim/YeniSay — sidebar nişanı (27.10.2026) ─────
    public async Task<IActionResult> YeniSay()
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null) return Json(new { say = 0 });

        var say = await _paylasim.YeniSayAsync(appUser.IsciId ?? 0, appUser.Id);
        return Json(new { say });
    }
}
