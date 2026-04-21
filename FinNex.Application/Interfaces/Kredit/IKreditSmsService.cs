using FinNex.Domain.Entities.Kredit;

namespace FinNex.Application.Interfaces.Kredit
{
    /// <summary>
    /// Müştəriyə SMS göndərmə servisi. Hazırda yalnız cədvələ log yazır,
    /// gələcəkdə SMS gateway inteqrasiyası buraya əlavə olunacaq — interfeys
    /// dəyişmir.
    /// </summary>
    public interface IKreditSmsService
    {
        Task<KreditSmsLog> GonderAsync(int? muracietId, string telefon, string metn,
                                         string? sablon, int gonderenIsciId);
        Task<IList<KreditSmsLog>> MuracietUzreGetirAsync(int muracietId);
        Task<IList<KreditSmsLog>> SonGonderilenler(int say = 100);
    }
}
