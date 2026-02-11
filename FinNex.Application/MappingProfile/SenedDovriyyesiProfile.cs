using AutoMapper;
using FinNex.Application.DTOs.SenedDovriyyesi.Fayl;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.Application.MappingProfile
{
    public class SenedDovriyyesiProfile : Profile
    {
        public SenedDovriyyesiProfile()
        {
            CreateMap<SenedFayl, SenedFaylDto>();

            CreateMap<Sened, SenedListDto>()
                .ForMember(d => d.Sobe, o => o.MapFrom(s => s.Sobe.Ad))
                .ForMember(d => d.SenedNovu, o => o.MapFrom(s => s.SenedNovu.Ad))
                .ForMember(d => d.FaylSayi, o => o.MapFrom(s => s.Fayllar.Count));

            CreateMap<Sened, SenedDetailDto>()
                .ForMember(d => d.Tags, o => o.MapFrom(s => s.SenedTagMaps.Select(x => x.Tag!.Ad)))
                .ForMember(d => d.Fayllar, o => o.MapFrom(s => s.Fayllar.OrderByDescending(f => f.VersiyaNo)));
        }
    }
}
