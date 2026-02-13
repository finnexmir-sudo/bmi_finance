
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Application.Interfaces.Structur;
using FinNex.Application.MappingProfile;
using FinNex.Application.MappingProfile.PR_Odenis_Tapsirigi;
using FinNex.Application.Services;
using FinNex.Application.Services.PR_Odenis_Tapsirigi;
using FinNex.Application.Services.SenedDovriyyesi;
using FinNex.Application.Services.Structur;
using FinNex.DataAccess.Repositories;
using FinNex.DataAccess.UnitOfWorks;
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
        services.AddScoped<IAuditLogService, AuditLogService>();

        // Storage
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // DataAccess registration
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepositoryAsync<>), typeof(EfRepositoryAsync<>));


        // AutoMapper üçün aşağıdakı düzəlişə bax
        //services.AddAutoMapper(typeof(Mapping));
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    }
}