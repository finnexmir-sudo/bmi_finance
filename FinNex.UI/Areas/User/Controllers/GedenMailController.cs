using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Services.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
// Gələn Mail ilə EYNİ giriş qaydası — qutu şəxsidir, bu da elə həmin qutunun
// "Göndərilənlər" qovluğudur. Ayrıca icazə YOXDUR, `mail_istifade` kifayətdir.
[MailIcazesi]
public class GedenMailController : Controller
{
    private readonly IGedenMailService _mailService;
    private readonly IGedenMailImapSyncer _imapSyncer;
    private readonly IMailIzlemeService _izlemeService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IDataProtector _protector;

    public GedenMailController(
        IGedenMailService mailService,
        IGedenMailImapSyncer imapSyncer,
        IMailIzlemeService izlemeService,
        UserManager<AppUser> userManager,
        IDataProtectionProvider dpProvider)
    {
        _mailService = mailService;
        _imapSyncer = imapSyncer;
        _izlemeService = izlemeService;
        _userManager = userManager;
        _protector = dpProvider.CreateProtector("MailSmtpParol");
    }

    public async Task<IActionResult> Index(string? q, string? tarixden, string? tarixe, bool? qosmali, bool? izleme, int page = 1)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null) return Unauthorized();

        DateTime? tdFrom = DateTime.TryParse(tarixden, out var tf) ? tf : null;
        DateTime? tdTo   = DateTime.TryParse(tarixe,   out var tt) ? tt : null;

        List<string>? izlenenEmaillar = null;
        if (izleme == true)
            izlenenEmaillar = await _izlemeService.GetEmailSiyahisiAsync(appUser.Id);

        var mails = await _mailService.GetListAsync(appUser.Id, q, tdFrom, tdTo, qosmali, page, 50, izlenenEmaillar);

        ViewBag.Axtaris      = q;
        ViewBag.Tarixden     = tarixden;
        ViewBag.Tarixe       = tarixe;
        ViewBag.Qosmali      = qosmali;
        ViewBag.IzlemeFilter = izleme;
        ViewBag.IzlemeSayi   = (await _izlemeService.GetListAsync(appUser.Id)).Count;
        ViewBag.Page         = page;
        ViewData["Title"]    = "Göndərilən Maillər";

        return View(mails);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null) return Unauthorized();

        var mail = await _mailService.GetDetailAsync(id, appUser.Id);
        if (mail == null) return NotFound();

        ViewData["Title"] = mail.Movzu;
        return View(mail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null) return Unauthorized();

        await _mailService.SilAsync(id, appUser.Id);
        TempData["Success"] = "Mail silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManualSync()
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (string.IsNullOrWhiteSpace(appUser?.MailSmtpEmail) || string.IsNullOrWhiteSpace(appUser?.MailSmtpParol))
            return Json(new { success = false, count = 0, message = "Profil → Mail Ayarları bölməsindən e-poçt və şifrəni əlavə edin." });

        try
        {
            var password = MailParolQoruyucu.Ac(_protector, appUser.MailSmtpParol);
            if (password == null)
                return Json(new { success = false, count = 0, message = MailParolQoruyucu.AcilmadiMesaji });

            var imapHost = GelenMailImapSyncer.DeriveImapHost(appUser.MailSmtpHost, appUser.MailSmtpEmail);
            var count = await _imapSyncer.SyncSentNowAsync(imapHost, appUser.MailSmtpEmail, password, appUser.Id);
            return Json(new { success = true, count, message = count > 0 ? $"{count} yeni göndərilmiş mail tapıldı." : "Yeni mail yoxdur." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, count = 0, message = $"IMAP xətası: {ex.Message}" });
        }
    }

    // ── GET /User/GedenMail/MailPanel/5 — split-pane partial ────
    [HttpGet]
    public async Task<IActionResult> MailPanel(int id)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null) return Unauthorized();
        var mail = await _mailService.GetDetailAsync(id, appUser.Id);
        if (mail == null) return NotFound();
        return PartialView("_MailDetail", mail);
    }
}
