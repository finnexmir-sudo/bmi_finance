
using FinNex.Application.Interfaces.PR_Document;
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Application.MappingProfile.PR_Document;
using FinNex.Application.MappingProfile.PR_Odenis_Tapsirigi;
using FinNex.Application.Services;
using FinNex.Application.Services.PR_Document;
using FinNex.Application.Services.PR_Odenis_Tapsirigi;
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

        // PR_Document
        services.AddScoped<ISenedIdareetmeService, SenedIdareetmeService>();

        // AutoMapper üçün aşağıdakı düzəlişə bax
        services.AddAutoMapper(typeof(Mapping), typeof(DocumentMapping));
    }
}