using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Marif;
using FinNex.Domain.Entities.Marif;

namespace FinNex.Application.Interfaces.AI
{
    /// <summary>
    /// Verilmiş qayda mətnindən Daxili Maarifləndirmə üçün sual qaralaması
    /// hazırlayır (27.10.2026). Yalnız QARALAMA qaytarır — heç nə bazaya yazmır,
    /// HR nəticəni redaktə edib təsdiqlədikdən sonra <c>IMarifService</c> yazır.
    /// </summary>
    public interface IMarifAiService
    {
        /// <param name="yalnizTip">
        /// <c>null</c> (default) — AI-dən qarışıq (yarısı çoxseçimli, yarısı açıq) istənir.
        /// Dəyər veriləndə — AI-dən YALNIZ həmin tipdə suallar istənir (27.10.2026,
        /// istifadəçi tələbi: generasiya anında tip seçimi).
        /// </param>
        Task<Result<List<MarifSualDto>>> SuallarHazirlaAsync(string menbeMetn, int sualSayi, MarifSualTipi? yalnizTip = null);
    }
}
