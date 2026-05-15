using FinNex.Application.Interfaces.Communication;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FinNex.Application.Services.Communication;

public class SmtpMailService : ISmtpMailService
{
    public async Task<(bool Ok, string? Xeta)> GonderAsync(
        string kimeEmail,
        string kimeAd,
        string movzu,
        string metin,
        string fromEmail,
        string fromParol,
        string? smtpHost = null,
        string? replyToMessageId = null)
    {
        if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(fromParol))
            return (false, "SMTP məlumatları daxil edilməyib. Profilinizə Mail Ayarları bölməsindən əlavə edin.");

        try
        {
            var (autoHost, port) = GetSmtpHost(fromEmail);
            var host = !string.IsNullOrWhiteSpace(smtpHost) ? smtpHost.Trim() : autoHost;

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("", fromEmail));
            msg.To.Add(new MailboxAddress(kimeAd, kimeEmail));
            msg.Subject = movzu;

            if (!string.IsNullOrWhiteSpace(replyToMessageId))
            {
                msg.InReplyTo = replyToMessageId;
                msg.References.Add(replyToMessageId);
            }

            var builder = new BodyBuilder
            {
                TextBody = metin,
                HtmlBody = "<pre style=\"font-family:sans-serif;white-space:pre-wrap\">" +
                           System.Net.WebUtility.HtmlEncode(metin) + "</pre>"
            };
            msg.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(fromEmail, fromParol);
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static (string host, int port) GetSmtpHost(string email)
    {
        var domain = email.Split('@').LastOrDefault()?.ToLower() ?? "";
        return domain switch
        {
            "gmail.com"                                     => ("smtp.gmail.com", 587),
            "yahoo.com"                                     => ("smtp.mail.yahoo.com", 587),
            "outlook.com" or "hotmail.com" or "live.com"   => ("smtp.office365.com", 587),
            _ when domain.Contains("titan")                 => ("smtp.titan.email", 587),
            _                                               => ($"smtp.{domain}", 587)
        };
    }
}
