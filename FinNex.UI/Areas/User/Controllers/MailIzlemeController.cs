using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.UI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
// Mail İzləmə siyahısı — Gələn/Gedən Mail-in bir hissəsi, EYNİ giriş qaydası.
[MailIcazesi]
public class MailIzlemeController : Controller
{
    private readonly IMailIzlemeService _izlemeService;
    private readonly UserManager<AppUser> _userManager;

    public MailIzlemeController(IMailIzlemeService izlemeService, UserManager<AppUser> userManager)
    {
        _izlemeService = izlemeService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null) return Unauthorized();

        var list = await _izlemeService.GetListAsync(appUser.Id);
        ViewData["Title"] = "Mail İzləmə Siyahısı";
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(string email, string? ad)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null) return Unauthorized();

        var sonuc = await _izlemeService.EkleAsync(appUser.Id, email, ad);
        if (sonuc.Success) TempData["Success"] = sonuc.Message;
        else TempData["Error"] = sonuc.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null) return Unauthorized();

        var sonuc = await _izlemeService.SilAsync(id, appUser.Id);
        if (sonuc.Success) TempData["Success"] = sonuc.Message;
        else TempData["Error"] = sonuc.Message;

        return RedirectToAction(nameof(Index));
    }
}
