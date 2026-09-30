using System.Security.Claims;
using FinNex.Application.DTOs.Risk;
using FinNex.Application.Interfaces.Risk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Risk.Controllers;

/// <summary>
/// Bankın öz "tədbir planı" zərər həddi (MB Qərar 04/1, Əlavə 4 qeydi) —
/// Risk departamentinin özünün sazladığı TƏK sətirli parametr (30.09.2026,
/// istifadəçi qərarı).
/// </summary>
[Area("Risk")]
[Authorize]
public class EmeliyyatRiskiParametrleriController : Controller
{
    private readonly IEmeliyyatRiskiParametrleriService _service;

    public EmeliyyatRiskiParametrleriController(IEmeliyyatRiskiParametrleriService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Əməliyyat Riski — Parametrlər";
        return View(await _service.AlAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(EmeliyyatRiskiParametrleriDto model)
    {
        var icraciId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
        var result = await _service.SaxlaAsync(model, icraciId);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
