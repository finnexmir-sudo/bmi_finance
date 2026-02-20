using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;

public interface IMezuniyyetService : IServiceAsync<Mezuniyyet, MezuniyyetDto, MezuniyyetCreateDto, MezuniyyetUpdateDto>
{
    // Siyahılama üçün xüsusi ListDTO metodu
    Task<Result<IList<MezuniyyetListDto>>> GetListAsync();

    // Təsdiqləmə prosesləri
    Task<Result> SobeReisiTesdiqAsync(int id, bool status, string? qeyd);
    Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd);
    Task<Result> HrTesdiqAsync(int id, bool status, string? qeyd);
}
