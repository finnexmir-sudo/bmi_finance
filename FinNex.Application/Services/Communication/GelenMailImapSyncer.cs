using FinNex.Application.Interfaces.Communication;
using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.Communication;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FinNex.Application.Services.Communication;

public class GelenMailImapSyncer : IGelenMailImapSyncer
{
    private readonly AppDbContext _db;
    private readonly ILogger<GelenMailImapSyncer> _logger;
    private readonly IAttachmentTextExtractor? _extractor;
    private readonly string _attachmentBaseDir;

    public GelenMailImapSyncer(
        AppDbContext db,
        IConfiguration config,
        ILogger<GelenMailImapSyncer> logger,
        IAttachmentTextExtractor? extractor = null)
    {
        _db = db;
        _logger = logger;
        _extractor = extractor;
        _attachmentBaseDir = config["GelenMail:AttachmentDir"] ?? Path.Combine("wwwroot", "mail-qosmalar");
    }

    public async Task<int> SyncNowAsync(string imapHost, string email, string password, int sahibUserId, CancellationToken ct = default)
    {
        // Dublikat yoxlaması YALNIZ bu sahibin mailləri üzrə — eyni mail başqa
        // sahibə də gələ bilər, ona görə qlobal deyil, sahibə görə yoxlanır.
        var knownIds = await _db.Set<GelenMail>()
            .AsNoTracking()
            .Where(x => x.SahibUserId == sahibUserId)
            .Select(x => x.MessageId)
            .ToListAsync(ct);
        var knownSet = new HashSet<string>(knownIds);

        using var client = new ImapClient();
        await client.ConnectAsync(imapHost, 993, MailKit.Security.SecureSocketOptions.SslOnConnect, ct);
        await client.AuthenticateAsync(email, password, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, ct);

        var count = inbox.Count;
        var startIndex = Math.Max(0, count - 100);
        var summaries = await inbox.FetchAsync(startIndex, -1,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags, ct);

        var newMails = new List<GelenMail>();

        foreach (var summary in summaries)
        {
            var msgId = summary.Envelope?.MessageId ?? summary.UniqueId.ToString();
            if (knownSet.Contains(msgId)) continue;

            MimeMessage msg;
            try { msg = await inbox.GetMessageAsync(summary.UniqueId, ct); }
            catch { continue; }

            var gelenMail = new GelenMail
            {
                MessageId = msgId,
                SahibUserId = sahibUserId,
                KimdenAd = msg.From.Mailboxes.FirstOrDefault()?.Name ?? "",
                KimdenEmail = msg.From.Mailboxes.FirstOrDefault()?.Address ?? "",
                Movzu = msg.Subject ?? "",
                MetinHtml = msg.HtmlBody ?? "",
                MetinDuz = msg.TextBody ?? msg.HtmlBody ?? "",
                AlinmaTarixi = msg.Date.LocalDateTime,
                YaradilmaTarixi = DateTime.Now,
                Qosmalar = new List<GelenMailQosma>()
            };

            Directory.CreateDirectory(_attachmentBaseDir);
            foreach (var attachment in msg.Attachments)
            {
                if (attachment is not MimePart part) continue;

                var safeFileName = SanitizeFileName(part.FileName ?? "attachment");
                var dir = Path.Combine(_attachmentBaseDir, $"mail_{DateTime.Now:yyyyMMdd}");
                Directory.CreateDirectory(dir);
                var filePath = Path.Combine(dir, $"{Guid.NewGuid():N}_{safeFileName}");

                using (var fs = File.Create(filePath))
                    await part.Content.DecodeToAsync(fs, ct);

                var extractedText = _extractor?.Extract(filePath, part.ContentType.MimeType);

                gelenMail.Qosmalar.Add(new GelenMailQosma
                {
                    FaylAdi = part.FileName ?? "attachment",
                    ContentType = part.ContentType.MimeType,
                    OlcuBayt = new FileInfo(filePath).Length,
                    FaylYolu = filePath,
                    CixarilmisMetin = extractedText,
                    YaradilmaTarixi = DateTime.Now
                });
            }

            newMails.Add(gelenMail);
            knownSet.Add(msgId);
        }

        if (newMails.Count > 0)
        {
            await _db.Set<GelenMail>().AddRangeAsync(newMails, ct);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("GelenMailImapSyncer: {Count} yeni mail yükləndi.", newMails.Count);
        }

        await client.DisconnectAsync(true, ct);
        return newMails.Count;
    }

