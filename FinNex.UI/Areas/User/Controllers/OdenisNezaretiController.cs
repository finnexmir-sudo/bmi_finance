using FinNex.Application.DTOs.Pid;
using FinNex.Application.Interfaces.Pid;
using FinNex.Domain;
using FinNex.Domain.Entities.Pid;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize(Roles = "Admin,PID")]
public class OdenisNezaretiController : Controller
{
    private readonly IOdenisNezaretiService _service;
    private readonly UserManager<AppUser> _userManager;

    public OdenisNezaretiController(IOdenisNezaretiService service, UserManager<AppUser> userManager)
    {
        _service = service;
        _userManager = userManager;
    }

    private async Task<int> CurrentIsciIdAsync()
    {
        var u = await _userManager.GetUserAsync(User);
        return u?.IsciId ?? 0;
    }

    public async Task<IActionResult> Index(BalansNovu? balans, string? axtaris)
    {
        var list = await _service.HamisiniGetirAsync(balans, axtaris);
        ViewData["BalansFiltri"] = balans;
        ViewData["Axtaris"] = axtaris;
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(OdenisNezaretiCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.MusteriAdi))
        {
            TempData["Error"] = "Müştəri adı məcburidir.";
            return RedirectToAction(nameof(Index));
        }
        await _service.YaratAsync(dto, await CurrentIsciIdAsync());
        TempData["Success"] = "Qeyd əlavə edildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yenile(OdenisNezaretiUpdateDto dto)
    {
        var ok = await _service.YenileAsync(dto, await CurrentIsciIdAsync());
        TempData[ok ? "Success" : "Error"] = ok ? "Yeniləndi." : "Qeyd tapılmadı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        await _service.SilAsync(id, await CurrentIsciIdAsync());
        TempData["Success"] = "Qeyd silindi.";
        return RedirectToAction(nameof(Index));
    }
}
