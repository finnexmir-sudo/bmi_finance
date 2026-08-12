using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Hevale;

namespace FinNex.Application.Interfaces.Hevale;

/// <summary>
/// BMI (Oracle) həvalə jurnallarının FinNex bazasına birdəfəlik köçürülməsi.
///
/// Oracle tərəfi YALNIZ OXUNUR (IOracleService → SELECT). Yazı yalnız SQL Server-ədir.
/// Hər il ayrıca idxal olunur ki, proses izlənilən və dayandırıla bilən olsun;
/// idxal AÇARA GÖRƏ İDEMPOTENTDİR (HEV_NOM, il daxilində) — yarıda dayansa təkrar
/// işlədəndə yalnız çatışmayan sətirlər gəlir, dublikat yaranmır.
///
/// MƏKTUBDAN FƏRQ: həvalə cədvəllərində `IL` sütunu YOXDUR. İl `TARIX`-dən çıxarılır
/// (Oracle tərəfdə EXTRACT(YEAR FROM tarix), FinNex tərəfdə Tarix.Year). Tarixi boş
/// olan sətirlər "ilsiz" qrupundadır və ayrıca köçürülür.
/// </summary>
public interface IHevaleImportService
{
    // Oracle və FinNex saylarını il-il tutuşdurur (idxaldan əvvəl/sonra yoxlama üçün).
    Task<Result<HevaleImportVeziyyetDto>> VeziyyetAsync(CancellationToken ct = default);

    // Bir ili idxal edir. jurnal: "geden" | "gelen". il = null → Oracle-da tarixi boş olanlar.
    Task<Result<HevaleImportNeticeDto>> IlIdxalAsync(string jurnal, int? il, CancellationToken ct = default);
}
