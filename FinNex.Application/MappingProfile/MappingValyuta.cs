using AutoMapper;
using FinNex.Application.DTOs;
using FinNex.Domain.Entities;

namespace FinNex.Application.MappingProfile
{
    public class MappingValyuta : Profile
    {
        public MappingValyuta()
        {
            CreateMap<Valyuta, ValyutaListDto>();
        }
    }
}
