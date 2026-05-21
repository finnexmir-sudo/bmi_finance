namespace FinNex.Application.Services.Sms;

/// <summary>
/// SMS gateway-in (Göyərçin və ya gələcəkdə başqası) qaytardığı nəticə.
/// </summary>
public class SmsGatewayResult
{
    public bool Success { get; set; }
    public string? ExternalId { get; set; }      // gateway tərəfindən qaytarılan unique id (status sorğusu üçün)
    public string? RawResponse { get; set; }     // ham cavab (debug, log üçün)
    public string? ErrorMessage { get; set; }    // səhv halında izah

    public static SmsGatewayResult Ok(string externalId, string raw) =>
        new() { Success = true, ExternalId = externalId, RawResponse = raw };

    public static SmsGatewayResult Fail(string error, string? raw = null) =>
        new() { Success = false, ErrorMessage = error, RawResponse = raw };
}
