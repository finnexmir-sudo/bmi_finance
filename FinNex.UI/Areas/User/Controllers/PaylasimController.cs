using FinNex.Application.Interfaces.Paylasim;
using Microsoft.AspNetCore.Authorization;
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

    public PaylasimController(IPaylasimService paylasim)
    {
        _paylasim = paylasim;
    }

    // ── GET /User/Paylasim ──────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Paylaşımlar";
        var lenti = await _paylasim.AktivLentAsync();
        return View(lenti);
    }
}
