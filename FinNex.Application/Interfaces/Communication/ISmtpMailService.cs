namespace FinNex.Application.Interfaces.Communication;

public interface ISmtpMailService
{
    Task<(bool Ok, string? Xeta)> GonderAsync(
        string kimeEmail,
        string kimeAd,
        string movzu,
        string metin,
        string? replyToMessageId = null);
}
