using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Structure.UserPermission;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Structure
{
    public class UserPermissionService
        : ServiceAsync<UserPermission, UserPermissionDto, UserPermissionCreateDto, UserPermissionUpdateDto>,
          IUserPermissionService
    {
        public UserPermissionService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper) { }

        public async Task<Result<IList<UserPermissionDto>>> GetByUserIdAsync(int userId)
        {
            try
            {
                var list = await _unitOfWork.Repository<UserPermission>()
                    .HamisiniGetirAsync(
                        x => x.UserId == userId,
                        q => q.Include(x => x.User)
                              .Include(x => x.Permission),
                        true);

                return Result<IList<UserPermissionDto>>.Ok(_mapper.Map<IList<UserPermissionDto>>(list));
            }
            catch
            {
                return Result<IList<UserPermissionDto>>.Fail("İstifadəçi icazələri gətirilərkən xəta baş verdi.");
            }
        }

        public async Task<Result<bool>> HasPermissionAsync(int userId, string permissionKod)
        {
            try
            {
                var exists = await _unitOfWork.Repository<UserPermission>()
                    .MovcuddurmuAsync(x => x.UserId == userId &&
                                           x.Permission.Kod == permissionKod &&
                                           x.Allowed);

                return Result<bool>.Ok(exists);
            }
            catch
            {
                return Result<bool>.Fail("Permission yoxlanarkən xəta baş verdi.");
            }
        }
    }
}