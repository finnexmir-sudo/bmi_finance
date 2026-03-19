using AutoMapper;
using FinNex.Domain.Entities.HR;
using FinNex.Application.DTOs.HR;
using FinNex.Application.DTOs.HR.Davamiyyet;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.DTOs.HR.Vezife;

public class HRProfile : Profile
{
    public HRProfile()
    {
        // =========================
        // VEZIFE
        // =========================

        CreateMap<Vezife, VezifeListDto>()
    .ForMember(dest => dest.DepartamentAd, opt => opt.MapFrom(src => src.Departament.Ad));
        CreateMap<Vezife, VezifeDetailDto>();

        CreateMap<VezifeCreateDto, Vezife>();
        CreateMap<VezifeUpdateDto, Vezife>();


        // =========================
        // ISCI
        // =========================

        CreateMap<Isci, IsciListDto>()
    .ForMember(dest => dest.TamAd, opt => opt.MapFrom(src => $"{src.Ad} {src.Soyad} {src.AtaAdi}"))
    .ForMember(dest => dest.CariMaas, opt => opt.MapFrom(src => src.Maliye.CariMaas))
    .ForMember(dest => dest.SobeAdi, opt => opt.MapFrom(src => src.Departament.Ad)) // Səndə Department.Name ola bilər
    .ForMember(dest => dest.VezifeAdi, opt => opt.MapFrom(src => src.Vezife.Ad));

        CreateMap<Isci, IsciDetailDto>()
            .ForMember(d => d.Sobe,
                o => o.MapFrom(s => s.Departament.Ad))
            .ForMember(d => d.Vezife,
                o => o.MapFrom(s => s.Vezife.Ad))
            .ForMember(d => d.LoginVar,
                o => o.MapFrom(s => s.AppUserId != null));

        CreateMap<IsciCreateDto, Isci>();
    
        CreateMap<IsciUpdateDto, Isci>();


        // =========================
        // MAAS
        // =========================

        CreateMap<Maas, MaasListDto>()
            .ForMember(d => d.IsciTamAd,
                o => o.MapFrom(s => s.Isci.Ad + " " + s.Isci.Soyad));

        CreateMap<Maas, MaasDetailDto>()
            .ForMember(d => d.IsciTamAd,
                o => o.MapFrom(s => s.Isci.Ad + " " + s.Isci.Soyad))
            .ForMember(d => d.Sobe,
                o => o.MapFrom(s => s.Isci.Departament.Ad))
            .ForMember(d => d.Vezife,
                o => o.MapFrom(s => s.Isci.Vezife.Ad));

        CreateMap<MaasCreateDto, Maas>();
        CreateMap<MaasUpdateDto, Maas>();


        // =========================
        // DAVAMIYYET
        // =========================

        CreateMap<Davamiyyet, DavamiyyetListDto>()
            .ForMember(d => d.IsciTamAd,
                o => o.MapFrom(s => s.Isci.Ad + " " + s.Isci.Soyad));

        CreateMap<Davamiyyet, DavamiyyetDetailDto>()
            .ForMember(d => d.IsciTamAd,
                o => o.MapFrom(s => s.Isci.Ad + " " + s.Isci.Soyad))
            .ForMember(d => d.Sobe,
                o => o.MapFrom(s => s.Isci.Departament.Ad))
            .ForMember(d => d.Vezife,
                o => o.MapFrom(s => s.Isci.Vezife.Ad));

        CreateMap<DavamiyyetCreateDto, Davamiyyet>();
        CreateMap<DavamiyyetUpdateDto, Davamiyyet>();


        // =========================
        // MEZUNIYYET
        // =========================

        CreateMap<Mezuniyyet, MezuniyyetDto>()
    .ForMember(dest => dest.IsciAdSoyad, opt => opt.MapFrom(src => src.Isci.Ad + " " + src.Isci.Soyad))
    .ForMember(dest => dest.EvezEdenIsciAdSoyad, opt => opt.MapFrom(src => src.EvezEdenIsci != null ? src.EvezEdenIsci.Ad + " " + src.EvezEdenIsci.Soyad : ""));

        CreateMap<MezuniyyetCreateDto, Mezuniyyet>();
        CreateMap<MezuniyyetUpdateDto, Mezuniyyet>();
        CreateMap<Mezuniyyet, MezuniyyetListDto>()
    .ForMember(dest => dest.SobeAdi, opt => opt.MapFrom(src => src.Isci.Departament.Ad))
    .ForMember(dest => dest.VezifeAdi, opt => opt.MapFrom(src => src.Isci.Vezife.Ad));
    }
}
