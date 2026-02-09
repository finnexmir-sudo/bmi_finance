
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
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

        // AutoMapper üçün aşağıdakı düzəlişə bax
        services.AddAutoMapper(typeof(MappingProfile));
    }
}