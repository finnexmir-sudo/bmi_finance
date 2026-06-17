using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.DigerTutulma;

namespace FinNex.Application.Interfaces.HR
{
    /// <summary>
    /// İşçidən digər tutulma (sığorta və s.) — maaşa təsir etmir, yalnız
    /// provodkada net ödənişdən aylıq pay çıxılır.
    /// </summary>
    public interface IDigerTutulmaService
    {
        Task<Result<int>> YaratAsync(DigerTutulmaYaratDto dto, int yaradanIsciId);
        Task<Result<IList<DigerTutulmaListDto>>> HamisiniGetirAsync();
        Task<Result> LegvEtAsync(int id);

        /// <summary>
        /// Provodka üçün: verilmiş il/ay-da hər işçinin aylıq tutulma cəmi
        /// (yalnız aktiv və müddət daxilində olanlar). isciId → aylıq məbləğ.
        /// </summary>
        Task<Dictionary<int, decimal>> GetAyTutulmaMapAsync(int il, int ay);
    }
}
