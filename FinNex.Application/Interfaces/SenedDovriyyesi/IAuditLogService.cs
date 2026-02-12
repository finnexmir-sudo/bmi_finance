using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;

namespace FinNex.Application.Interfaces.SenedDovriyyesi
{
    public interface IAuditLogService
    {
        Task WriteAsync(int userId, string action, int? senedId, string? ip, object? details = null);
        Task<List<AuditLogDto>> GetBySenedIdAsync(int senedId);
    }

    public interface ISenedNovuService
    {
        Task<Result<int>> CreateAsync(SenedNovuCreateDto dto);
    }

}
