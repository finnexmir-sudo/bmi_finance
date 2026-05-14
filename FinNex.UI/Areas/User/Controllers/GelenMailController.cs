using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize(Roles = $"{RoleNames.Rehber},{RoleNames.Admin}")]
public class GelenMailController : Controller
{
    private readonly IGelenMailService _mailService;
    private readonly IAnthropicService _ai;
    private readonly IIsciService _isciService;
    private readonly UserManager<AppUser> _userManager;

    public GelenMailController(
        IGelenMailService mailService,
        IAnthropicService ai,
        IIsciService isciService,
        UserManager<AppUser> userManager)
    {
        _mailService = mailService;
        _ai = ai;
        _isciService = isciService;
        _userManager = userManager;
    }

    // GET /User/GelenMail
    public async Task<IActionResult> Index(bool? oxunmamis, int? isciId, int page = 1)
    {
        var mails = await _mailService.GetListAsync(oxunmamis, isciId, page, 50);
        var oxunmamisSay = await _mailService.GetOxunmamisSayAsync();
        var iscilerResult = await _isciService.HamisiniGetirAsync();
        var isciler = iscilerResult.Success ? iscilerResult.Data!.ToList() : new();

        ViewBag.OxunmamisSay = oxunmamisSay;
        ViewBag.Isciler = isciler;
        ViewBag.OxunmamisFilter = oxunmamis;
        ViewBag.IsciFilter = isciId;
        ViewBag.Page = page;
        ViewData["Title"] = "Gələn Maillər";

        return View(mails);
    }

    // GET /User/GelenMail/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var mail = await _mailService.GetDetailAsync(id);
        if (mail == null) return NotFound();

        // Mark as read
        await _mailService.OxunduIsareEtAsync(id);

        // Auto-run AI analysis if not done yet
        if (string.IsNullOrEmpty(mail.AIXulase))
        {
            var qosmaMetinler = mail.Qosmalar
                .Where(q => !string.IsNullOrWhiteSpace(q.CixarilmisMetin))
                .Select(q => (q.FaylAdi, q.ContentType, q.CixarilmisMetin!))
                .ToList();

            var xulase = await _ai.MailTahlilEtAsync(
                $"{mail.KimdenAd} <{mail.KimdenEmail}>",
                mail.Movzu,
                mail.MetinDuz,
                qosmaMetinler);

            // Save AI result
            await _mailService.SaveAIXulaseAsync(id, xulase);
            mail.AIXulase = xulase;
            mail.AITahlilTarixi = DateTime.Now;
        }

        var iscilerResult = await _isciService.HamisiniGetirAsync();
        var isciler = iscilerResult.Success ? iscilerResult.Data!.ToList() : new();
        ViewBag.Isciler = isciler;
        ViewData["Title"] = mail.Movzu;

        return View(mail);
    }

    // POST /User/GelenMail/Tapa
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tapa(int mailId, int isciId, string? qeyd)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null) return Unauthorized();

        await _mailService.TapaAsync(mailId, isciId, qeyd, appUser.Id);
        TempData["Success"] = "Mail işçiyə tapşırıldı.";
        return RedirectToAction(nameof(Detail), new { id = mailId });
    }

    // POST /User/GelenMail/SenedeCevir
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SenedeCevir(int mailId, int qosmaId)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null) return Unauthorized();

        var storageDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "mail-qosmalar");
        var senedId = await _mailService.SenedeCevir(mailId, qosmaId, appUser.Id, storageDir);

        if (senedId.HasValue)
        {
            TempData["Success"] = "Qoşma sənəd dövriyyəsinə əlavə edildi.";
            return RedirectToAction("Detail", "Sened", new { area = "User", id = senedId });
        }

        TempData["Error"] = "Sənədə çevirmə uğursuz oldu.";
        return RedirectToAction(nameof(Detail), new { id = mailId });
    }

    // GET /User/GelenMail/OxunmamisSay (for badge refresh)
    [HttpGet]
    public async Task<IActionResult> OxunmamisSay()
    {
        var say = await _mailService.GetOxunmamisSayAsync();
        return Json(new { say });
    }
}
