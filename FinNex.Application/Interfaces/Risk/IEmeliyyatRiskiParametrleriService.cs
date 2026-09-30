using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Risk;

namespace FinNex.Application.Interfaces.Risk;

/// <summary>Bankın öz "tədbir planı" zərər həddi (MB Qərar 04/1, Əlavə 4 qeydi) —
/// TƏK sətirli sazlama, HR-in MezuniyyetHuquqParametrleri ilə eyni naxış.</summary>
public interface IEmeliyyatRiskiParametrleriService
{
    Task<EmeliyyatRiskiParametrleriDto> AlAsync();
    Task<Result> SaxlaAsync(EmeliyyatRiskiParametrleriDto dto, int? icraciId);
}
