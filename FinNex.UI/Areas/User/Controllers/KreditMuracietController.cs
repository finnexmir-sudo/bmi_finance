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

        // Komitəyə göndərilmiş + bu gün qərar verilmiş
        var bugun = DateTime.Today;
        var muracietler = await _unitOfWork.Repository<KreditMuraciet>()
            .Query()
            .Where(x => !x.Silinib &&
                (x.Status == KreditMuracietStatus.KomiteyeGonderildi ||
                 (x.KomiteQerarTarixi != null && x.KomiteQerarTarixi >= bugun)))
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
        string? komiteProtokolNo, decimal? tesdiqMebleg, string? tesdiqMuddet,
        decimal? faizDerecesi, string? teminat)
    {
        var muraciet = await _unitOfWork.Repository<KreditMuraciet>()
            .GetirAsync(x => x.Id == id && !x.Silinib);
        if (muraciet == null) return NotFound();

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
        muraciet.TesdiqMebleg = tesdiqMebleg;
        muraciet.TesdiqMuddet = tesdiqMuddet;
        muraciet.FaizDerecesi = faizDerecesi;
        muraciet.Teminat = teminat;

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

    // ══════════════════════════════════════════════════════
    // MAİL — Yenilə düyməsi + Bax-da oxunmuş et
    // ══════════════════════════════════════════════════════

    // ── POST /User/KreditMuraciet/Yenile ────────────────────
    // Düyməyə basanda: mail-dən oxunmamışları gətir, dublikat yoxla, DB-yə yaz
    [HttpPost]
    public async Task<IActionResult> Yenile()
    {
        try
        {
            var server = _config["KreditMail:ImapServer"] ?? "imap.titan.email";
            var port = _config.GetValue("KreditMail:Port", 993);
            var email = _config["KreditMail:Email"] ?? "";
            var password = _config["KreditMail:Password"] ?? "";

            if (string.IsNullOrEmpty(password) || password == "PAROL_BURA_YAZIN")
            {
                TempData["Error"] = "Mail parolu konfiqurasiya olunmayıb.";
                return RedirectToAction("Index");
            }

            using var client = new ImapClient();
            await client.ConnectAsync(server, port, true);
            await client.AuthenticateAsync(email, password);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            // Yalnız oxunmamış "Online Kredit" mail-ləri
            var uids = await inbox.SearchAsync(
                SearchQuery.SubjectContains("Online Kredit").And(SearchQuery.NotSeen));

            int yeniSay = 0;

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid);
                var messageId = message.MessageId ?? uid.ToString();

                // DB-də varsa skip et (dublikat)
                var exists = await _unitOfWork.Repository<KreditMuraciet>()
                    .Query().AnyAsync(x => x.MailMessageId == messageId);
                if (exists) continue;

                // Body-ni al və təmizlə
                var body = message.TextBody;
                if (string.IsNullOrWhiteSpace(body))
                    body = message.HtmlBody ?? "";
                body = CleanHtml(body);

                var muraciet = ParseMailBody(body, messageId);
                if (muraciet != null)
                {
                    await _unitOfWork.Repository<KreditMuraciet>().YaratAsync(muraciet);
                    await _unitOfWork.YaddaSaxlaAsync();
                    yeniSay++;
                }
            }

            await client.DisconnectAsync(true);

            TempData["Success"] = yeniSay > 0
                ? $"{yeniSay} yeni müraciət gətirildi."
                : "Yeni müraciət yoxdur.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Mail xətası: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    // ── Bax vuranda mail-i oxunmuş et ───────────────────────
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

    // ══════════════════════════════════════════════════════
    // PARSER
    // ══════════════════════════════════════════════════════

    private static string CleanHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<br\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"</(p|div|tr|li)>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[ \t]+", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n+", "\n");
        return text.Trim();
    }

    private static KreditMuraciet? ParseMailBody(string body, string messageId)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        var lines = body.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx > 0 && colonIdx < line.Length - 1)
            {
                var key = line[..colonIdx].Trim();
                var val = line[(colonIdx + 1)..].Trim();
                if (!string.IsNullOrEmpty(val) && val.Length < 300)
                    fields[key] = val;
            }
        }

        if (fields.Count < 2) return null;

        var m = new KreditMuraciet
        {
            MailMessageId = messageId,
            MuracietTarixi = DateTime.Now,
            Status = KreditMuracietStatus.Yeni,
            AdSoyadAtaAdi = FindField(fields, "Soyad") ?? FindField(fields, "ad v") ?? "Naməlum",
            FIN = FindField(fields, "FİN") ?? FindField(fields, "FIN"),
            Valyuta = FindField(fields, "valyuta") ?? "AZN",
            KreditMuddeti = FindField(fields, "müddət") ?? FindField(fields, "muddet"),
            IsYeri = FindField(fields, "İş yer") ?? FindField(fields, "Is yer") ?? FindField(fields, "yeriniz"),
            Telefon = FindField(fields, "telefon"),
            Meqsed = FindField(fields, "məqsəd") ?? FindField(fields, "meqsed"),
            IP = FindField(fields, "Remote IP")
        };

        var meblegStr = FindField(fields, "kredit məbləğ") ?? FindField(fields, "kredit mebleq");
        if (meblegStr != null)
        {
            var numStr = new string(meblegStr.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            if (decimal.TryParse(numStr.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var mebleg))
                m.KreditMeblegi = mebleg;
        }

        var haqqiStr = FindField(fields, "əmək haqq") ?? FindField(fields, "emek haqq");
        if (haqqiStr != null)
        {
            var numStr = new string(haqqiStr.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            if (decimal.TryParse(numStr.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var haqqi))
                m.EmekHaqqi = haqqi;
        }

        var dateStr = FindField(fields, "Date");
        var timeStr = FindField(fields, "Time");
        if (!string.IsNullOrEmpty(dateStr))
        {
            if (DateTime.TryParse(dateStr + " " + (timeStr ?? ""),
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
                m.MuracietTarixi = dt;
        }

        return m;
    }

    private static string? FindField(Dictionary<string, string> fields, string search)
    {
        search = search.ToLowerInvariant();
        foreach (var kv in fields)
        {
            if (kv.Key.ToLowerInvariant().Contains(search))
                return kv.Value;
        }
        return null;
    }
}
