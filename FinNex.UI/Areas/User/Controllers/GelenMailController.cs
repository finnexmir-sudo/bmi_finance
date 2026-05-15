using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize(Roles = $"{RoleNames.Rehber},{RoleNames.Admin}")]
public class GelenMailController : Controller
{
    private readonly IGelenMailService _mailService;
    private readonly IAnthropicService _ai;
    private readonly ISmtpMailService _smtp;
    private readonly IIsciService _isciService;
    private readonly IXatirlatmaService _xatirlatmaService;
    private readonly IBildirisService _bildirisService;
    private readonly IGelenMailImapSyncer _imapSyncer;
    private readonly UserManager<AppUser> _userManager;
    private readonly IDataProtector _protector;

    public GelenMailController(
        IGelenMailService mailService,
        IAnthropicService ai,
        ISmtpMailService smtp,
        IIsciService isciService,
        IXatirlatmaService xatirlatmaService,
        IBildirisService bildirisService,
        IGelenMailImapSyncer imapSyncer,
        UserManager<AppUser> userManager,
        IDataProtectionProvider dpProvider)
    {
        _mailService = mailService;
        _ai = ai;
        _smtp = smtp;
        _isciService = isciService;
        _xatirlatmaService = xatirlatmaService;
        _bildirisService = bildirisService;
        _imapSyncer = imapSyncer;
        _userManager = userManager;
        _protector = dpProvider.CreateProtector("MailSmtpParol");
    }

    public async Task<IActionResult> Index(
        bool? oxunmamis, int? isciId, string? q,
        string? tarixden, string? tarixe,
        bool? tapshirildi, bool? qosmali, string? deadline,
        int page = 1)
    {
        DateTime? tdFrom = DateTime.TryParse(tarixden, out var tf) ? tf : null;
        DateTime? tdTo   = DateTime.TryParse(tarixe,   out var tt) ? tt : null;

        var mails = await _mailService.GetListAsync(
            oxunmamis, isciId, page, 50, q,
            tdFrom, tdTo, tapshirildi, qosmali, deadline);
        var oxunmamisSay = await _mailService.GetOxunmamisSayAsync();
        var iscilerResult = await _isciService.HamisiniGetirAsync();
        var isciler = iscilerResult.Success ? iscilerResult.Data!.ToList() : new();

        ViewBag.OxunmamisSay  = oxunmamisSay;
        ViewBag.Isciler        = isciler;
        ViewBag.OxunmamisFilter = oxunmamis;
        ViewBag.IsciFilter     = isciId;
        ViewBag.Axtaris        = q;
        ViewBag.Tarixden       = tarixden;
        ViewBag.Tarixe         = tarixe;
        ViewBag.Tapshirildi    = tapshirildi;
        ViewBag.Qosmali        = qosmali;
        ViewBag.DeadlineFilter = deadline;
        ViewBag.Page           = page;
        ViewData["Title"]      = "Gələn Maillər";

        return View(mails);
    }

    // Səhifəni dərhal qaytarır — AI ayrı endpoint ilə çağırılır
    public async Task<IActionResult> Detail(int id)
    {
        var mail = await _mailService.GetDetailAsync(id);
        if (mail == null) return NotFound();

        await _mailService.OxunduIsareEtAsync(id);

        var iscilerResult = await _isciService.HamisiniGetirAsync();
        var isciler = iscilerResult.Success ? iscilerResult.Data!.ToList() : new();
        ViewBag.Isciler = isciler;
        ViewData["Title"] = mail.Movzu;

        return View(mail);
    }

