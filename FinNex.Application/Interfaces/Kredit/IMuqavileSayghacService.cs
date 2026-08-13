using FinNex.Application.DTOs.Kredit.Muqavile;
using FinNex.Domain.Entities.Kredit;

namespace FinNex.Application.Interfaces.Kredit;

/// <summary>
/// Müqavilə nömrə sayğacları — FinNex bazasında (MuqavileSayghaci).
///
/// BMI (odb.muqavile_nomreleri) əvəzinə artıq bu işlədilir. Sayğac (Novu, Il)
/// üzrə SON VERİLMİŞ nömrəni saxlayır; növbəti = SonNomre + 1. Həmin il üçün
/// sətir yoxdursa nömrələmə 1-dən başlayır (il dönümü avtomatikdir).
/// </summary>
public interface IMuqavileSayghacService
{
    /// <summary>
    /// Bir nömrə ayırır və sayğacı artırır. Yazma əməliyyatıdır.
    /// yaz=false olduqda HEÇ NƏ yazılmır, yalnız növbəti nömrə qaytarılır (preview).
    /// </summary>
    Task<int> NomreAyirAsync(MuqavileNomreNovu novu, int il, bool yaz, CancellationToken ct = default);

    /// <summary>
    /// Ardıcıl `adet` nömrə ayırır (zaminliklər üçün) və sayğacı bir dəfə artırır.
    /// adet ≤ 0 olduqda boş siyahı qaytarır və sayğaca toxunmur.
    /// </summary>
    Task<List<int>> NomreAyirAsync(MuqavileNomreNovu novu, int il, int adet, bool yaz,
        CancellationToken ct = default);

    /// <summary>Bir sayğacın cari SonNomre dəyəri (sətir yoxdursa 0).</summary>
    Task<int> SonNomreAsync(MuqavileNomreNovu novu, int il);

    /// <summary>
    /// SonNomre-ni əl ilə təyin edir (köhnə nömrələrin davamı / köçürmə üçün).
    /// Növbəti generasiya SonNomre + 1-dən davam edir.
    /// </summary>
    Task SonNomreTeyinEtAsync(MuqavileNomreNovu novu, int il, int sonNomre, int? istifadeciId = null);

    /// <summary>Bütün sayğacların vəziyyəti — il üzrə qruplanmış (idarəetmə ekranı).</summary>
    Task<List<MuqavileSayghacDto>> HamisiniGetirAsync();
}
