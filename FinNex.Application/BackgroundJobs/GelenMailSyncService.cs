using FinNex.Application.Interfaces.Communication;
using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FinNex.Infrastructure.BackgroundJobs;

public class GelenMailSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GelenMailSyncService> _logger;
    private readonly IConfiguration _config;
    private readonly TimeSpan _interval;
    private readonly string _attachmentBaseDir;
    private DateTime _lastEscalationDate = DateTime.MinValue;

    public GelenMailSyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<GelenMailSyncService> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
        var minutes = config.GetValue("GelenMail:CheckIntervalMinutes", 5);
        _interval = TimeSpan.FromMinutes(minutes);
        _attachmentBaseDir = config["GelenMail:AttachmentDir"] ?? Path.Combine("wwwroot", "mail-qosmalar");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GelenMailSyncService başladı.");

        // startup-da 30 saniyə gözlə
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await SyncAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "GelenMailSyncService xətası"); }

            // Səhər saat 9:00-9:10 arasında gündəlik eskalasiya hesabatı
            try { await MaybeRunEscalationAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Eskalasiya hesabatı xətası"); }

            try { await Task.Delay(_interval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task MaybeRunEscalationAsync(CancellationToken ct)
    {
        var now = DateTime.Now;
        if (now.Date == _lastEscalationDate.Date) return;          // bu gün artıq göndərilib
        if (now.Hour != 9 || now.Minute > 10) return;             // yalnız 09:00–09:10

        _lastEscalationDate = now;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var today = now.Date;
        var inSevenDays = today.AddDays(7);

        var deadlineMails = await db.Set<GelenMail>()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.DedlaynTarix.HasValue)
            .Select(x => new { x.Movzu, x.DedlaynTarix, x.KimdenAd, x.KimdenEmail })
            .ToListAsync(ct);

        var geciken    = deadlineMails.Where(m => m.DedlaynTarix!.Value.Date < today).ToList();
        var yaxinlasan = deadlineMails.Where(m => m.DedlaynTarix!.Value.Date >= today && m.DedlaynTarix.Value.Date <= inSevenDays).ToList();

        if (geciken.Count == 0 && yaxinlasan.Count == 0) return;

        var lines = new System.Text.StringBuilder();
        if (geciken.Count > 0)
        {
            lines.Append($"🔴 Gecikənlər ({geciken.Count}): ");
            lines.Append(string.Join("; ", geciken.Take(3).Select(m => $"{m.Movzu} ({m.DedlaynTarix!.Value:dd.MM})")));
            if (geciken.Count > 3) lines.Append($" +{geciken.Count - 3}");
        }
        if (yaxinlasan.Count > 0)
        {
            if (lines.Length > 0) lines.Append(" | ");
            lines.Append($"🟡 Yaxınlaşanlar ({yaxinlasan.Count}): ");
            lines.Append(string.Join("; ", yaxinlasan.Take(3).Select(m => $"{m.Movzu} ({m.DedlaynTarix!.Value:dd.MM})")));
            if (yaxinlasan.Count > 3) lines.Append($" +{yaxinlasan.Count - 3}");
        }

        var metn = lines.ToString();
        var bashliq = $"Dedlayn xülasəsi — {geciken.Count} gecikən, {yaxinlasan.Count} yaxınlaşan";

        await SendEscalationBildirisAsync(scope, bashliq, metn, ct);
    }

    private async Task SendEscalationBildirisAsync(IServiceScope scope, string bashliq, string metn, CancellationToken ct)
    {
        try
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var bildirisService = scope.ServiceProvider.GetRequiredService<IBildirisService>();

            var rehberUsers = await userManager.GetUsersInRoleAsync(FinNex.Domain.RoleNames.Rehber);
            foreach (var user in rehberUsers)
            {
                if (user.IsciId is null) continue;
                await bildirisService.YaratAsync(
                    user.IsciId.Value,
                    BildirisNovu.YeniGelenMail,
                    bashliq,
                    metn,
                    "/User/GelenMail");
            }
            _logger.LogInformation("Eskalasiya hesabatı göndərildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eskalasiya bildiris göndərmə xətası");
        }
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        var host = _config["GelenMail:ImapServer"];
        var port = _config.GetValue("GelenMail:Port", 993);
        var email = _config["GelenMail:Email"];
        var password = _config["GelenMail:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("GelenMail IMAP konfiqurasiyası boşdur, sinxronizasiya atlanır.");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var extractor = scope.ServiceProvider.GetService<FinNex.Application.Interfaces.Communication.IAttachmentTextExtractor>();

        using var client = new ImapClient();
        await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.SslOnConnect, ct);
        await client.AuthenticateAsync(email, password, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, ct);

        // Only fetch messages not yet seen in DB — by Message-ID
        var knownIds = await db.Set<GelenMail>()
            .AsNoTracking()
            .Select(x => x.MessageId)
            .ToListAsync(ct);
        var knownSet = new HashSet<string>(knownIds);

        // Fetch last 100 messages
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

            // Save attachments
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

                var extractedText = extractor?.Extract(filePath, part.ContentType.MimeType);

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
            await db.Set<GelenMail>().AddRangeAsync(newMails, ct);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("GelenMailSync: {Count} yeni mail yükləndi.", newMails.Count);

            await SendRehberBildirisAsync(scope, newMails, ct);
        }

        await client.DisconnectAsync(true, ct);
    }

    private async Task SendRehberBildirisAsync(IServiceScope scope, List<GelenMail> newMails, CancellationToken ct)
    {
        try
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var bildirisService = scope.ServiceProvider.GetRequiredService<IBildirisService>();

            var rehberUsers = await userManager.GetUsersInRoleAsync(FinNex.Domain.RoleNames.Rehber);

            string bashliq, metn;
            if (newMails.Count == 1)
            {
                var m = newMails[0];
                var kimden = string.IsNullOrWhiteSpace(m.KimdenAd) ? m.KimdenEmail : m.KimdenAd;
                bashliq = "Yeni mail";
                metn = $"{kimden} — {m.Movzu}";
            }
            else
            {
                bashliq = $"{newMails.Count} yeni mail";
                metn = $"Gələn qutunuza {newMails.Count} yeni mail daxil oldu.";
            }

            var redirectUrl = "/User/GelenMail";

            foreach (var user in rehberUsers)
            {
                if (user.IsciId is null) continue;
                await bildirisService.YaratAsync(
                    user.IsciId.Value,
                    BildirisNovu.YeniGelenMail,
                    bashliq,
                    metn,
                    redirectUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rehber bildiris göndərmə xətası");
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Length > 100 ? name.Substring(0, 100) : name;
    }
}
