using FinNex.Application.DTOs.Communication.IsciMail;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class IsciMailController : Controller
{
    private readonly IIsciMailService _isciMailService;
    private readonly IIsciService _isciService;
    private readonly UserManager<AppUser> _userManager;

    public IsciMailController(
        IIsciMailService isciMailService,
        IIsciService isciService,
        UserManager<AppUser> userManager)
    {
        _isciMailService = isciMailService;
        _isciService = isciService;
        _userManager = userManager;
    }

    // GET /User/IsciMail
    public async Task<IActionResult> Index()
    {
        var isciId = await GetIsciIdAsync();
        if (isciId == null) return RedirectToLogin();

        var inbox = await _isciMailService.InboxGetirAsync(isciId.Value);
        ViewData["Title"] = "Gələn Qutusu";
        ViewData["OxunmamisSayi"] = inbox.Count(m => !m.Oxunub);
        return View(inbox);
    }

    // GET /User/IsciMail/Gonderilenler
    public async Task<IActionResult> Gonderilenler()
    {
        var isciId = await GetIsciIdAsync();
        if (isciId == null) return RedirectToLogin();

        var mails = await _isciMailService.GonderilenlerGetirAsync(isciId.Value);
        ViewData["Title"] = "Göndərilənlər";
        return View(mails);
    }

    // GET /User/IsciMail/Taslaqlar
    public async Task<IActionResult> Taslaqlar()
    {
        var isciId = await GetIsciIdAsync();
        if (isciId == null) return RedirectToLogin();

        var mails = await _isciMailService.TaslaqlarGetirAsync(isciId.Value);
        ViewData["Title"] = "Qaralamalar";
        return View(mails);
    }

    // GET /User/IsciMail/Detay/5
    public async Task<IActionResult> Detay(int id)
    {
        var isciId = await GetIsciIdAsync();
        if (isciId == null) return RedirectToLogin();

        var mail = await _isciMailService.DetayGetirAsync(id, isciId.Value);
        if (mail == null)
        {
            TempData["Error"] = "Mail tapılmadı.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = mail.Movzu;
        return View(mail);
    }

    // GET /User/IsciMail/Yaz
    public async Task<IActionResult> Yaz(int? aliciId = null)
    {
        var isciId = await GetIsciIdAsync();
        if (isciId == null) return RedirectToLogin();

        var iscilerResult = await _isciService.HamisiniGetirAsync(
            x => x.Id != isciId.Value && x.Status == IsciStatus.Aktiv);

        ViewBag.Isciler = iscilerResult.Success
            ? iscilerResult.Data!
                .Select(x => new SelectListItem($"{x.TamAd} ({x.SobeAdi ?? "-"})", x.Id.ToString()))
                .ToList()
            : new List<SelectListItem>();

        var dto = new IsciMailCreateDto();
        if (aliciId.HasValue)
            dto.AliciIsciIds = new List<int> { aliciId.Value };

        ViewData["Title"] = "Yeni Mail";
        return View(dto);
    }

    // POST /User/IsciMail/Yaz
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yaz(IsciMailCreateDto dto, string action = "gonder")
    {
        var isciId = await GetIsciIdAsync();
        if (isciId == null) return RedirectToLogin();

        if (action == "taslaq")
        {
            dto.Taslagdir = true;
            await _isciMailService.TaslagKaydetAsync(dto, isciId.Value);
            TempData["Success"] = "Qaralama yadda saxlanıldı.";
            return RedirectToAction(nameof(Taslaqlar));
        }

        if (!dto.AliciIsciIds.Any())
        {
            TempData["Error"] = "Ən azı bir alıcı seçin.";
            return RedirectToAction(nameof(Yaz));
        }

        await _isciMailService.GonderAsync(dto, isciId.Value);
        TempData["Success"] = "Mail uğurla göndərildi.";
        return RedirectToAction(nameof(Gonderilenler));
    }

    // POST /User/IsciMail/TaslagiGonder
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaslagiGonder(int id)
    {
        var isciId = await GetIsciIdAsync();
        if (isciId == null) return RedirectToLogin();

        await _isciMailService.TaslagiGonderAsync(id, isciId.Value);
        TempData["Success"] = "Qaralama göndərildi.";
        return RedirectToAction(nameof(Gonderilenler));
    }

    // POST /User/IsciMail/Sil
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id, string donusSehifesi = "Index")
    {
        var isciId = await GetIsciIdAsync();
        if (isciId == null) return RedirectToLogin();

        await _isciMailService.SilAsync(id, isciId.Value);
        TempData["Success"] = "Mail silindi.";
        return donusSehifesi switch
        {
            "Gonderilenler" => RedirectToAction(nameof(Gonderilenler)),
            "Taslaqlar" => RedirectToAction(nameof(Taslaqlar)),
            _ => RedirectToAction(nameof(Index))
        };
    }

    // GET /User/IsciMail/OxunmamisSayi (AJAX)
    [HttpGet]
    public async Task<IActionResult> OxunmamisSayi()
    {
        var isciId = await GetIsciIdAsync();
        if (isciId == null) return Json(new { sayi = 0 });

        var sayi = await _isciMailService.OxunmamisSayiAsync(isciId.Value);
        return Json(new { sayi });
    }

    private async Task<int?> GetIsciIdAsync()
    {
        var appUser = await _userManager.GetUserAsync(User);
        return appUser?.IsciId;
    }

    private IActionResult RedirectToLogin() =>
        RedirectToAction("Login", "Account", new { area = "" });
}