    // AJAX: AI analizi çağır (arxa planda)
    [HttpPost]
    public async Task<IActionResult> RunAI(int id)
    {
        var mail = await _mailService.GetDetailAsync(id);
        if (mail == null) return NotFound();

        if (!string.IsNullOrEmpty(mail.AIXulase))
            return Json(new { xulase = mail.AIXulase, dedlaynTarix = mail.DedlaynTarix?.ToString("dd.MM.yyyy"), dedlaynNov = mail.DedlaynNov, dedlaynQeyd = mail.DedlaynQeyd });

        var qosmaMetinler = mail.Qosmalar
            .Where(q => !string.IsNullOrWhiteSpace(q.CixarilmisMetin))
            .Select(q => (q.FaylAdi, q.ContentType, q.CixarilmisMetin!))
            .ToList();

        var netic = await _ai.MailTahlilEtAsync(
            $"{mail.KimdenAd} <{mail.KimdenEmail}>",
            mail.Movzu,
            mail.MetinDuz,
            qosmaMetinler);

        await _mailService.SaveAINeticAsync(id, netic);

        // Deadline tapıldısa Xatırlatma yarat
        if (netic.DedlaynTarix.HasValue && netic.DedlaynTarix.Value > DateTime.Now)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId != null)
            {
                var novLabel = netic.DedlaynNov switch
                {
                    "Gorus"    => "Görüş",
                    "Hesabat"  => "Hesabat",
                    "Muqavile" => "Müqavilə",
                    "SonTarix" => "Son tarix",
                    _          => "Deadline"
                };
                try
                {
                    await _xatirlatmaService.SistemXatirlatmasiYaratAsync(new XatirlatmaSistemCreateDto
                    {
                        IsciId = appUser.IsciId.Value,
                        Bashliq = $"{novLabel}: {mail.Movzu}",
                        Qeyd = netic.DedlaynQeyd ?? $"Mail dedlayni: {netic.DedlaynTarix.Value:dd.MM.yyyy}",
                        XatirlatmaTarixi = netic.DedlaynTarix.Value.AddDays(-1).Date.AddHours(9),
                        Nov = XatirlatmaNov.Sistem,
                        EntityTipi = XatirlatmaEntityTipi.GelenMail,
                        EntityId = id
                    });
                }
                catch { }
            }
        }

        return Json(new
        {
            xulase = netic.Xulase,
            dedlaynTarix = netic.DedlaynTarix?.ToString("dd.MM.yyyy"),
            dedlaynNov = netic.DedlaynNov,
            dedlaynQeyd = netic.DedlaynQeyd
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tapa(int mailId, List<int> isciIds, string? qeyd)
    {
        if (isciIds == null || isciIds.Count == 0)
        {
            TempData["Error"] = "Ən az bir işçi seçin.";
            return RedirectToAction(nameof(Detail), new { id = mailId });
        }

        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null) return Unauthorized();

        var mail = await _mailService.GetDetailAsync(mailId);
        await _mailService.TapaAsync(mailId, isciIds, qeyd, appUser.Id);

        // Hər tapşırılan işçiyə bildiris göndər
        if (mail != null)
        {
            var senderName = string.IsNullOrWhiteSpace(appUser.Ad)
                ? (User.Identity?.Name ?? "Rəhbər")
                : $"{appUser.Ad} {appUser.Soyad}".Trim();
            foreach (var isciId in isciIds)
            {
                try
                {
                    await _bildirisService.YaratAsync(
                        isciId,
                        BildirisNovu.YeniTapshiriq,
                        "Sizə mail tapşırıldı",
                        $"{senderName}: {mail.Movzu}" + (string.IsNullOrWhiteSpace(qeyd) ? "" : $" — {qeyd}"),
                        "/User/Tapshiriq");
                }
                catch { }
            }
        }

        TempData["Success"] = $"{isciIds.Count} işçiyə tapşırıldı.";
        return RedirectToAction(nameof(Detail), new { id = mailId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        await _mailService.SilAsync(id);
        TempData["Success"] = "Mail silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SenedeCevir(int mailId, int qosmaId)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null) return Unauthorized();

        // Duplikat yoxlaması — əvvəlcədən sənəd dövriyyəsindədirsə əlavə etmə
        var mailDetail = await _mailService.GetDetailAsync(mailId);
        if (mailDetail?.SenedId.HasValue == true)
        {
            TempData["Error"] = "Bu mail artıq sənəd dövriyyəsindədir.";
            return RedirectToAction(nameof(Detail), new { id = mailId });
        }

        var senedId = await _mailService.SenedeCevir(mailId, qosmaId, appUser.Id, appUser.IsciId);

        if (senedId.HasValue)
            TempData["Success"] = "Qoşma sənəd dövriyyəsinə əlavə edildi.";
        else
            TempData["Error"] = "Sənədə çevirmə uğursuz oldu. Fayl tapılmadı.";

        return RedirectToAction(nameof(Detail), new { id = mailId });
    }

    [HttpGet]
    public async Task<IActionResult> TapshirilanMailler()
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null) return Unauthorized();

        var list = await _mailService.GetRehberTapshirilanMaillerAsync(appUser.Id);
        ViewData["Title"] = "Tapşırılan Maillər";
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> OxunmamisSay()
    {
        var say = await _mailService.GetOxunmamisSayAsync();
        return Json(new { say });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManualSync()
    {
        try
        {
            var count = await _imapSyncer.SyncNowAsync();
            return Json(new { success = true, count, message = count > 0 ? $"{count} yeni mail tapıldı." : "Yeni mail yoxdur." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, count = 0, message = $"Xəta: {ex.Message}" });
        }
    }

    // ── GET /User/GelenMail/Cavab/5 ─────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Cavab(int id)
    {
        var mail = await _mailService.GetDetailAsync(id);
        if (mail == null) return NotFound();

        var dto = new MailCavabDto
        {
            MailId          = id,
            MessageId       = mail.MessageId,
            KimeEmail       = mail.KimdenEmail,
            KimeAd          = mail.KimdenAd,
            Movzu           = "Re: " + mail.Movzu,
            AsilMailMovzu   = mail.Movzu,
            AsilMailTarix   = mail.AlinmaTarixi,
            AsilMailMetinDuz = mail.MetinDuz?.Length > 800
                ? mail.MetinDuz.Substring(0, 800) + "\n[...]"
                : mail.MetinDuz,
            AIXulase        = mail.AIXulase,
            CavabMetni      = ""
        };

        ViewData["Title"] = "Cavab — " + mail.Movzu;
        return View(dto);
    }

    // ── POST AJAX /User/GelenMail/AICavabHazirla ────────────────
    [HttpPost]
    public async Task<IActionResult> AICavabHazirla(int id)
    {
        var mail = await _mailService.GetDetailAsync(id);
        if (mail == null) return NotFound();

        var metin = await _ai.MailCavabHazirlaAsync(
            $"{mail.KimdenAd} <{mail.KimdenEmail}>",
            mail.Movzu,
            mail.MetinDuz ?? "",
            mail.AIXulase);

        return Json(new { metin });
    }

    // ── POST /User/GelenMail/CavabGonder ────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CavabGonder(MailCavabDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.KimeEmail) || string.IsNullOrWhiteSpace(dto.CavabMetni))
        {
            TempData["Error"] = "Alıcı e-poçt və mətn mütləqdir.";
            return RedirectToAction(nameof(Cavab), new { id = dto.MailId });
        }

        var appUser = await _userManager.GetUserAsync(User);
        if (string.IsNullOrWhiteSpace(appUser?.MailSmtpEmail) || string.IsNullOrWhiteSpace(appUser?.MailSmtpParol))
        {
            TempData["Error"] = "SMTP məlumatları tapılmadı. Profil → Mail Ayarları bölməsindən e-poçt və şifrəni əlavə edin.";
            return RedirectToAction(nameof(Cavab), new { id = dto.MailId });
        }

        var smtpParol = _protector.Unprotect(appUser.MailSmtpParol);

        var (ok, xeta) = await _smtp.GonderAsync(
            dto.KimeEmail,
            dto.KimeAd,
            dto.Movzu,
            dto.CavabMetni,
            appUser.MailSmtpEmail,
            smtpParol,
            appUser.MailSmtpHost,
            string.IsNullOrWhiteSpace(dto.MessageId) ? null : dto.MessageId);

        if (ok)
        {
            await _mailService.CavabVerildiIsareEtAsync(dto.MailId);
            TempData["Success"] = $"Cavab {dto.KimeEmail} ünvanına göndərildi.";
            return RedirectToAction(nameof(Detail), new { id = dto.MailId });
        }
        else
        {
            TempData["Error"] = $"Göndərmə xətası: {xeta}";
            return RedirectToAction(nameof(Cavab), new { id = dto.MailId });
        }
    }
}
