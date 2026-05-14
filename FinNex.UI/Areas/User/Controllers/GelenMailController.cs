using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize(Roles = $"{RoleNames.Rehber},{RoleNames.Admin}")]
public class GelenMailController : Controller
{
    private readonly IGelenMailService _mailService;
    private readonly IAnthropicService _ai;
    private readonly IIsciService _isciService;
    private readonly IXatirlatmaService _xatirlatmaService;
    private readonly UserManager<AppUser> _userManager;

    public GelenMailController(
        IGelenMailService mailService,
        IAnthropicService ai,
        IIsciService isciService,
        IXatirlatmaService xatirlatmaService,
        UserManager<AppUser> userManager)
    {
        _mailService = mailService;
        _ai = ai;
        _isciService = isciService;
        _xatirlatmaService = xatirlatmaService;
        _userManager = userManager;
    }

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

        await _mailService.TapaAsync(mailId, isciIds, qeyd, appUser.Id);
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

        // İstifadəçinin əsas şöbəsini tap
        var deptId = await _userManager.Users
            .Where(u => u.Id == appUser.Id)
            .SelectMany(u => u.UserDepartments.Where(ud => ud.Esasdir))
            .Select(ud => ud.DepartmentId)
            .FirstOrDefaultAsync();

        var senedId = await _mailService.SenedeCevir(mailId, qosmaId, appUser.Id, deptId);

        if (senedId.HasValue)
            TempData["Success"] = "Qoşma sənəd dövriyyəsinə əlavə edildi.";
        else
            TempData["Error"] = "Sənədə çevirmə uğursuz oldu. Fayl tapılmadı.";

        return RedirectToAction(nameof(Detail), new { id = mailId });
    }

    [HttpGet]
    public async Task<IActionResult> OxunmamisSay()
    {
        var say = await _mailService.GetOxunmamisSayAsync();
        return Json(new { say });
    }
}
