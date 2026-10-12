using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;

namespace FinNex.Application.Interfaces.Communication;

public interface IMailIzlemeService
{
    Task<List<MailIzlemeUnvaniDto>> GetListAsync(int sahibUserId);
    Task<Result> EkleAsync(int sahibUserId, string email, string? ad);
    Task<Result> SilAsync(int id, int sahibUserId);

    /// <summary>
    /// Normallaşdırılmış (kiçik hərf, boşluqsuz) e-poçt siyahısı — "yalnız
    /// izlənənlər" filtrində və fon sinxronizasiyasının bildiriş qərarında
    /// işlədilir. Boş siyahı = heç bir ünvan əlavə edilməyib.
    /// </summary>
    Task<List<string>> GetEmailSiyahisiAsync(int sahibUserId);
}
