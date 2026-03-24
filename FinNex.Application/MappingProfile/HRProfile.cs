using AutoMapper;
using FinNex.Application.DTOs.HR;
using FinNex.Application.DTOs.HR.Davamiyyet;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.DTOs.HR.IsciStrukturRolu;
using FinNex.Application.DTOs.HR.IsciTeyinat;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.DTOs.HR.Vezife;
using FinNex.Application.DTOs.Structure.Permission;
using FinNex.Application.DTOs.Structure.UserPermission;
using FinNex.Domain.Entities.HR;
using System.Linq;

public class HRProfile : Profile
{
    public HRProfile()
    {
        // =========================
        // VEZIFE
        // =========================

        CreateMap<Vezife, VezifeListDto>()
            .ForMember(dest => dest.DepartamentAd,
                opt => opt.MapFrom(src => src.Departament.Ad));

        CreateMap<Vezife, VezifeDetailDto>();

        CreateMap<VezifeCreateDto, Vezife>();
        CreateMap<VezifeUpdateDto, Vezife>();


        // =========================
        // ISCI
        // =========================

        CreateMap<IsciCreateDto, Isci>()
            .ForMember(dest => dest.AppUserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.Maliye, opt => opt.Ignore())
            .ForMember(dest => dest.MezuniyyetBalans, opt => opt.Ignore())
            .ForMember(dest => dest.Maaslar, opt => opt.Ignore())
            .ForMember(dest => dest.MaasTarixcesi, opt => opt.Ignore())
            .ForMember(dest => dest.IsciTeyinatlari, opt => opt.Ignore());

        CreateMap<IsciUpdateDto, Isci>()
            .ForMember(dest => dest.AppUserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.Maliye, opt => opt.Ignore())
            .ForMember(dest => dest.MezuniyyetBalans, opt => opt.Ignore())
            .ForMember(dest => dest.Maaslar, opt => opt.Ignore())
            .ForMember(dest => dest.MaasTarixcesi, opt => opt.Ignore())
            .ForMember(dest => dest.IsciTeyinatlari, opt => opt.Ignore());

        CreateMap<Isci, IsciListDto>()
            .ForMember(dest => dest.TamAd,
                opt => opt.MapFrom(src => $"{src.Ad} {src.Soyad} {src.AtaAdi}".Trim()))
            .ForMember(dest => dest.CariMaas,
                opt => opt.MapFrom(src => src.Maliye != null ? src.Maliye.CariMaas : 0))
            .ForMember(dest => dest.SobeAdi,
                opt => opt.MapFrom(src =>
                    src.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault()))
            .ForMember(dest => dest.VezifeAdi,
                opt => opt.MapFrom(src =>
                    src.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault()));

        CreateMap<Isci, IsciDetailDto>()
            .ForMember(dest => dest.LoginVar,
                opt => opt.MapFrom(src => src.AppUserId != null))
            .ForMember(dest => dest.CariMaas,
                opt => opt.MapFrom(src => src.Maliye != null ? src.Maliye.CariMaas : 0))
            .ForMember(dest => dest.BankHesabNo,
                opt => opt.MapFrom(src => src.Maliye != null ? src.Maliye.BankHesabNo : null))
            .ForMember(dest => dest.SosialSigortaNo,
                opt => opt.MapFrom(src => src.Maliye != null ? src.Maliye.SosialSigortaNo : null))
            .ForMember(dest => dest.SobeAdi,
                opt => opt.MapFrom(src =>
                    src.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault()))
            .ForMember(dest => dest.VezifeAdi,
                opt => opt.MapFrom(src =>
                    src.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault()))
            .ForMember(dest => dest.AktivDepartamentId,
                opt => opt.MapFrom(src =>
                    src.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => (int?)t.DepartamentId)
                        .FirstOrDefault()))
            .ForMember(dest => dest.AktivVezifeId,
                opt => opt.MapFrom(src =>
                    src.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => (int?)t.VezifeId)
                        .FirstOrDefault()));


        // =========================
        // MAAS
        // =========================

        CreateMap<Maas, MaasListDto>()
            .ForMember(dest => dest.IsciTamAd,
                opt => opt.MapFrom(src => src.Isci.Ad + " " + src.Isci.Soyad));

        CreateMap<Maas, MaasDetailDto>()
            .ForMember(dest => dest.IsciTamAd,
                opt => opt.MapFrom(src => src.Isci.Ad + " " + src.Isci.Soyad))
            .ForMember(dest => dest.Sobe,
                opt => opt.MapFrom(src =>
                    src.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault()))
            .ForMember(dest => dest.Vezife,
                opt => opt.MapFrom(src =>
                    src.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault()));

        CreateMap<MaasCreateDto, Maas>();
        CreateMap<MaasUpdateDto, Maas>();


        // =========================
        // DAVAMIYYET
        // =========================

        CreateMap<Davamiyyet, DavamiyyetListDto>()
            .ForMember(dest => dest.IsciTamAd,
                opt => opt.MapFrom(src => src.Isci.Ad + " " + src.Isci.Soyad));

        CreateMap<Davamiyyet, DavamiyyetDetailDto>()
            .ForMember(dest => dest.IsciTamAd,
                opt => opt.MapFrom(src => src.Isci.Ad + " " + src.Isci.Soyad))
            .ForMember(dest => dest.Sobe,
                opt => opt.MapFrom(src =>
                    src.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault()))
            .ForMember(dest => dest.Vezife,
                opt => opt.MapFrom(src =>
                    src.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault()));

        CreateMap<DavamiyyetCreateDto, Davamiyyet>();
        CreateMap<DavamiyyetUpdateDto, Davamiyyet>();


        // =========================
        // MEZUNIYYET
        // =========================

        CreateMap<Mezuniyyet, MezuniyyetDto>()
            .ForMember(dest => dest.IsciAdSoyad,
                opt => opt.MapFrom(src => src.Isci.Ad + " " + src.Isci.Soyad))
            .ForMember(dest => dest.EvezEdenIsciAdSoyad,
                opt => opt.MapFrom(src => src.EvezEdenIsci != null
                    ? src.EvezEdenIsci.Ad + " " + src.EvezEdenIsci.Soyad
                    : ""));

        CreateMap<MezuniyyetCreateDto, Mezuniyyet>();
        CreateMap<MezuniyyetUpdateDto, Mezuniyyet>();

        CreateMap<Mezuniyyet, MezuniyyetListDto>()
            .ForMember(dest => dest.SobeAdi,
                opt => opt.MapFrom(src =>
                    src.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Departament.Ad)
                        .FirstOrDefault()))
            .ForMember(dest => dest.VezifeAdi,
                opt => opt.MapFrom(src =>
                    src.Isci.IsciTeyinatlari
                        .Where(t => t.Aktivdir)
                        .Select(t => t.Vezife.Ad)
                        .FirstOrDefault()));

        CreateMap<IsciTeyinat, IsciTeyinatDto>()
    .ForMember(d => d.IsciTamAd, o => o.MapFrom(s => s.Isci.TamAd))
    .ForMember(d => d.DepartamentAd, o => o.MapFrom(s => s.Departament.Ad))
    .ForMember(d => d.VezifeAd, o => o.MapFrom(s => s.Vezife.Ad));
        CreateMap<IsciTeyinatCreateDto, IsciTeyinat>();
        CreateMap<IsciTeyinatUpdateDto, IsciTeyinat>();

        CreateMap<IsciStrukturRolu, IsciStrukturRoluDto>()
            .ForMember(d => d.IsciTamAd, o => o.MapFrom(s => s.Isci.TamAd))
            .ForMember(d => d.DepartamentAd, o => o.MapFrom(s => s.Departament != null ? s.Departament.Ad : null));
        CreateMap<IsciStrukturRoluCreateDto, IsciStrukturRolu>();
        CreateMap<IsciStrukturRoluUpdateDto, IsciStrukturRolu>();

        CreateMap<Permission, PermissionDto>();
        CreateMap<PermissionCreateDto, Permission>();
        CreateMap<PermissionUpdateDto, Permission>();

        CreateMap<UserPermission, UserPermissionDto>()
            .ForMember(d => d.UserAdSoyad, o => o.MapFrom(s => s.User.Ad + " " + s.User.Soyad))
            .ForMember(d => d.PermissionKod, o => o.MapFrom(s => s.Permission.Kod))
            .ForMember(d => d.PermissionAd, o => o.MapFrom(s => s.Permission.Ad));
        CreateMap<UserPermissionCreateDto, UserPermission>();
        CreateMap<UserPermissionUpdateDto, UserPermission>();
    }
}