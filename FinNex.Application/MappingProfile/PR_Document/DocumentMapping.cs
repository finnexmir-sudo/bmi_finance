using AutoMapper;
using FinNex.Application.DTOs.PR_Document;
using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.Application.MappingProfile.PR_Document
{
    public class DocumentMapping : Profile
    {
        public DocumentMapping()
        {
            CreateMap<Sened, SenedListDto>()
                .ForMember(d => d.SobeAd, o => o.MapFrom(s => s.Sobe.Ad))
                .ForMember(d => d.SenedNovuAd, o => o.MapFrom(s => s.SenedNovu.Ad))
                .ForMember(d => d.StatusAd, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.StatusDeger, o => o.MapFrom(s => (int)s.Status));

            CreateMap<Sened, SenedDetailDto>()
                .ForMember(d => d.SobeAd, o => o.MapFrom(s => s.Sobe.Ad))
                .ForMember(d => d.SenedNovuAd, o => o.MapFrom(s => s.SenedNovu.Ad))
                .ForMember(d => d.StatusAd, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.StatusDeger, o => o.MapFrom(s => (int)s.Status));

            CreateMap<SenedFayl, SenedFaylDto>();
        }
    }
}
