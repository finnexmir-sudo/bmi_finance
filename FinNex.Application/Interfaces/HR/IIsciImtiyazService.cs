using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Imtiyaz;

namespace FinNex.Application.Interfaces.HR
{
    /// <summary>
    /// İşçiyə HR tərəfindən verilmiş fərdi imtiyaz/istisna qeydləri (07.10.2026,
    /// istifadəçi tələbi). YALNIZ GÖSTƏRİŞDİR — heç bir hesablamaya,
    /// Davamiyyət statusuna və ya avtomatik qaydaya təsir etmir.
    /// </summary>
    public interface IIsciImtiyazService
    {
        /// <summary>İşçi profilindəki "İmtiyazlar" sekmesi üçün — hamısı (aktiv/deaktiv).</summary>
        Task<IList<IsciImtiyazDto>> IsciUzreHamisiniGetirAsync(int isciId);

        /// <summary>
        /// Dashboard kartı üçün — yalnız BU GÜN qüvvədə olan aktiv qeydlər
        /// (<c>Aktivdir</c>, <c>BaslamaTarixi &lt;= bugün</c>,
        /// <c>BitmeTarixi == null || BitmeTarixi &gt;= bugün</c>).
        /// </summary>
        Task<IList<IsciImtiyazDto>> AktivImtiyazlarAsync(int isciId);

        Task<Result<int>> YaratAsync(IsciImtiyazCreateDto dto, int userId);
        Task<Result> YenileAsync(IsciImtiyazCreateDto dto, int userId);

        /// <summary>Silmir — <c>Aktivdir</c> sahəsini dəyişir (tarixçə qalsın).</summary>
        Task<Result> AktivlikDeyisAsync(int id, bool aktivdir, int userId);

        Task<Result> SilAsync(int id, int userId);
    }
}
