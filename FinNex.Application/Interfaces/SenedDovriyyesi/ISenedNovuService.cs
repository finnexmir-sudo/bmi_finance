using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.SenedDovriyyesi.SenedNovu;
using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.Application.Interfaces.SenedDovriyyesi
{
    public interface ISenedNovuService
        : IServiceAsync<
            SenedNovu,
            SenedNovuListDto,
            SenedNovuCreateDto,
            SenedNovuUpdateDto>
    {
        // xüsusi metodlar

        Task<Result<List<SenedNovuListDto>>> GetByDepartmentAsync(int departmentId);
        Task<Result<int>> CreateAsync(SenedNovuCreateDto dto, int userId);
        Task<Result> SoftDeleteAsync(int id, int userId);
        Task<Result> ToggleAktivAsync(int id, int userId);
    }
}
