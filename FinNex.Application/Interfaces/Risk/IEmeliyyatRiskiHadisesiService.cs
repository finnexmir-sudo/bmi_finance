using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Risk;
using FinNex.Domain.Entities.Risk;

namespace FinNex.Application.Interfaces.Risk;

/// <summary>
/// MB Qərar 04/1, Əlavə 4 — əməliyyat riski hadisələri jurnalı. Təsdiq
/// zənciri yoxdur (30.09.2026 qərarı): Risk/AML işçisi yaratdığı an qeyd
/// rəsmidir; redaktə sərbəstdir, amma hər dəyişiklik tarixçəyə yazılır.
/// </summary>
public interface IEmeliyyatRiskiHadisesiService
{
    Task<IList<EmeliyyatRiskiHadisesiDto>> SiyahiAsync(
        BiznesSahesi? biznesSahesi = null,
        RiskKateqoriyasi1? riskKateqoriyasi1 = null,
        DateTime? basTarix = null,
        DateTime? sonTarix = null);

    Task<EmeliyyatRiskiHadisesiDto?> DetalAsync(int id);

    Task<IList<EmeliyyatRiskiHadisesiTarixceDto>> TarixceAsync(int hadiseId);

    Task<Result<int>> YaratAsync(EmeliyyatRiskiHadisesiCreateDto dto, int? icraciId);

    Task<Result> YenileAsync(EmeliyyatRiskiHadisesiUpdateDto dto, int? icraciId);
}
