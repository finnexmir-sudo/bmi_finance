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

// IMAP "Göndərilənlər" (Sent) qovluğunun sinxronu — GelenMailImapSyncer-in
// (Inbox) GÜZGÜSÜ, 12.10.2026. Yalnız GÖSTƏRMƏ üçündür, real göndərmə SMTP
// ilə gedir (GelenMailController.CavabGonder və s.) — bu servis heç nə
// göndərmir, yalnız poçt qutusunun artıq göndərdiyi mailləri oxuyur.
public class GedenMailImapSyncer : IGedenMailImapSyncer
{
    private readonly AppDbContext _db;
    private readonly ILogger<GedenMailImapSyncer> _logger;
    private readonly IAttachmentTextExtractor? _extractor;
    private readonly string _attachmentBaseDir;

    // "Göndərilənlər" qovluğunun adı provayderdən provayderə dəyişir. Əvvəlcə
    // IMAP SPECIAL-USE/XLIST genişlənməsinə (MailKit `SpecialFolder.Sent`) etibar
    // edilir, dəstəklənmirsə bu siyahı üzrə AD-a görə axtarılır.
    private static readonly string[] BilinenSentAdlari =
    {
        "Sent", "Sent Items", "Sent Messages", "Sent Mail",
        "INBOX.Sent", "INBOX/Sent", "INBOX.Sent Items"
    };

    public GedenMailImapSyncer(
        AppDbContext db,
        IConfiguration config,
        ILogger<GedenMailImapSyncer> logger,
        IAttachmentTextExtractor? extractor = null)
    {
        _db = db;
        _logger = logger;
        _extractor = extractor;
        _attachmentBaseDir = config["GelenMail:AttachmentDir"] ?? Path.Combine("wwwroot", "mail-qosmalar");
    }

    public async Task<int> SyncSentNowAsync(string imapHost, string email, string password, int sahibUserId, CancellationToken ct = default)
    {
        var knownIds = await _db.Set<GedenMail>()
            .AsNoTracking()
            .Where(x => x.SahibUserId == sahibUserId)
            .Select(x => x.MessageId)
            .ToListAsync(ct);
        var knownSet = new HashSet<string>(knownIds);

        using var client = new ImapClient();
        await client.ConnectAsync(imapHost, 993, MailKit.Security.SecureSocketOptions.SslOnConnect, ct);
        await client.AuthenticateAsync(email, password, ct);

        var sentFolder = await TapSentQovlugunuAsync(client, ct);
        if (sentFolder == null)
        {
            _logger.LogWarning("GedenMail: {Email} üçün Göndərilənlər qovluğu tapılmadı.", email);
            await client.DisconnectAsync(true, ct);
            return 0;
        }

        await sentFolder.OpenAsync(FolderAccess.ReadOnly, ct);

        var count = sentFolder.Count;
        var startIndex = Math.Max(0, count - 100);
        var summaries = await sentFolder.FetchAsync(startIndex, -1,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags, ct);

        var newMails = new List<GedenMail>();

        foreach (var summary in summaries)
        {
            var msgId = summary.Envelope?.MessageId ?? summary.UniqueId.ToString();
            if (knownSet.Contains(msgId)) continue;

            MimeMessage msg;
            try { msg = await sentFolder.GetMessageAsync(summary.UniqueId, ct); }
            catch { continue; }

            var aliciMailboxlar = msg.To.Mailboxes.Concat(msg.Cc.Mailboxes).ToList();
            var ilkAlici = aliciMailboxlar.FirstOrDefault();

            var gedenMail = new GedenMail
            {
                MessageId = msgId,
                SahibUserId = sahibUserId,
                KimeAd = ilkAlici?.Name ?? "",
                KimeEmail = ilkAlici?.Address ?? "",
                KimeToplu = string.Join(", ", aliciMailboxlar.Select(m => string.IsNullOrWhiteSpace(m.Name) ? m.Address : $"{m.Name} <{m.Address}>")),
                Movzu = msg.Subject ?? "",
                MetinHtml = msg.HtmlBody ?? "",
                MetinDuz = msg.TextBody ?? msg.HtmlBody ?? "",
                GonderilmeTarixi = msg.Date.UtcDateTime,
                YaradilmaTarixi = DateTime.Now,
                Qosmalar = new List<GedenMailQosma>()
            };

            Directory.CreateDirectory(_attachmentBaseDir);
            foreach (var attachment in msg.Attachments)
            {
                if (attachment is not MimePart part) continue;

                var safeFileName = SanitizeFileName(part.FileName ?? "attachment");
                var dir = Path.Combine(_attachmentBaseDir, $"mail_sent_{DateTime.Now:yyyyMMdd}");
                Directory.CreateDirectory(dir);
                var filePath = Path.Combine(dir, $"{Guid.NewGuid():N}_{safeFileName}");

                using (var fs = File.Create(filePath))
                    await part.Content.DecodeToAsync(fs, ct);

                var extractedText = _extractor?.Extract(filePath, part.ContentType.MimeType);

                gedenMail.Qosmalar.Add(new GedenMailQosma
                {
                    FaylAdi = part.FileName ?? "attachment",
                    ContentType = part.ContentType.MimeType,
                    OlcuBayt = new FileInfo(filePath).Length,
                    FaylYolu = filePath,
                    CixarilmisMetin = extractedText,
                    YaradilmaTarixi = DateTime.Now
                });
            }

            newMails.Add(gedenMail);
            knownSet.Add(msgId);
        }

        if (newMails.Count > 0)
        {
            await _db.Set<GedenMail>().AddRangeAsync(newMails, ct);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("GedenMailImapSyncer: {Count} yeni göndərilmiş mail yükləndi.", newMails.Count);
        }

        await client.DisconnectAsync(true, ct);
        return newMails.Count;
    }

    private async Task<IMailFolder?> TapSentQovlugunuAsync(ImapClient client, CancellationToken ct)
    {
        // 1) SPECIAL-USE/XLIST — server dəstəkləyirsə ən etibarlı yol.
        try
        {
            var special = client.GetFolder(SpecialFolder.Sent);
            if (special != null) return special;
        }
        catch { /* dəstəklənmir — ad üzrə axtarışa keç */ }

        // 2) Ad üzrə axtarış — şəxsi namespace-dəki bütün qovluqları gəz.
        try
        {
            var root = client.GetFolder(client.PersonalNamespaces[0]);
            var hamisi = await root.GetSubfoldersAsync(false, ct);

            var tamUygun = hamisi.FirstOrDefault(f =>
                BilinenSentAdlari.Any(ad => string.Equals(f.Name, ad, StringComparison.OrdinalIgnoreCase)));
            if (tamUygun != null) return tamUygun;

            // Son çara — adında "sent" keçən ilk qovluq.
            return hamisi.FirstOrDefault(f => f.Name.IndexOf("sent", StringComparison.OrdinalIgnoreCase) >= 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GedenMail: qovluq siyahısı alınmadı.");
            return null;
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Length > 100 ? name.Substring(0, 100) : name;
    }
}
