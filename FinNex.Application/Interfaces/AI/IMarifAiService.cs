using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Marif;

namespace FinNex.Application.Interfaces.AI
{
    /// <summary>
    /// Verilmiş qayda mətnindən Daxili Maarifləndirmə üçün sual qaralaması
    /// hazırlayır (27.10.2026). Yalnız QARALAMA qaytarır — heç nə bazaya yazmır,
    /// HR nəticəni redaktə edib təsdiqlədikdən sonra <c>IMarifService</c> yazır.
    /// </summary>
    public interface IMarifAiService
    {
        Task<Result<List<MarifSualDto>>> SuallarHazirlaAsync(string menbeMetn, int sualSayi);
    }
}
