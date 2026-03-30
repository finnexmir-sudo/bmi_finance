using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;

namespace FinNex.Application.Interfaces.Communication
{
    public interface IEvezediciTesdiqService
    {
        Task<Result<IList<EvezediciTesdiqDto>>> GetGozleyenlerAsync(int evezediciIsciId);
        Task<Result> QebulEtAsync(int tesdiqId, int isciId);
        Task<Result> ReddEtAsync(int tesdiqId, int isciId, string? qeyd);
        Task<Result> YaratAsync(int mezuniyyetId, int evezediciIsciId);
        Task<Result<EvezediciTesdiqDto?>> GetByMezuniyyetAsync(int mezuniyyetId);
    }
}
