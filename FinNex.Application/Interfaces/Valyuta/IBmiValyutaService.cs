using FinNex.Application.DTOs.Valyuta;

namespace FinNex.Application.Interfaces.Valyuta;

/// <summary>
/// Valyuta siyahısı — BMI `kurval` cədvəlindən oxunur (YALNIZ SELECT).
///
/// FinNex-də bu siyahı üçün cədvəl SAXLANILMIR: 6 sətirdir, dəyişməsi nadirdir
/// və mənbəyi BMI-nin əsas bankçılıq sistemidir. Canlı oxumaqla həmişə sinxron
/// qalırıq — köçürsək, BMI yeni valyuta əlavə edəndə bizdə görünməzdi.
///
/// ⚠️ Layihədə `IValyutaService` adlı BAŞQA servis var (ödəniş tapşırığı modulu,
/// FinNex-in öz `Valyuta` cədvəli). Bu ikisi ayrı mənbələrdir — qarışdırma.
/// </summary>
public interface IBmiValyutaService
{
    /// <summary>
    /// Valyutalar (kod + ad), koda görə sıralı.
    /// Oracle əlçatmaz olarsa siyahı BOŞ QAYTARILMIR — sabit ehtiyat siyahı
    /// işə düşür ki, forma istifadəyə yararsız olmasın (bax: implementasiya).
    /// </summary>
    Task<IList<BmiValyutaDto>> SiyahiAsync(CancellationToken ct = default);
}
