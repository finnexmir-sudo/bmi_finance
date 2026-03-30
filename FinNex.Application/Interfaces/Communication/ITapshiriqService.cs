// ITapshiriqService.cs
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;

namespace FinNex.Application.Interfaces.Communication
{
    public interface ITapshiriqService
    {
        Task<Result<IList<TapshiriqListDto>>> GetMenimTapshiriqlarimAsync(int isciId);
        Task<Result<IList<TapshiriqListDto>>> GetVerdiklerimAsync(int isciId);
        Task<Result<IList<TapshiriqListDto>>> GetKomandaTapshiriqlariAsync(int isciId);
        Task<Result<TapshiriqDetailDto>> GetDetayAsync(int id, int isciId);
        Task<Result> YaratAsync(TapshiriqCreateDto dto);
        Task<Result> StatusYenileAsync(TapshiriqUpdateDto dto, int isciId);
        Task<Result> SherhEleveEtAsync(int tapshiriqId, int isciId, string metn);
        Task<Result> SilAsync(int id, int isciId);
        Task<Result<IList<TapshiriqListDto>>> GetGecikenleriAsync();
        Task<Result> EditAsync(TapshiriqEditDto dto);

        Task<Result<IList<TapshiriqListDto>>> GetYaxinlasanlarAsync(DateTime tarix);
    }
}