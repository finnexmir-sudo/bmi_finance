using FinNex.Application.DTOs.SenedDovriyyesi;

namespace FinNex.Application.Interfaces.SenedDovriyyesi
{
    public interface IAuditLogService
    {
        Task WriteAsync(int userId, string action, int? senedId, string? ip, object? details = null);
        Task<List<AuditLogDto>> GetBySenedIdAsync(int senedId);
    }

}
