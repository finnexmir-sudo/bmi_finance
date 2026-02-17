using AutoMapper;
using FinNex.Application.DTOs.Structur;
using FinNex.Application.DTOs.User;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Structur
{
    public class UserDepartmentService : IUserDepartmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserDepartmentService(IUnitOfWork unitOfWork,
                                     IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task AssignAsync(UserDepartmentCreateDto dto)
        {
            var entity = _mapper.Map<UserDepartment>(dto);

            await _unitOfWork.Repository<UserDepartment>().AddAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();
        }

        public async Task RemoveAsync(int id)
        {
            await _unitOfWork.Repository<UserDepartment>().SoftDeleteAsync(id);
            await _unitOfWork.YaddaSaxlaAsync();
        }

        public async Task<IList<UserDepartmentListDto>> GetUserDepartmentsAsync(int userId)
        {
            var list = await _unitOfWork.Repository<UserDepartment>()
                .GetAllAsync(x => x.UserId == userId,
                    include: q => q.Include(x => x.Department),
                    izlemeden: true);

            return _mapper.Map<IList<UserDepartmentListDto>>(list);
        }

        public async Task SetPrimaryAsync(int userDepartmentId)
        {
            var repo = _unitOfWork.Repository<UserDepartment>();

            var entity = await repo.GetByIdAsync(userDepartmentId);
            if (entity == null)
                throw new Exception("Assignment not found");

            // 1️⃣ əvvəl həmin user-in bütün primary-lərini false et
            var allUserDeps = await repo.GetAllAsync(x => x.UserId == entity.UserId);

            foreach (var item in allUserDeps)
                item.Esasdir = false;

            // 2️⃣ seçiləni primary et
            entity.Esasdir = true;

            await _unitOfWork.YaddaSaxlaAsync();
        }
        public async Task<IList<UserListDto>> GetUsersByDepartmentAsync(int departmentId)
        {
            var userDepartments = await _unitOfWork
                .Repository<UserDepartment>()
                .GetAllAsync(
                    x => x.DepartmentId == departmentId,
                    include: q => q.Include(x => x.User),
                    izlemeden: true);

            var users = userDepartments.Select(x => new UserListDto
            {
                Id = x.User.Id,
                UserName = x.User.UserName,
                FullName = x.User.Ad + " " + x.User.Soyad
            }).ToList();

            return users;
        }


    }

}
