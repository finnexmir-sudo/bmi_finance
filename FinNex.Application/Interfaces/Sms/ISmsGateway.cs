using FinNex.Application.Services.Sms;

namespace FinNex.Application.Interfaces.Sms;

/// <summary>
/// SMS provayder üçün abstraksiya. Hazırda Göyərçin (Posta Güvercini) tətbiq olunur,
/// gələcəkdə başqa provayder lazım olsa yalnız yeni implementasiya əlavə olunacaq.
/// </summary>
public interface ISmsGateway
{
    /// <param name="telefon">Beynəlxalq formatda, "+" olmadan. Məs: 994551234567</param>
    /// <param name="metn">Göndərilən mətn (max 1000 simvol, LATIN)</param>
    Task<SmsGatewayResult> SendAsync(string telefon, string metn, CancellationToken ct = default);
}
