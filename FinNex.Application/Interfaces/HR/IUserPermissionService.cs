using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Structure.UserPermission;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces
{
    public interface IUserPermissionService
        : IServiceAsync<UserPermission, UserPermissionDto, UserPermissionCreateDto, UserPermissionUpdateDto>
    {
        Task<Result<IList<UserPermissionDto>>> GetByUserIdAsync(int userId);
        Task<Result<bool>> HasPermissionAsync(int userId, string permissionKod);
    }
}