namespace FinNex.Application.Services.Sms;

/// <summary>
/// appsettings.json "Goyercin" bölməsindən bind olunur.
/// PublicKey və PrivateKey production-da User Secrets / environment variable-da saxlanılmalıdır.
/// </summary>
public class GoyercinSettings
{
    public string BaseUrl { get; set; } = "https://api.poctgoyercini.com";
    public string PublicKey { get; set; } = "";
    public string PrivateKey { get; set; } = "";
    public string Originator { get; set; } = "IranBankBMI";
    public string Purpose { get; set; } = "INF";        // INF / OTP / ADV — hesabımızda yalnız INF aktivdir
    public string Encoding { get; set; } = "LATIN";     // LATIN / TURKISH / UNICODE
    public bool DryRun { get; set; } = false;           // true olsa, real API çağırışı edilmir, sadəcə log
}
