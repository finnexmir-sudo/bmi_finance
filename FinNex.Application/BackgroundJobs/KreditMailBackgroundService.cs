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

            // Yalnız oxunmamış "Online Kredit" mail-lərini gətir
            var query = SearchQuery.SubjectContains("Online Kredit")
                .And(SearchQuery.NotSeen);

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

                    // Mail-ə toxunmuruq — "Bax" vuranda oxunmuş olacaq
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

            _logger.LogInformation("KreditMail PARSE: body uzunluq={Len}, ilk 200={Body}",
                body.Length, body.Length > 200 ? body[..200] : body);

            // Sətirləri ayır və key:value cütlükləri çıxar
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
                    {
                        fields[key] = val;
                    }
                }
            }

            _logger.LogInformation("KreditMail PARSE: {Count} sahə tapıldı: {Keys}",
                fields.Count, string.Join(", ", fields.Keys));

            var muraciet = new KreditMuraciet
            {
                MailMessageId = messageId,
                MuracietTarixi = DateTime.Now,
                Status = KreditMuracietStatus.Yeni
            };

            // Sahələri tap — key-in hissəsinə görə axtar
            muraciet.AdSoyadAtaAdi = FindField(fields, "Soyad") ?? FindField(fields, "ad v") ?? "Naməlum";
            muraciet.FIN = FindField(fields, "FİN") ?? FindField(fields, "FIN");
            muraciet.Valyuta = FindField(fields, "valyuta") ?? "AZN";
            muraciet.KreditMuddeti = FindField(fields, "müddət") ?? FindField(fields, "muddet");
            muraciet.IsYeri = FindField(fields, "İş yer") ?? FindField(fields, "Is yer") ?? FindField(fields, "yeriniz");
            muraciet.Telefon = FindField(fields, "telefon");
            muraciet.Meqsed = FindField(fields, "məqsəd") ?? FindField(fields, "meqsed") ?? FindField(fields, "maqsad");
            muraciet.IP = FindField(fields, "Remote IP");

            // Kredit məbləği
            var meblegStr = FindField(fields, "kredit məbləğ") ?? FindField(fields, "kredit mebleq");
            if (meblegStr != null)
            {
                var numStr = new string(meblegStr.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                if (decimal.TryParse(numStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var mebleg))
                    muraciet.KreditMeblegi = mebleg;
            }

            // Əmək haqqı
            var haqqiStr = FindField(fields, "əmək haqq") ?? FindField(fields, "emek haqq");
            if (haqqiStr != null)
            {
                var numStr = new string(haqqiStr.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                if (decimal.TryParse(numStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var haqqi))
                    muraciet.EmekHaqqi = haqqi;
            }

            // Tarix
            var dateStr = FindField(fields, "Date");
            var timeStr = FindField(fields, "Time");
            if (!string.IsNullOrEmpty(dateStr))
            {
                var dtStr = dateStr + (timeStr != null ? " " + timeStr : "");
                if (DateTime.TryParse(dtStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    muraciet.MuracietTarixi = dt;
            }

            _logger.LogInformation("KreditMail PARSE nəticə: Ad={Ad}, FIN={Fin}, Məbləğ={Meb}",
                muraciet.AdSoyadAtaAdi, muraciet.FIN, muraciet.KreditMeblegi);

            return muraciet;
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

    }
}
