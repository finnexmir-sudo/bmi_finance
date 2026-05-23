using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.AyliqElave;

namespace FinNex.Application.Interfaces.HR
{
    /// <summary>
    /// İşçilərin aylıq Bonus / Overtime qeydlərini idarə edir.
    /// Maaşı artıq hesablanmış işçinin sətri kilidli olur.
    /// </summary>
    public interface IAyliqElaveService
    {
        /// <summary>Seçilmiş ay üçün bütün aktiv işçilərin sətirlərini qaytarır.</summary>
        Task<IList<AyliqElaveSetirDto>> GetSetirlerAsync(int il, int ay);

        /// <summary>Bir neçə sətrin Bonus/Overtime məbləğlərini upsert edir.
        /// Hər ikisi 0 olduqda mövcud qeyd soft-delete olunur.
        /// Kilidli işçilər atlanır. Data: yenilənmiş qeyd sayı.</summary>
        Task<Result<int>> SaxlaAsync(int il, int ay, IList<AyliqElaveSetirDto> setirler);

        /// <summary>Seçilmiş ay üçün IsciId → (Bonus, Overtime) cütlüklərini qaytarır.
        /// MaasController-in TopluHesabla səhifəsi pre-fill üçün istifadə edir.</summary>
        Task<(IDictionary<int, decimal> Bonus, IDictionary<int, decimal> Overtime)> GetAyMapAsync(int il, int ay);
    }
}
