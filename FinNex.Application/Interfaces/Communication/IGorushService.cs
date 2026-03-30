// IGorushService.cs
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;
using FinNex.Domain.Entities.Communication;

namespace FinNex.Application.Interfaces.Communication
{
    public interface IGorushService
    {
        Task<Result<IList<GorushListDto>>> GetMenimGorushlerimAsync(int isciId);
        Task<Result<IList<GorushListDto>>> GetTeshkilEtdiklerimAsync(int isciId);
        Task<Result<GorushDetailDto>> GetDetayAsync(int id, int isciId);
        Task<Result> YaratAsync(GorushCreateDto dto);
        Task<Result> StatusYenileAsync(int id, GorushStatus status, string? qeydler, int isciId);
        Task<Result> IshtirakciCavabAsync(int gorushId, int isciId, IshtirakciStatus status, string? qeyd);
        Task<Result> QeydEleveEtAsync(int gorushId, int isciId, string qeydler);
        Task<Result> SilAsync(int id, int isciId);
        Task<Result> EditAsync(GorushEditDto dto);

        Task<Result<IList<GorushDetailDto>>> GetYaxinlasanlarAsync(DateTime tarix);
    }
}