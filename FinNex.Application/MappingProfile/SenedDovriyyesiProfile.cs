using AutoMapper;
using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.DTOs.SenedDovriyyesi.Fayl;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Application.DTOs.SenedDovriyyesi.SenedNovu;
using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.Application.MappingProfile
{
    public class SenedDovriyyesiProfile : Profile
    {
        public SenedDovriyyesiProfile()
        {
            CreateMap<SenedFayl, SenedFaylDto>();

            CreateMap<Sened, SenedListDto>()
                .ForMember(d => d.Sobe, o => o.MapFrom(s => s.Department.Ad))
                .ForMember(d => d.SenedNovu, o => o.MapFrom(s => s.SenedNovu.Ad))
                .ForMember(d => d.FaylSayi, o => o.MapFrom(s => s.Fayllar.Count));

            CreateMap<Sened, SenedDetailDto>()
                .ForMember(d => d.Sobe, o => o.MapFrom(s => s.Department.Ad))
                .ForMember(d => d.SenedNovu, o => o.MapFrom(s => s.SenedNovu.Ad))
                .ForMember(d => d.Tags, o => o.MapFrom(s => s.SenedTagMaps.Select(x => x.Tag!.Ad)))
                .ForMember(d => d.Fayllar, o => o.MapFrom(s => s.Fayllar.OrderByDescending(f => f.VersiyaNo)));

            CreateMap<AuditLog, AuditLogDto>();
            CreateMap<SenedNovu, SenedNovuListDto>()
    .ForMember(dest => dest.DepartmentAd,
        opt => opt.MapFrom(src => src.Department.Ad));

            CreateMap<SenedNovu, SenedNovuDetailDto>()
                .ForMember(dest => dest.DepartmentAd,
                    opt => opt.MapFrom(src => src.Department.Ad));

            CreateMap<DTOs.SenedDovriyyesi.SenedNovu.SenedNovuCreateDto, SenedNovu>();
            CreateMap<SenedNovuUpdateDto, SenedNovu>();

            CreateMap<AuditLog, AuditLogDto>();
            CreateMap<SenedFayl, SenedFaylDto>();
            CreateMap<Sened, SenedDetailDto>();

        }
    }
}
