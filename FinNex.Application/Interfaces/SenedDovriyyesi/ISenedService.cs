
using AutoMapper;
using FinNex.Application.Common.Paged;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.DTOs.SenedDovriyyesi.Fayl;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Application.DTOs.Structur;
using FinNex.Application.Interfaces.Structur;
using FinNex.Application.Services;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Interfaces.SenedDovriyyesi
{
    public interface ISenedService: IServiceAsync<Sened,SenedDetailDto,SenedCreateDto,SenedUpdateDto>
    {

            // --- Yaratma Əməliyyatları ---
            Task<Result<int>> CreateAsync(SenedCreateDto dto, SenedUploadDto sdto, int userId, string? ip);
        
        Task<Result<int>> CreateWithFileAsync(
            SenedCreateDto dto,
            Stream stream,
            string originalName,
            string contentType,
            long size,
            int userId,
            string? ip);

        // --- Yeniləmə Əməliyyatları ---
        // Claude tərəfindən yeni əlavə edilən metod:
        Task<Result> UpdateAsync(SenedUpdateDto dto, int userId, string? ip);

        Task<Result> UpdateStatusAsync(int senedId, SenedStatusu status, int userId, string? ip);

        Task<Result<int>> UploadNewVersionAsync(SenedFaylUploadDto dto, int userId, string? ip);

        // --- Oxuma/Listələmə Əməliyyatları ---
        Task<Result<PagedResult<SenedListDto>>> GetPagedAsync(
    PagedRequest req,
    List<int> icazeliSobeIdleri,
    int? sobeId,
    int? senedNovuId,
    SenedStatusu? status,
    string? search);

        Task<Result<PagedResult<SenedListDto>>> GetSilinmisPagedAsync(
            PagedRequest req,
            List<int> icazeliSobeIdleri,
            int? sobeId,
            int? senedNovuId,
            SenedStatusu? status,
            string? search);

        Task<Result<SenedDetailDto>> GetDetailAsync(
            int senedId,
            List<int> icazeliSobeIdleri,
            bool isAdmin);

        Task<Result<SenedDetailDto>> GetDetailSilinmisAsync(
            int senedId,
            List<int> icazeliSobeIdleri,
            bool isAdmin);

        Task<Result<SenedDetailDto>> silmeİCazeSorgusuAsync(
            int senedId,
            List<int> icazeliSobeIdleri,
            bool isAdmin);

        Task<Result<SenedDashboardDto>> GetDashboardAsync();

        Task<Result<SenedFayl>> GetFileEntityAsync(int faylId);

        // --- Silmə və Bərpa Əməliyyatları ---
        Task<Result> SoftDeleteAsync(int senedId, int userId, string? ip);

        Task<Result> RestoreAsync(int senedId, int userId, string? ip);
    }

}
