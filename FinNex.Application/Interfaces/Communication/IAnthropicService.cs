using FinNex.Application.DTOs.Communication;

namespace FinNex.Application.Interfaces.Communication;

public interface IAnthropicService
{
    Task<AIMailTahlilNetic> MailTahlilEtAsync(string kimden, string movzu, string metin,
        List<(string FileName, string ContentType, string Content)> qosmalar);
}
