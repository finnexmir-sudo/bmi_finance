using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Yardim;

namespace FinNex.Application.Interfaces.Yardim;

/// <summary>
/// Səhifə təlimatları — «?» düyməsinin və `/Yardim` indeksinin mənbəyi.
///
/// QAYDA: açar HƏMİŞƏ `YardimAcar.Qur(...)` ilə qurulur. Çağıran tərəf
/// açarı əl ilə yığmasın — kiçik/böyük hərf fərqi yardımı «tapılmadı» edir
/// və heç bir xəta çıxmır.
/// </summary>
public interface ISehifeYardimiService
{
    /// <summary>
    /// Marşrut açarına görə «?» panelinin məlumatı.
    /// Qeyd yoxdursa `Var = false` qaytarır (istisna atmır) — panel
    /// «hələ yazılmayıb» göstərsin, səhifə sınmasın.
    /// </summary>
    Task<YardimPanelDto> PanelAsync(string acar, bool adminmi);

    /// <summary>Qısa ünvana (slug) görə — çatda paylaşılan link üçün.</summary>
    Task<YardimPanelDto?> SlugaGoreAsync(string slug, bool adminmi);

    /// <summary>İndeks siyahısı — modul üzrə qruplaşdırma və axtarış üçün.</summary>
    Task<IReadOnlyList<YardimListDto>> SiyahiAsync(string? axtaris, bool adminmi);

    /// <summary>Admin redaktoru üçün mövcud qeyd (yoxdursa null).</summary>
    Task<YardimUpsertDto?> RedakteMelumatiAsync(int id);

    /// <summary>Yeni qeyd formasının ilkin dəyərləri (açar öncədən doldurulur).</summary>
    Task<YardimUpsertDto> YeniMelumatAsync(string acar);

    /// <summary>Yaradır və ya yeniləyir. Slug boş gələrsə başlıqdan qurulur.</summary>
    Task<Result> YaddaSaxlaAsync(YardimUpsertDto dto, int userId);

    Task<Result> SilAsync(int id, int userId);

    /// <summary>«?» açılanda baxış sayğacını artırır (səssiz — xəta maneə olmamalıdır).</summary>
    Task BaxisArtirAsync(string acar);

    /// <summary>
    /// Əhatə: verilmiş səhifə açarları siyahısı üçün «yazılıbmı» vəziyyəti.
    /// Açarları UI qatı verir (marşrut cədvəlini yalnız o bilir).
    /// </summary>
    Task<IReadOnlyList<YardimEhateDto>> EhateAsync(IEnumerable<string> acarlar);
}
