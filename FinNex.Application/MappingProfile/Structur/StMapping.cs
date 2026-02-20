using AutoMapper;
using FinNex.Application.DTOs.Structur;
using FinNex.Domain.Entities.Structure;

namespace FinNex.Application.MappingProfile.Structur
{
   
    public class StMapping : Profile
    {
        public StMapping()
        {
            CreateMap<UserDepartment, UserDepartmentDto>()
     .ReverseMap();

            CreateMap<UserDepartmentCreateDto, UserDepartment>();

            CreateMap<UserDepartment, UserDepartmentListDto>()
    .ForMember(dest => dest.DepartmentName,
        opt => opt.MapFrom(src => src.Department.Ad));

            CreateMap<Department, DepartmentListDto>();
            CreateMap<DepartmentCreateDto, Department>();
            CreateMap<DepartmentUpdateDto, Department>().ReverseMap();
            CreateMap<Department, DepartmentDetailDto>()
                .ForMember(dest => dest.UserCount,
                    opt => opt.MapFrom(src => src.UserDepartments.Count));

        }
    }
}
