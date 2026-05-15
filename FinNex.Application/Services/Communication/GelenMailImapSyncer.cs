using FinNex.Application.Interfaces.Communication;
using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.Communication;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FinNex.Application.Services.Communication;

public class GelenMailImapSyncer : IGelenMailImapSyncer
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IAttachmentTextExtractor? _extractor;
    private readonly ILogger<GelenMailImapSyncer> _logger;
    private readonly string _attachmentBaseDir;

    public GelenMailImapSyncer(
        AppDbContext db,
        IConfiguration config,
        ILogger<GelenMailImapSyncer> logger,
        IAttachmentTextExtractor? extractor = null)
    {
        _db = db;
        _config = config;
        _logger = logger;
        _extractor = extractor;
        _attachmentBaseDir = config["GelenMail:AttachmentDir"] ?? Path.Combine("wwwroot", "mail-qosmalar");
    }

    public async Task<int> SyncNowAsync(CancellationToken ct = default)
    {
        var host = _config["GelenMail:ImapServer"];
        var port = _config.GetValue("GelenMail:Port", 993);
        var email = _config["GelenMail:Email"];
        var password = _config["GelenMail:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("GelenMail IMAP konfiqurasiyası boşdur.");
            return -1;
        }

        var knownIds = await _db.Set<GelenMail>()
            .AsNoTracking()
            .Select(x => x.MessageId)
            .ToListAsync(ct);
        var knownSet = new HashSet<string>(knownIds);

        using var client = new ImapClient();
        await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.SslOnConnect, ct);
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

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Length > 100 ? name.Substring(0, 100) : name;
    }
}
