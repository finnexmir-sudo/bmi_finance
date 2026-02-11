
using FinNex.Application.Common.Paged;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.DTOs.SenedDovriyyesi.Fayl;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.Application.Interfaces.SenedDovriyyesi
{
    public interface ISenedService
    {
        Task<Result<int>> CreateAsync(SenedCreateDto dto, int userId, string? ip);
        Task<Result> UpdateStatusAsync(int senedId, SenedStatusu status, int userId, string? ip);

        Task<Result<PagedResult<SenedListDto>>> GetPagedAsync(
            PagedRequest req, int? sobeId, int? senedNovuId, SenedStatusu? status, string? search);

        Task<Result<PagedResult<SenedListDto>>> GetDeletedPagedAsync(PagedRequest req);

        Task<Result<SenedDetailDto>> GetDetailAsync(int senedId);

        Task<Result<SenedDashboardDto>> GetDashboardAsync();

        Task<Result<int>> UploadNewVersionAsync(SenedFaylUploadDto dto, int userId, string? ip);

        Task<Result> SoftDeleteAsync(int senedId, int userId, string? ip);
        Task<Result> RestoreAsync(int senedId, int userId, string? ip);

        Task<Result<SenedFayl>> GetFileEntityAsync(int faylId);
        Task<Result<int>> CreateWithFileAsync(
    SenedCreateDto dto,
    Stream stream,
    string originalName,
    string contentType,
    long size,
    int userId,
    string? ip);

    }
    public interface IFileStorageService
    {
        Task<(string storedName, string path, string sha256)> SaveAsync(
            Stream stream, string originalName, string contentType);

        Task<Stream> OpenReadAsync(string path);
    }
    public interface IAuditLogService
    {
        Task WriteAsync(int userId, string action, int? senedId, string? ip, object? details = null);
        Task<List<AuditLogDto>> GetBySenedIdAsync(int senedId);
    }

}
