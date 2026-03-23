using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Structure.Permission;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Structure
{
    public class PermissionService
        : ServiceAsync<Permission, PermissionDto, PermissionCreateDto, PermissionUpdateDto>,
          IPermissionService
    {
        public PermissionService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper) { }

        public async Task<Result<PermissionDto?>> GetByKodAsync(string kod)
        {
            try
            {
                var entity = await _unitOfWork.Repository<Permission>()
                    .GetirAsync(x => x.Kod == kod);

                if (entity == null)
                    return Result<PermissionDto?>.Fail("Permission tapılmadı.");

                return Result<PermissionDto?>.Ok(_mapper.Map<PermissionDto>(entity));
            }
            catch
            {
                return Result<PermissionDto?>.Fail("Permission gətirilərkən xəta baş verdi.");
            }
        }
    }
}