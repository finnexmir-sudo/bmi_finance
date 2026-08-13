using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Kredit.Muqavile;

namespace FinNex.Application.Interfaces.Kredit;

/// <summary>
/// BMI (odb.muqavile_nomreleri) sayğaclarının FinNex-ə birdəfəlik köçürülməsi.
/// Oracle YALNIZ OXUNUR (IOracleService → SELECT); yazı yalnız SQL Server-ədir.
///
/// ⚠️ SEMANTİKA ÇEVRİLMƏSİ: BMI-də `kr_zaminlik`/`kr_menzil` və digərləri NÖVBƏTİ
/// nömrəni saxlayır, `kr_zaminler` isə SONUNCUNU. FinNex-də hamısı SONUNCUDUR
/// (`SonNomre`), ona görə "növbəti saxlayan" sayğaclardan 1 çıxılır. Ekran hər
/// sətirdə həm xam Oracle dəyərini, həm çevrilmiş dəyəri, həm də nəticədə
/// veriləcək növbəti nömrəni göstərir — yazmazdan əvvəl gözlə yoxlanılsın deyə.
/// </summary>
public interface IMuqavileSayghacImportService
{
    // Oracle və FinNex sayğaclarını il-il tutuşdurur (köçürmədən əvvəl/sonra yoxlama).
    Task<Result<MuqavileSayghacKocurmeDto>> VeziyyetAsync(CancellationToken ct = default);

    // Bir ilin bütün sayğaclarını köçürür. İdempotentdir — eyni dəyər varsa keçir.
    Task<Result<MuqavileSayghacKocurmeNeticeDto>> IlKocurAsync(int il, int? istifadeciId,
        CancellationToken ct = default);
}
