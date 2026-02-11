
using FinNex.Application.DTOs.SenedDovriyyesi;

namespace FinNex.Application.Interfaces.SenedDovriyyesi
{
    public interface ISenedService
    {
        Task<int> UploadAsync(SenedUploadDto dto, string userId, string? ip);
    }

}
