using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.Kredit;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FinNex.Infrastructure.BackgroundJobs
{
    public class KreditMailBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<KreditMailBackgroundService> _logger;
        private readonly IConfiguration _config;

        public KreditMailBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<KreditMailBackgroundService> logger,
            IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalMin = _config.GetValue("KreditMail:CheckIntervalMinutes", 5);

            // İlk başlanğıcda 30 saniyə gözlə (app tam yüklənsin)
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckMailAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "KreditMail: mail yoxlama xətası");
                }

                await Task.Delay(TimeSpan.FromMinutes(intervalMin), stoppingToken);
            }
        }

        private async Task CheckMailAsync(CancellationToken ct)
        {
            var server = _config["KreditMail:ImapServer"] ?? "imap.titan.email";
            var port = _config.GetValue("KreditMail:Port", 993);
            var email = _config["KreditMail:Email"] ?? "";
            var password = _config["KreditMail:Password"] ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || password == "PAROL_BURA_YAZIN")
            {
                _logger.LogWarning("KreditMail: email/parol konfiqurasiya olunmayıb");
                return;
            }

            using var client = new ImapClient();
            await client.ConnectAsync(server, port, true, ct);
            await client.AuthenticateAsync(email, password, ct);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, ct);

            // "Online Kredit" mövzulu bütün mailləri tap (oxunmuş/oxunmamış fərq etməz)
            var query = SearchQuery.SubjectContains("Online Kredit");

            var uids = await inbox.SearchAsync(query, ct);

            if (!uids.Any())
            {
                await client.DisconnectAsync(true, ct);
                return;
            }

            _logger.LogInformation("KreditMail: {Count} yeni müraciət tapıldı", uids.Count);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var uid in uids)
            {
                try
                {
                    var message = await inbox.GetMessageAsync(uid, ct);
                    var messageId = message.MessageId ?? uid.ToString();

                    // Artıq əlavə olunubsa skip et
                    var exists = await db.KreditMuracietler
                        .AnyAsync(x => x.MailMessageId == messageId, ct);

                    if (exists) continue;

                    var body = message.TextBody;
                    if (string.IsNullOrWhiteSpace(body))
                    {
                        body = message.HtmlBody ?? "";
                    }
                    // Hər halda HTML-i təmizlə
                    body = CleanHtml(body);
                    var muraciet = ParseMailBody(body, messageId);

                    if (muraciet != null)
                    {
                        db.KreditMuracietler.Add(muraciet);
                        await db.SaveChangesAsync(ct);
                        _logger.LogInformation("KreditMail: müraciət əlavə edildi - {Ad}", muraciet.AdSoyadAtaAdi);
                    }

                    // Mail-ə toxunmuruq — oxunmamış qalır
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "KreditMail: mail parse xətası (UID: {Uid})", uid);
                }
            }

            await client.DisconnectAsync(true, ct);
        }

        private KreditMuraciet? ParseMailBody(string body, string messageId)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;

            var muraciet = new KreditMuraciet
            {
                MailMessageId = messageId,
                MuracietTarixi = DateTime.Now,
                Status = KreditMuracietStatus.Yeni
            };

            // Sahələri parse et
            muraciet.AdSoyadAtaAdi = ExtractField(body, @"Soyad,?\s*ad\s+v[əe]\s+ata\s+ad[ıi]:\s*(.+)") ?? "Naməlum";
            muraciet.FIN = ExtractField(body, @"FİN\s+n[öo]mr[əe]si:\s*(.+)");
            muraciet.Valyuta = ExtractField(body, @"valyuta[sı]*:\s*(.+)") ?? "AZN";
            muraciet.KreditMuddeti = ExtractField(body, @"m[üu]dd[əe]ti:\s*(.+)");
            muraciet.IsYeri = ExtractField(body, @"[İi][şs]\s+yeri(?:niz)?:\s*(.+)");
            muraciet.Telefon = ExtractField(body, @"telefon\s+n[öo]mr[əe]si:\s*(.+)");
            muraciet.Meqsed = ExtractField(body, @"m[əe]qs[əe]d:\s*(.+)");
            muraciet.IP = ExtractField(body, @"Remote\s+IP:\s*(.+)");

            // Kredit məbləği
            var meblegStr = ExtractField(body, @"kredit\s+m[əe]bl[əe][ğg]i:\s*([\d.,]+)");
            if (decimal.TryParse(meblegStr?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var mebleg))
                muraciet.KreditMeblegi = mebleg;

            // Əmək haqqı
            var haqqiStr = ExtractField(body, @"[əe]m[əe]k\s+haqq[ıi]:\s*([\d.,]+)");
            if (decimal.TryParse(haqqiStr?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var haqqi))
                muraciet.EmekHaqqi = haqqi;

            // Tarix
            var dateStr = ExtractField(body, @"Date:\s*(.+)");
            var timeStr = ExtractField(body, @"Time:\s*(.+)");
            if (!string.IsNullOrEmpty(dateStr))
            {
                var dtStr = dateStr + (timeStr != null ? " " + timeStr : "");
                if (DateTime.TryParse(dtStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    muraciet.MuracietTarixi = dt;
            }

            return muraciet;
        }

        private static string CleanHtml(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            // <br> → yeni sətir
            text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            // <p>, <div> → yeni sətir
            text = Regex.Replace(text, @"</(p|div|tr|li)>", "\n", RegexOptions.IgnoreCase);
            // Bütün HTML tag-larını sil
            text = Regex.Replace(text, @"<[^>]+>", " ", RegexOptions.Compiled);
            // HTML entity decode
            text = System.Net.WebUtility.HtmlDecode(text);
            // Çoxlu boşluqları tək boşluğa
            text = Regex.Replace(text, @"[ \t]+", " ");
            // Çoxlu yeni sətirləri tək sətirə
            text = Regex.Replace(text, @"\n\s*\n+", "\n");
            return text.Trim();
        }

        private static string? ExtractField(string body, string pattern)
        {
            var match = Regex.Match(body, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (!match.Success) return null;
            var val = match.Groups[1].Value.Trim();
            // Yalnız ilk sətri götür
            var nlIdx = val.IndexOfAny(new[] { '\n', '\r' });
            if (nlIdx > 0) val = val[..nlIdx].Trim();
            // Artıq boşluqları təmizlə
            val = Regex.Replace(val, @"\s+", " ").Trim();
            return val.Length > 200 ? val[..200] : val;
        }
    }
}
