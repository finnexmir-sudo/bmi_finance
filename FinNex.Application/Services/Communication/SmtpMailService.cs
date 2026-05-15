using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FinNex.Application.Services.Communication;

public class SmtpMailService : ISmtpMailService
{
    private readonly SmtpSettings _cfg;

    public SmtpMailService(IOptions<SmtpSettings> opts)
    {
        _cfg = opts.Value;
    }

    public async Task<(bool Ok, string? Xeta)> GonderAsync(
        string kimeEmail,
        string kimeAd,
        string movzu,
        string metin,
        string? replyToMessageId = null)
    {
        if (string.IsNullOrWhiteSpace(_cfg.Host) || string.IsNullOrWhiteSpace(_cfg.FromEmail))
            return (false, "SMTP konfiqurasiyası tamamlanmayıb (appsettings: SmtpSettings).");

        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_cfg.FromName, _cfg.FromEmail));
            msg.To.Add(new MailboxAddress(kimeAd, kimeEmail));
            msg.Subject = movzu;

            if (!string.IsNullOrWhiteSpace(replyToMessageId))
            {
                msg.InReplyTo = replyToMessageId;
                msg.References.Add(replyToMessageId);
            }

            // HTML + düz mətn hər ikisi
            var builder = new BodyBuilder
            {
                TextBody = metin,
                HtmlBody = "<pre style=\"font-family:sans-serif;white-space:pre-wrap\">" +
                           System.Net.WebUtility.HtmlEncode(metin) + "</pre>"
            };
            msg.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            var socketOpts = _cfg.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : (_cfg.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            await smtp.ConnectAsync(_cfg.Host, _cfg.Port, socketOpts);
            await smtp.AuthenticateAsync(_cfg.UserName, _cfg.Password);
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
