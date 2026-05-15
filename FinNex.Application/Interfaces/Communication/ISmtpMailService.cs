namespace FinNex.Application.Interfaces.Communication;

public interface ISmtpMailService
{
    Task<(bool Ok, string? Xeta)> GonderAsync(
        string kimeEmail,
        string kimeAd,
        string movzu,
        string metin,
        string fromEmail,
        string fromParol,
        string? smtpHost = null,
        string? replyToMessageId = null);
}
