using System.Security.Claims;
using FinNex.Application.Interfaces.Kredit;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

/// <summary>
/// Müqavilə nömrə sayğacları — BMI-dən köçürmə və cari vəziyyət.
/// Yalnız Admin: sayğaca toxunmaq müqavilə nömrələrini dəyişir.
/// </summary>
[Area("User")]
[Authorize(Roles = RoleNames.Admin)]
public class MuqavileSayghacController : Controller
{
    private readonly IMuqavileSayghacImportService _import;
    private readonly IMuqavileSayghacService _saygac;

    public MuqavileSayghacController(
        IMuqavileSayghacImportService import, IMuqavileSayghacService saygac)
    {
        _import = import;
        _saygac = saygac;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET: /User/MuqavileSayghac
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var res = await _import.VeziyyetAsync(ct);
        if (!res.Success)
            TempData["Error"] = res.Message;

        ViewBag.FinNexSayghaclar = await _saygac.HamisiniGetirAsync();
        return View(res.Data ?? new Application.DTOs.Kredit.Muqavile.MuqavileSayghacKocurmeDto());
    }

    // POST: /User/MuqavileSayghac/Il — bir ilin sayğaclarını köçürür (idempotent)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Il(int il, CancellationToken ct)
    {
        var res = await _import.IlKocurAsync(il, GetUserId(), ct);
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }
}
