using FinNex.Application.Interfaces.Pid;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class PidSmsController : Controller
{
    private readonly IPidSmsService _smsService;
    private readonly IPidSmsSablonService _sablonService;
    private readonly UserManager<AppUser> _userManager;

    public PidSmsController(
        IPidSmsService smsService,
        IPidSmsSablonService sablonService,
        UserManager<AppUser> userManager)
    {
        _smsService = smsService;
        _sablonService = sablonService;
        _userManager = userManager;
    }

    private async Task<int?> CurrentIsciIdAsync()
    {
        var u = await _userManager.GetUserAsync(User);
        return u?.IsciId;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Sablonlar = await _sablonService.HamisiniGetirAsync(yalnizAktiv: true);
        ViewBag.Loglar = await _smsService.SonGonderilenler(200);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Gonder(string telefon, string metn, int? sablonId)
    {
        if (string.IsNullOrWhiteSpace(telefon) || string.IsNullOrWhiteSpace(metn))
        {
            TempData["Error"] = "Telefon və mətn boş ola bilməz.";
            return RedirectToAction("Index");
        }

        var isciId = await CurrentIsciIdAsync();
        if (isciId is null)
        {
            TempData["Error"] = "İşçi məlumatı tapılmadı.";
            return RedirectToAction("Index");
        }

        var log = await _smsService.GonderAsync(telefon, metn, sablonId, isciId.Value);
        if (log.Status == Domain.Entities.Pid.PidSmsStatus.Xeta)
            TempData["Error"] = $"SMS göndərilmədi: {log.Xeta}";
        else
            TempData["Success"] = $"SMS göndərildi → {log.Telefon}";

        return RedirectToAction("Index");
    }

    // ── Şablonlar ─────────────────────────────────────────

    public async Task<IActionResult> Sablonlar()
    {
        ViewBag.Sablonlar = await _sablonService.HamisiniGetirAsync(yalnizAktiv: false);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SablonYarat(string ad, string metn, string? aciqlama)
    {
        if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(metn))
        {
            TempData["Error"] = "Ad və mətn məcburidir.";
            return RedirectToAction("Sablonlar");
        }
        var isciId = await CurrentIsciIdAsync() ?? 0;
        await _sablonService.YaratAsync(ad, metn, aciqlama, isciId);
        TempData["Success"] = "Şablon yaradıldı.";
        return RedirectToAction("Sablonlar");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SablonYenile(int id, string ad, string metn, string? aciqlama, bool aktiv)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        var ok = await _sablonService.YenileAsync(id, ad, metn, aciqlama, aktiv, isciId);
        TempData[ok ? "Success" : "Error"] = ok ? "Şablon yeniləndi." : "Şablon tapılmadı.";
        return RedirectToAction("Sablonlar");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SablonSil(int id)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        var ok = await _sablonService.SilAsync(id, isciId);
        TempData[ok ? "Success" : "Error"] = ok ? "Şablon silindi." : "Şablon tapılmadı.";
        return RedirectToAction("Sablonlar");
    }
}
