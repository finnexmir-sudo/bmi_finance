using FinNex.Application.DTOs.Countrycode;

namespace FinNex.Application.Interfaces.Countrycode;

/// <summary>
/// Ölkə siyahısı — BMI `countrycode` cədvəlindən CANLI oxunur (idxal yoxdur).
/// `IBmiValyutaService` (kurval) ilə eyni yanaşma: kataloq BMI-nindir, biz
/// yalnız oxuyuruq ki, orada dəyişiklik olanda bizdə də dərhal görünsün.
/// </summary>
public interface IBmiOlkeService
{
    /// <summary>
    /// Bütün ölkələr, ada görə sıralanmış. Oracle əlçatmaz olsa sabit ehtiyat
    /// siyahı qaytarılır — forma sıradan çıxmır.
    /// </summary>
    Task<IList<BmiOlkeDto>> SiyahiAsync(CancellationToken ct = default);

    /// <summary>
    /// Oracle-dan gələn dəyəri müqaviləyə yazılacaq ADA çevirir.
    /// Dəyər həm KOD ("AZE"), həm də hazır AD ("Azərbaycan Respublikası") ola
    /// bilər — hansı olduğunu bilmək lazım deyil, hər ikisi yoxlanılır.
    /// Tapılmasa gələn dəyər olduğu kimi qaytarılır (məlumat itmir).
    /// </summary>
    Task<string?> AdaCevirAsync(string? kodVeyaAd, CancellationToken ct = default);
}
