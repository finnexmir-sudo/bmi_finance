using FinNex.Application.DTOs.Structur;
using FinNex.Application.DTOs.User;
using FinNex.Domain.Entities.Structure;

namespace FinNex.Application.Interfaces.Structur
{
    public interface IDepartmentService :
        IServiceAsync<Department, DepartmentListDto, DepartmentCreateDto, DepartmentUpdateDto>
    {
        


    }

}
