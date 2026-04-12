using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Kredit;
using FinNex.Domain.Interfaces;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class KreditMuracietController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;

    public KreditMuracietController(IUnitOfWork unitOfWork, IConfiguration config)
    {
        _unitOfWork = unitOfWork;
        _config = config;
    }

    private async Task<Isci?> GetCurrentIsciAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _unitOfWork.Repository<Isci>()
            .GetirAsync(x => x.AppUserId == userId && !x.Silinib);
    }

    // ══════════════════════════════════════════════════════
    // İŞÇİ (Kredit Mütəxəssisi) — müraciətlərə baxır, yoxlayır, komitəyə göndərir
    // ══════════════════════════════════════════════════════

    // ── GET /User/KreditMuraciet ────────────────────────────
    public async Task<IActionResult> Index(int? status)
    {
        ViewData["Title"] = "Kredit Müraciətləri";

        var hamisi = await _unitOfWork.Repository<KreditMuraciet>()
            .Query().Where(x => !x.Silinib).ToListAsync();

        var muracietler = status.HasValue
            ? hamisi.Where(x => (int)x.Status == status.Value).ToList()
            : hamisi;

        ViewBag.SecilmisStatus = status;
        ViewBag.StatusSaylari = new Dictionary<int, int>
        {
            [0] = hamisi.Count(x => x.Status == KreditMuracietStatus.Yeni),
            [1] = hamisi.Count(x => x.Status == KreditMuracietStatus.Yoxlanilir),
            [2] = hamisi.Count(x => x.Status == KreditMuracietStatus.KomiteyeGonderildi),
            [3] = hamisi.Count(x => x.Status == KreditMuracietStatus.Tesdiqlenib),
            [4] = hamisi.Count(x => x.Status == KreditMuracietStatus.ReddEdilib)
        };

        return View(muracietler.OrderByDescending(x => x.MuracietTarixi).ToList());
    }

    // ── GET /User/KreditMuraciet/Detail/5 ───────────────────
    // İşçi baxışı — yoxlama + komitəyə göndərmə
    public async Task<IActionResult> Detail(int id)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .Query()
            .Include(x => x.BaxanIsci)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

        if (muraciet == null) return NotFound();

        // FİN tarixçəsi
        ViewBag.Tarixce = !string.IsNullOrEmpty(muraciet.FIN)
            ? await _unitOfWork.Repository<KreditMuraciet>()
                .Query()
                .Where(x => x.FIN == muraciet.FIN && x.Id != id && !x.Silinib)
                .OrderByDescending(x => x.MuracietTarixi)
                .ToListAsync()
            : new List<KreditMuraciet>();

        // Mail-i oxunmuş et (arxa planda, səhifəni yavaşlatmır)
        if (!string.IsNullOrEmpty(muraciet.MailMessageId))
        {
            _ = Task.Run(() => MarkMailAsReadAsync(muraciet.MailMessageId));
        }

        ViewData["Title"] = "Müraciət #" + id;
        return View(muraciet);
    }

    // ── POST /User/KreditMuraciet/IsciQiymetlendir ──────────
    // İşçi: yalnız Yeni → Yoxlanılır → Komitəyə göndər
    [HttpPost]
    public async Task<IActionResult> IsciQiymetlendir(int id, int yeniStatus, string? qeyd)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .GetirAsync(x => x.Id == id && !x.Silinib);
        if (muraciet == null) return NotFound();

        // İşçi yalnız bu statuslara dəyişə bilər
        if (yeniStatus > (int)KreditMuracietStatus.KomiteyeGonderildi)
        {
            TempData["Error"] = "Bu əməliyyat yalnız Kredit Komitəsinə aiddir.";
            return RedirectToAction("Detail", new { id });
        }

        var isci = await GetCurrentIsciAsync();

        muraciet.Status = (KreditMuracietStatus)yeniStatus;
        muraciet.Qeyd = qeyd;
        muraciet.BaxanIsciId = isci?.Id;
        muraciet.BaxilmaTarixi = DateTime.Now;

        await _unitOfWork.Repository<KreditMuraciet>().YenileAsync(muraciet);
        await _unitOfWork.YaddaSaxlaAsync();

        TempData["Success"] = yeniStatus == (int)KreditMuracietStatus.KomiteyeGonderildi
            ? "Müraciət Kredit Komitəsinə göndərildi."
            : "Status yeniləndi.";
        return RedirectToAction("Detail", new { id });
    }

    // ══════════════════════════════════════════════════════
    // KREDİT KOMİTƏSİ — qərar verir
    // ══════════════════════════════════════════════════════

    // ── GET /User/KreditMuraciet/Komite ─────────────────────
    public async Task<IActionResult> Komite()
    {
        ViewData["Title"] = "Kredit Komitəsi";

        var muracietler = await _unitOfWork.Repository<KreditMuraciet>()
            .Query()
            .Where(x => !x.Silinib && x.Status == KreditMuracietStatus.KomiteyeGonderildi)
            .Include(x => x.BaxanIsci)
            .OrderByDescending(x => x.BaxilmaTarixi)
            .ToListAsync();

        return View(muracietler);
    }

    // ── GET /User/KreditMuraciet/KomiteDetail/5 ─────────────
    // Komitə baxışı — tam dosye + qərar formu
    public async Task<IActionResult> KomiteDetail(int id)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .Query()
            .Include(x => x.BaxanIsci)
            .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

        if (muraciet == null) return NotFound();

        // FİN tarixçəsi
        ViewBag.Tarixce = !string.IsNullOrEmpty(muraciet.FIN)
            ? await _unitOfWork.Repository<KreditMuraciet>()
                .Query()
                .Where(x => x.FIN == muraciet.FIN && x.Id != id && !x.Silinib)
                .OrderByDescending(x => x.MuracietTarixi)
                .ToListAsync()
            : new List<KreditMuraciet>();

        ViewData["Title"] = "Komitə — Müraciət #" + id;
        return View(muraciet);
    }

    // ── POST /User/KreditMuraciet/KomiteQerar ───────────────
    // Komitə: yalnız Təsdiq / Rədd
    [HttpPost]
    public async Task<IActionResult> KomiteQerar(int id, int yeniStatus, string? qeyd,
        string? komiteProtokolNo)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .GetirAsync(x => x.Id == id && !x.Silinib);
        if (muraciet == null) return NotFound();

        // Komitə yalnız Təsdiq və ya Rədd edə bilər
        if (yeniStatus != (int)KreditMuracietStatus.Tesdiqlenib &&
            yeniStatus != (int)KreditMuracietStatus.ReddEdilib)
        {
            TempData["Error"] = "Komitə yalnız təsdiq və ya rədd edə bilər.";
            return RedirectToAction("KomiteDetail", new { id });
        }

        muraciet.Status = (KreditMuracietStatus)yeniStatus;
        muraciet.KomiteQerari = yeniStatus == (int)KreditMuracietStatus.Tesdiqlenib ? "Təsdiqlənib" : "Rədd edilib";
        muraciet.KomiteProtokolNo = komiteProtokolNo;
        muraciet.KomiteQerarTarixi = DateTime.Now;
        muraciet.Qeyd = qeyd;

        await _unitOfWork.Repository<KreditMuraciet>().YenileAsync(muraciet);
        await _unitOfWork.YaddaSaxlaAsync();

        TempData["Success"] = "Komitə qərarı qeydə alındı.";
        return RedirectToAction("Komite");
    }

    // ── GET /User/KreditMuraciet/Qerarlar ──────────────────
    public async Task<IActionResult> Qerarlar()
    {
        ViewData["Title"] = "Komitə Qərarları";

        var muracietler = await _unitOfWork.Repository<KreditMuraciet>()
            .Query()
            .Where(x => !x.Silinib &&
                (x.Status == KreditMuracietStatus.Tesdiqlenib || x.Status == KreditMuracietStatus.ReddEdilib))
            .Include(x => x.BaxanIsci)
            .OrderByDescending(x => x.KomiteQerarTarixi)
            .ToListAsync();

        return View(muracietler);
    }

    // ── POST /User/KreditMuraciet/Delete/5 ──────────────────
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .GetirAsync(x => x.Id == id);
        if (muraciet != null)
        {
            await _unitOfWork.Repository<KreditMuraciet>().DeleteAsync(muraciet.Id);
            await _unitOfWork.YaddaSaxlaAsync();
        }
        TempData["Success"] = "Müraciət silindi.";
        return RedirectToAction("Index");
    }

    // ── Mail-i IMAP-da oxunmuş et ──────────────────────────
    private async Task MarkMailAsReadAsync(string messageId)
    {
        try
        {
            var server = _config["KreditMail:ImapServer"] ?? "imap.titan.email";
            var port = _config.GetValue("KreditMail:Port", 993);
            var email = _config["KreditMail:Email"] ?? "";
            var password = _config["KreditMail:Password"] ?? "";

            if (string.IsNullOrEmpty(password) || password == "PAROL_BURA_YAZIN") return;

            using var client = new ImapClient();
            await client.ConnectAsync(server, port, true);
            await client.AuthenticateAsync(email, password);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            // Subject ilə axtar
            var uids = await inbox.SearchAsync(SearchQuery.SubjectContains("Online Kredit"));

            foreach (var uid in uids)
            {
                var msg = await inbox.GetMessageAsync(uid);
                var msgId = msg.MessageId ?? "";
                if (msgId == messageId || uid.ToString() == messageId)
                {
                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
                    break;
                }
            }

            await client.DisconnectAsync(true);
        }
        catch { }
    }
}
