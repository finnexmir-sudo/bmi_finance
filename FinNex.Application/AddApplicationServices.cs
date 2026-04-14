
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.HR;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Application.Interfaces.Structur;
using FinNex.Application.Services;
using FinNex.Application.Services.Communication;
using FinNex.Application.Services.HR;
using FinNex.Application.Services.PR_Odenis_Tapsirigi;
using FinNex.Application.Services.SenedDovriyyesi;
using FinNex.Application.Services.Structur;
using FinNex.Application.Services.Structure;
using FinNex.DataAccess.Repositories;
using FinNex.DataAccess.UnitOfWorks;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FinNex.Application;

public static class ServiceRegistration // Class mütləq static olmalıdır
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBankService, BankService>();
        services.AddScoped<IBankHesabiService, BankHesabiService>();
        services.AddScoped<IMusteriService, MusteriService>();
        services.AddScoped<IMusteriHesabiService, MusteriHesabiService>();
        services.AddScoped<IOdenisTapsirigiService, OdenisTapsirigiService>();
        services.AddScoped<IOdenisTapsirigiNomreService, OdenisTapsirigiNomreService>();
        services.AddScoped<IValyutaService, ValyutaService>();
        services.AddScoped<IUserDepartmentService, UserDepartmentService>();
        services.AddScoped<IDepartmentService, DepartmentService>();


        // AutoMapper: profil assembly scan
        //services.AddAutoMapper(typeof(SenedDovriyyesiProfile).Assembly);

        // Sened dovriyyesi
        services.AddScoped<ISenedService, SenedService>();
        services.AddScoped<ISenedNovuService, SenedNovuService>();
        services.AddScoped<ISenedDovriyyesiIstifadeciIcazesiService, SenedDovriyyesiIstifadeciIcazesiService>();

        services.AddScoped<IOdenisTapsirigiNomreService, OdenisTapsirigiNomreService>();

        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ISenedSablonService, SenedSablonService>();

        // Storage
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // DataAccess registration
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepositoryAsync<>), typeof(EfRepositoryAsync<>));


        // AutoMapper üçün aşağıdakı düzəlişə bax
        //services.AddAutoMapper(typeof(Mapping));
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        services.AddScoped<IIsciService, IsciService>();
        services.AddScoped<IVezifeService, VezifeService>();
        services.AddScoped<IMaasService, MaasService>();
        services.AddScoped<IIsciAyliqQazancService, IsciAyliqQazancService>();
        services.AddScoped<IXestelikService, XestelikService>();
        services.AddScoped<IDavamiyyetService, DavamiyyetService>();
        // Məzuniyyət Modulu Servisləri
        services.AddScoped<IMezuniyyetService, MezuniyyetService>();
        services.AddScoped<IIcazeService, IcazeService>();

        services.AddScoped<IDashboardService, DashboardService>();

        services.AddScoped<IIsciTeyinatService, IsciTeyinatService>();
        services.AddScoped<IIsciStrukturRoluService, IsciStrukturRoluService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserPermissionService, UserPermissionService>();

        services.AddScoped<IIsciMaliyeService, IsciMaliyeService>();
        services.AddScoped<IMaasNovuService, MaasNovuService>();
        services.AddScoped<IMaasParametriService, MaasParametriService>();
        services.AddScoped<IMaasDetayService, MaasDetayService>();
        services.AddScoped<IIsciMaasTarixcesiService, IsciMaasTarixcesiService>();

        services.AddScoped<IBildirisService, BildirisService>();
        services.AddScoped<IMesajService, MesajService>();
        services.AddScoped<IEvezediciTesdiqService, EvezediciTesdiqService>();

        services.AddScoped<ITapshiriqService, TapshiriqService>();
        services.AddScoped<IGorushService, GorushService>();
        services.AddScoped<IXatirlatmaService, XatirlatmaService>();

        // AddApplicationServices.cs — yeni servis əlavələri
        // Mövcud qeydiyyatlara əlavə edin:

        // ── Maaş Hesablama Engine ───────────────────────────────────
        services.AddScoped<IMaasHesablamaService, MaasHesablamaService>();

        // ── Hesabat İzləmə ────────────────────────────────────
        services.AddScoped<IHesabatIzlemeService, HesabatIzlemeService>();


        // ── Parametri servisini yenilə (artıq var, sadəcə yoxla) ──
        // services.AddScoped<IMaasParametriService, MaasParametriService>();


        // Əgər BayramGunu və Balans üçün xüsusi məntiq yazmamısansa, 
        // onları generik servis üzərindən belə qeydiyyatdan keçirə bilərsən:
        services.AddScoped(typeof(IServiceAsync<BayramGunu, BayramGunuDto, BayramGunuCreateDto, BayramGunuUpdateDto>), typeof(ServiceAsync<BayramGunu, BayramGunuDto, BayramGunuCreateDto, BayramGunuUpdateDto>));
        services.AddScoped(typeof(IServiceAsync<MezuniyyetBalans, MezuniyyetBalansDto, MezuniyyetBalansCreateDto, MezuniyyetBalansUpdateDto>), typeof(ServiceAsync<MezuniyyetBalans, MezuniyyetBalansDto, MezuniyyetBalansCreateDto, MezuniyyetBalansUpdateDto>));

    }
}