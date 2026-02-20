using FinNex.Application.DTOs.Structur;
using FinNex.Application.DTOs.User;

namespace FinNex.Application.Interfaces.Structur
{
    public interface IUserDepartmentService
    {
        Task AssignAsync(UserDepartmentCreateDto dto);

        Task RemoveAsync(int id);

        Task<IList<UserDepartmentListDto>> GetUserDepartmentsAsync(int userId);

        Task SetPrimaryAsync(int userDepartmentId);
        Task<IList<UserListDto>> GetUsersByDepartmentAsync(int departmentId);
    }

}
