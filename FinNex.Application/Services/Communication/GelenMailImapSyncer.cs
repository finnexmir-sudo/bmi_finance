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

    public async Task<int> SyncNowAsync(string imapHost, string email, string password, CancellationToken ct = default)
    {
        var knownIds = await _db.Set<GelenMail>()
            .AsNoTracking()
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
                KimdenAd = msg.From.Mailboxes.FirstOrDefault()?.Name ?? "",
                KimdenEmail = msg.From.Mailboxes.FirstOrDefault()?.Address ?? "",
                Movzu = msg.Subject ?? "",
                MetinHtml = msg.HtmlBody ?? "",
                MetinDuz = msg.TextBody ?? msg.HtmlBody ?? "",
                AlinmaTarixi = msg.Date.UtcDateTime,
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
