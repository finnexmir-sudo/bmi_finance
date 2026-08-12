using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Mektub;

namespace FinNex.Application.Interfaces.Mektub;

/// <summary>
/// BMI (Oracle) məktub jurnallarının FinNex bazasına birdəfəlik köçürülməsi.
///
/// Oracle tərəfi YALNIZ OXUNUR (IOracleService → SELECT). Yazı yalnız SQL Server-ədir.
/// Hər il ayrıca idxal olunur ki, proses izlənilən və dayandırıla bilən olsun;
/// idxal AÇARA GÖRƏ İDEMPOTENTDİR (xaric → KOD, daxil → NOM), yəni yarıda dayansa
/// təkrar işlədəndə yalnız çatışmayan sətirlər gəlir, dublikat yaranmır.
///
/// DİQQƏT: daxil_mektub.MEZMUN (LONG RAW) bu axına DAXİL DEYİL — 54 101 sətrin
/// yalnız 264-ündə var və LONG oxumaq ayrıca konfiqurasiya tələb edir; ayrı keçidlə
/// gətiriləcək.
/// </summary>
public interface IMektubImportService
{
    // Oracle və FinNex saylarını il-il tutuşdurur (idxaldan əvvəl/sonra yoxlama üçün).
    Task<Result<MektubImportVeziyyetDto>> VeziyyetAsync(CancellationToken ct = default);

    // Bir ili idxal edir. jurnal: "xaric" | "daxil". il = null → Oracle-da ili boş olanlar.
    Task<Result<MektubImportNeticeDto>> IlIdxalAsync(string jurnal, int? il, CancellationToken ct = default);
}
