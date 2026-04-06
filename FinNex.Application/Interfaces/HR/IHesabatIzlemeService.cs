using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.HesabatIzleme;

namespace FinNex.Application.Interfaces.HR
{
    public interface IHesabatIzlemeService
    {
        // Kateqoriyalar
        Task<Result<IList<HesabatKateqoriyaDto>>> GetKateqoriyalarAsync();
        Task<Result> KateqoriyaYaratAsync(string ad);
        Task<Result> KateqoriyaSilAsync(int id);

        // Şablonlar
        Task<Result<IList<HesabatSablonListDto>>> GetSablonlarAsync(int? departamentId = null);
        Task<Result> SablonYaratAsync(HesabatSablonCreateDto dto);
        Task<Result> SablonSilAsync(int id);

        // Tapşırıqlar
        Task<Result<HesabatIzlemeDashboardDto>> GetDashboardAsync(int? departamentId = null);
        Task<Result<IList<HesabatTapshiriqListDto>>> GetTapshiriqlarAsync(int? departamentId = null, int? status = null);
        Task<Result> TamamlaAsync(int tapshiriqId, int isciId, string? qeyd);
        Task<Result> IcrayaBaslaAsync(int tapshiriqId, int isciId);

        // Avtomatik
        Task<Result> DovrTapshiriqlariniYaratAsync();
        Task<Result> GecikenleriYoxlaAsync();
    }
}
