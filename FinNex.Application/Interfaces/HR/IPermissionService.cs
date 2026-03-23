using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Structure.Permission;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces
{
    public interface IPermissionService
        : IServiceAsync<Permission, PermissionDto, PermissionCreateDto, PermissionUpdateDto>
    {
        Task<Result<PermissionDto?>> GetByKodAsync(string kod);
    }
}