    // ── "\Seen" yazması (16.10.2026) ────────────────────────────────────
    // FinNex-də oxunan mail serverin ÖZ qutusunda da `\Seen` olaraq
    // işarələnir — eyni qutuya IMAP ilə qoşulan Outlook (və ya başqa
    // müştəri) də onu oxunmuş görsün. Bax CLAUDE.md "Mail — Oxundu Statusu
    // Serverə Yazılır" bölməsi.
    public async Task OxunduIsaretleServerdeAsync(string imapHost, string email, string password,
        IReadOnlyCollection<string> messageIds, CancellationToken ct = default)
    {
        if (messageIds == null || messageIds.Count == 0) return;

        try
        {
            using var client = new ImapClient();
            await client.ConnectAsync(imapHost, 993, MailKit.Security.SecureSocketOptions.SslOnConnect, ct);
            await client.AuthenticateAsync(email, password, ct);

            // ReadWrite MƏCBURİDİR — SyncNowAsync-in ReadOnly açması ilə QARIŞDIRMA,
            // bayraq yazmaq üçün yazma icazəsi tələb olunur.
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, ct);

            foreach (var msgId in messageIds)
            {
                if (string.IsNullOrWhiteSpace(msgId)) continue;

                try
                {
                    // UID SAXLANMIR (GelenMail.MessageId yalnız Message-Id header-idir) —
                    // ona görə hər dəfə TƏZƏ axtarış aparılır. Bu, UID-i keşləməkdən
                    // YAVAŞDIR, amma TƏHLÜKƏSİZDİR: UIDVALIDITY dəyişsə (qutu yenidən
                    // qurulsa) köhnə UID SƏHV mailə yapışardı — axtarış heç vaxt bunu etmir,
                    // ya DOĞRU mailı tapır, ya da HEÇ NƏYİ (səhv mailı YOX).
                    var uids = await inbox.SearchAsync(MailKit.Search.SearchQuery.HeaderContains("Message-Id", msgId), ct);
                    if (uids.Count == 0)
                    {
                        _logger.LogWarning(
                            "GelenMail \\Seen yazma: mail serverdə tapılmadı (silinib/köçürülüb ola bilər) — Message-Id={MsgId}",
                            msgId);
                        continue;
                    }

                    await inbox.AddFlagsAsync(uids, MessageFlags.Seen, true, ct);
                }
                catch (Exception ex)
                {
                    // Bir mailin axtarışı/yazması uğursuz olsa, qalanlara MANE OLMASIN.
                    _logger.LogWarning(ex, "GelenMail \\Seen yazma xətası — Message-Id={MsgId}", msgId);
                }
            }

            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            // Best-effort: server əlçatmaz olsa belə YEREL "oxundu" statusu
            // artıq yazılıb (çağıran tərəf — GelenMailService — DB-ni bundan
            // ƏVVƏL yeniləyir) — bu metod heç vaxt istisna ATMIR.
            _logger.LogWarning(ex, "GelenMail \\Seen yazma — IMAP bağlantısı alınmadı ({Email})", email);
        }
    }

    // smtp.titan.email → imap.titan.email; auto-detect if no smtpHost
    public static string DeriveImapHost(string? smtpHost, string email)
    {
        if (!string.IsNullOrWhiteSpace(smtpHost) && smtpHost.StartsWith("smtp.", StringComparison.OrdinalIgnoreCase))
            return "imap." + smtpHost[5..];

        var domain = email.Split('@').LastOrDefault()?.ToLower() ?? "";
        return domain switch
        {
            "gmail.com"                                   => "imap.gmail.com",
            "yahoo.com"                                   => "imap.mail.yahoo.com",
            "outlook.com" or "hotmail.com" or "live.com"  => "imap-mail.outlook.com",
            _ when domain.Contains("titan")               => "imap.titan.email",
            _                                             => $"imap.{domain}"
        };
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Length > 100 ? name.Substring(0, 100) : name;
    }
}
