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
    public async Task<HashSet<string>> OxunduIsaretleServerdeAsync(string imapHost, string email, string password,
        IReadOnlyCollection<string> messageIds, CancellationToken ct = default)
    {
        var ugurluMsgIdler = new HashSet<string>();
        if (messageIds == null || messageIds.Count == 0) return ugurluMsgIdler;

        // 21.10.2026 — DİAQNOSTİK LOG ƏLAVƏ EDİLDİ. Əvvəlki versiya yalnız XƏTA
        // halında log yazırdı — UĞURLU yazmada HEÇ BİR iz qalmırdı. Canlı testdə
        // Titan hələ "oxunmamış" göstərməyə davam edəndə bu, "tapıldı/yazıldı" ilə
        // "heç vaxt çağırılmadı/sakitcə uğursuz oldu" hallarını ayırd etməyi
        // mümkünsüz edirdi — log faylına baxmaqla belə səbəb bilinmirdi. İndi hər
        // addım (başlanğıc, server tapma, bayraq yazma) Information/Warning
        // səviyyəsində qeyd olunur.
        _logger.LogInformation(
            "GelenMail \\Seen yazma BAŞLADI — Email={Email}, Host={Host}, MesajSayi={Say}",
            email, imapHost, messageIds.Count);

        try
        {
            using var client = new ImapClient();
            await client.ConnectAsync(imapHost, 993, MailKit.Security.SecureSocketOptions.SslOnConnect, ct);
            await client.AuthenticateAsync(email, password, ct);

            // ReadWrite MƏCBURİDİR — SyncNowAsync-in ReadOnly açması ilə QARIŞDIRMA,
            // bayraq yazmaq üçün yazma icazəsi tələb olunur.
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, ct);

            // 21.10.2026 — İKİNCİ DALĞA: server-side `SEARCH HEADER Message-Id …`
            // Titan-da DƏFƏLƏRLƏ "tapılmadı" qaytardı, hətta elə indicə
            // `SyncNowAsync`-in ÖZÜNÜN yüklədiyi mailə görə belə — loglar bunu
            // dəqiq sübut etdi (bax CLAUDE.md). SEARCH əməliyyatı Titan-da
            // etibarsızdır. Həll: `SyncNowAsync`-in artıq İŞLƏK olan üsulunu
            // TƏKRARLA — son mesajları FETCH edib Message-Id-ni YERLİ olaraq
            // müqayisə et, serverə axtarış GÖNDƏRMƏ.
            var count = inbox.Count;
            var startIndex = Math.Max(0, count - 300);
            var summaries = await inbox.FetchAsync(startIndex, -1,
                MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope, ct);

            var msgIdToUid = new Dictionary<string, UniqueId>();
            foreach (var summary in summaries)
            {
                var envelopeMsgId = summary.Envelope?.MessageId;
                if (!string.IsNullOrWhiteSpace(envelopeMsgId))
                    msgIdToUid[envelopeMsgId] = summary.UniqueId;
            }

            foreach (var msgId in messageIds)
            {
                if (string.IsNullOrWhiteSpace(msgId)) continue;

                try
                {
                    if (!msgIdToUid.TryGetValue(msgId, out var uid))
                    {
                        _logger.LogWarning(
                            "GelenMail \\Seen yazma: mail serverdə (son {Pencere} mesaj içində) tapılmadı — Message-Id={MsgId}",
                            summaries.Count, msgId);
                        continue;
                    }

                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, ct);
                    ugurluMsgIdler.Add(msgId);
                    _logger.LogInformation(
                        "GelenMail \\Seen yazıldı — Message-Id={MsgId}, UID={Uid}",
                        msgId, uid);
                }
                catch (Exception ex)
                {
                    // Bir mailin yazması uğursuz olsa, qalanlara MANE OLMASIN.
                    // `ugurluMsgIdler`-ə ƏLAVƏ EDİLMİR — çağıran tərəf bunu "hələ
                    // təsdiqlənməyib" sayıb növbəti açılışda TƏKRAR cəhd edəcək.
                    _logger.LogWarning(ex, "GelenMail \\Seen yazma xətası — Message-Id={MsgId}", msgId);
                }
            }

            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            // Best-effort: server əlçatmaz olsa belə YEREL "oxundu" statusu
            // artıq yazılıb (çağıran tərəf — GelenMailService — DB-ni bundan
            // ƏVVƏL yeniləyir) — bu metod heç vaxt istisna ATMIR. Bura qədər
            // uğurla yazılmış (əgər varsa) Message-Id-lər hələ `ugurluMsgIdler`-dədir.
            _logger.LogWarning(ex, "GelenMail \\Seen yazma — IMAP bağlantısı alınmadı ({Email})", email);
        }

        return ugurluMsgIdler;
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
