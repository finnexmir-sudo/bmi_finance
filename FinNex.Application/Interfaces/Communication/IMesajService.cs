using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;

namespace FinNex.Application.Interfaces.Communication
{
    public interface IMesajService
    {
        Task<Result<IList<MesajListDto>>> GetGelenlerAsync(int isciId);
        Task<Result<IList<MesajListDto>>> GetGonderilenlerAsync(int isciId);
        Task<Result<MesajDetailDto>> GetDetayAsync(int mesajId, int isciId);
        Task<Result> GonderAsync(MesajCreateDto dto);
        Task<Result> OxunduIsareEtAsync(int mesajId, int isciId);
        Task<Result<int>> OxunmamisSayiAsync(int isciId);
    }
}
