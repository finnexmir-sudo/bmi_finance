using FinNex.DataAccess.Contexts;
using FinNex.DataAccess.Repositories;
using FinNex.DataAccess.UnitOfWorks;
using FinNex.Domain;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinNex.DataAccess
{
    public static class ServiceRegistration
    {
        public static void AddDataAccessServices(this IServiceCollection services, IConfiguration configuration)
        {
            // =========================
            // 1. DbContext (Identity ilə)
            // =========================
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // =========================
            // 2. ASP.NET Identity
            // =========================
            services.AddIdentity<AppUser, AppRole>(options =>
            {
                // 🔐 Password qaydaları (minimum bank standardı)
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                // 🔒 Brute-force qorunması
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;

                // 👤 User ayarları
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // =========================
            // 3. Repository Pattern
            // =========================
            services.AddScoped(typeof(IRepositoryAsync<>), typeof(EfRepositoryAsync<>));

            // =========================
            // 4. Unit Of Work
            // =========================
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }
    }
}
