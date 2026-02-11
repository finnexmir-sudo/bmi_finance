
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Application.MappingProfile;
using FinNex.Application.MappingProfile.PR_Odenis_Tapsirigi;
using FinNex.Application.Services;
using FinNex.Application.Services.PR_Odenis_Tapsirigi;
using FinNex.Application.Services.SenedDovriyyesi;
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

        // AutoMapper: profil assembly scan
        services.AddAutoMapper(typeof(SenedDovriyyesiProfile).Assembly);

        // Sened dovriyyesi
        services.AddScoped<ISenedService, SenedService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // Storage (bunu Infrastructure/DataAccess-da edəcəksən)
        // services.AddScoped<IFileStorageService, FileServerStorageService>();

        // AutoMapper üçün aşağıdakı düzəlişə bax
        services.AddAutoMapper(typeof(Mapping));
    }
}