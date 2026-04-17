using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces.Communication
{
    /// <summary>
    /// Təsdiq iş axınlarında istifadə olunan bildiriş yönləndiricisi.
    /// Tək işçiyə, müəyyən rola sahib bütün istifadəçilərə və ya
    /// departamentə bağlı struktur rollarına bildiriş paylayır.
    /// Bildiriş göndərmə xətası heç bir biznes əməliyyatını dayandırmamalıdır.
    /// </summary>
    public interface IBildirisRouter
    {
        Task NotifyIsciAsync(
            int isciId,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl = null,
            int? mezuniyyetId = null,
            int? icazeId = null);

        Task NotifyRoleAsync(
            string roleName,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl = null,
            int? mezuniyyetId = null,
            int? icazeId = null,
            int? exceptIsciId = null);

        Task NotifyRolesAsync(
            IEnumerable<string> roleNames,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl = null,
            int? mezuniyyetId = null,
            int? icazeId = null,
            int? exceptIsciId = null);

        /// <summary>
        /// Verilən departamentdə müəyyən struktur rolunu (Şöbə Rəisi/Rəhbər/HR)
        /// daşıyan aktiv işçilərə bildiriş göndərir.
        /// </summary>
        Task NotifyDepartmentRoleAsync(
            int departamentId,
            StrukturRolTipi rolTipi,
            BildirisNovu nov,
            string bashliq,
            string metn,
            string? redirectUrl = null,
            int? mezuniyyetId = null,
            int? icazeId = null,
            int? exceptIsciId = null);
    }
}
