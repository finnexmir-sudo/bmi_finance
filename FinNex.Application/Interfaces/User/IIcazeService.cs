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
    }
}