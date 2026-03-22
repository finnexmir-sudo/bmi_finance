using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces
{
    public interface IMezuniyyetService
        : IServiceAsync<Mezuniyyet, MezuniyyetDto, MezuniyyetCreateDto, MezuniyyetUpdateDto>
    {
        // Mövcud metodlar
        Task<Result<IList<MezuniyyetListDto>>> GetListAsync();
        Task<Result> SobeReisiTesdiqAsync(int id, bool status, string? qeyd);
        Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd);
        Task<Result> HrTesdiqAsync(int id, bool status, string? qeyd);

        // YENİ: İşçi paneli üçün əlavə edildi
        Task<Result<IList<MezuniyyetListDto>>> GetIsciMezuniyyetleriAsync(int isciId);
        Task<Result> LegvEtAsync(int id, int isciId);
    }
}