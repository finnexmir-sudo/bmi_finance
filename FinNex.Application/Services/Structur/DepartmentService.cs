using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Structur;
using FinNex.Application.DTOs.User;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Structur
{
    public class DepartmentService
    : ServiceAsync<Departament, DepartmentListDto, DepartmentCreateDto, DepartmentUpdateDto>,
      IDepartmentService
    {
        public DepartmentService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper)
        {
        }
        public async Task AssignAsync(UserDepartmentCreateDto dto)
        {
            var exists = await _unitOfWork
                .Repository<UserDepartment>()
                .AnyAsync(x =>
                    x.UserId == dto.UserId &&
                    x.DepartmentId == dto.DepartmentId);

            if (exists)
                throw new Exception("User already assigned to this department.");

            var entity = _mapper.Map<UserDepartment>(dto);

            await _unitOfWork.Repository<UserDepartment>()
                .YaratAsync(entity);

            await _unitOfWork.YaddaSaxlaAsync();
        }

        public async Task<Result<List<DepartmentListDto>>> GetAllWithEmployeeCountAsync()
        {
            var data = await _unitOfWork
     .Repository<Departament>()
     .Query()
     .Where(d => !d.Silinib) // silinmişləri göstərmə
     .Select(d => new DepartmentListDto
     {
         Id = d.Id,
         Ad = d.Ad,
         Aciqlama = d.Aciqlama,
         IsciSayi = d.UserDepartments.Count()
     })
     .ToListAsync();


            return Result<List<DepartmentListDto>>.Ok(data);
        }
    }

}


