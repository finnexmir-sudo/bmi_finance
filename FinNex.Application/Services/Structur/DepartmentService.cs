using AutoMapper;
using FinNex.Application.DTOs.Structur;
using FinNex.Application.DTOs.User;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Structur
{
    public class DepartmentService
    : ServiceAsync<Department, DepartmentListDto, DepartmentCreateDto, DepartmentUpdateDto>,
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
       
    }

}


