using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Kredit;

namespace FinNex.Application.Interfaces.Kredit;

/// <summary>
/// VM 98.2.1 — bazar faiz dərəcəsi tarixçəsi (mühasib əl ilə idarə edir).
///
/// DİQQƏT: metod imzasını dəyişəndə İNTERFEYS + İMPLEMENTASİYA + BÜTÜN
/// ÇAĞIRIŞ YERLƏRİ eyni anda yenilənməlidir (CLAUDE.md — CS0535/CS1501 tələsi).
/// </summary>
public interface IKreditFaizDerecesiService
{
    /// <summary>Bütün dərəcələr — valyuta, sonra tarix üzrə azalan.</summary>
    Task<IList<KreditFaizDerecesiDto>> HamisiniGetirAsync();

    /// <summary>
    /// Verilmiş tarixdə qüvvədə olan dərəcə: <c>Tarix &lt;= hedef</c> olan ƏN SON sətir.
    /// Tapılmasa <c>null</c> — çağıran tərəf bunu «təyin edilməyib» kimi göstərməlidir,
    /// 0 SAYMAMALIDIR (0 dərəcə düsturda sıfıra bölmə deməkdir).
    /// </summary>
    Task<KreditFaizDerecesiDto?> QuvvededirAsync(DateTime hedef, string valyutaKodu);

    Task<Result<int>> YaratAsync(KreditFaizDerecesiCreateDto dto, int userId);
    Task<Result> YenileAsync(KreditFaizDerecesiCreateDto dto, int userId);
    Task<Result> SilAsync(int id, int userId);
}
