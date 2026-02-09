using FinNex.DataAccess.Contexts;
using FinNex.DataAccess.Repositories;
using FinNex.DataAccess.UnitOfWork;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinNex.DataAccess;

public static class ServiceRegistration
{
    public static void AddDataAccessServices(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext qeydiyyatı
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repository və UnitOfWork qeydiyyatı
        services.AddScoped<IUnitOfWork, IUnitOfWork>();
        services.AddScoped(typeof(IRepositoryAsync<>), typeof(EfRepositoryAsync<>));
    }
}