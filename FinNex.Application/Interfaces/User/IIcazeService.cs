using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Icaze;

namespace FinNex.Application.Interfaces
{
    public interface IIcazeService
    {
        Task<Result<IList<IcazeListDto>>> GetIsciIcazeleriAsync(int isciId);
        Task<Result<IcazeListDto>> YaratAsync(IcazeCreateDto dto);
        Task<Result> LegvEtAsync(int icazeId, int isciId);
        Task<Result<IcazeDetailDto>> GetDetayAsync(int icazeId);

        // Təsdiq paneli üçün
        Task<Result<IList<IcazeListDto>>> GetAllAsync();
        Task<Result<IList<IcazeListDto>>> GetGozlemededeAsync();
        Task<Result<IList<IcazeListDto>>> GetRehberTesdiqindeAsync();
        Task<Result<IList<IcazeListDto>>> GetHrTesdiqindeAsync();
        Task<Result> SobeReisiTesdiqAsync(int id, bool status, string? qeyd);
        Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd);
        Task<Result> HrTesdiqAsync(int id, bool status, string? qeyd);
    }
}