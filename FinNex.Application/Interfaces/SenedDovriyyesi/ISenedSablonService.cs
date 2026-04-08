using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.SenedDovriyyesi.SenedSablon;

namespace FinNex.Application.Interfaces.SenedDovriyyesi
{
    public interface ISenedSablonService
    {
        Task<Result<List<SenedSablonListDto>>> GetListAsync(int? senedNovuId = null);
        Task<Result<int>> CreateAsync(SenedSablonCreateDto dto, Stream faylStream, string faylAdi, string contentType, int userId);
        Task<Result> DeleteAsync(int id, int userId);
        Task<Result<(string faylYolu, string faylAdi)>> GetFaylAsync(int id);
    }
}
