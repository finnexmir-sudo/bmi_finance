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
/// DİQQƏT: daxil_mektub.MEZMUN (LONG RAW — skan olunmuş məktub faylı) bu axına
/// DAXİL DEYİL və GƏTİRİLMƏYƏCƏK. 54 101 sətrin yalnız 264-ündə var, hamısı da
/// 2013-cü ildədir (12.08.2026 yoxlaması) — köhnə arxivdir, istifadəçi qərarı ilə
/// köçürülmür. LONG RAW oxumaq ayrıca ODP.NET konfiqurasiyası (InitialLONGFetchSize)
/// tələb edir; bu iş üçün onu etməyə dəyməz.
/// DaxilMektub.Mezmun (varbinary) sütunu sxemdə qalır, amma idxalda həmişə null olur —
/// FaylVar yoxlaması FaylYolu üzərindən işləyir (yeni yükləmələr DMS-ə gedir).
/// </summary>
public interface IMektubImportService
{
    // Oracle və FinNex saylarını il-il tutuşdurur (idxaldan əvvəl/sonra yoxlama üçün).
    Task<Result<MektubImportVeziyyetDto>> VeziyyetAsync(CancellationToken ct = default);

    // Bir ili idxal edir. jurnal: "xaric" | "daxil". il = null → Oracle-da ili boş olanlar.
    Task<Result<MektubImportNeticeDto>> IlIdxalAsync(string jurnal, int? il, CancellationToken ct = default);
}
