using FinNex.DataAccess.Contexts;
using FinNex.DataAccess.Repositories;
using FinNex.DataAccess.UnitOfWork;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;

namespace FinNex.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. DbContext-i qeydiyyatdan keçiririk
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            // 2. Repository və UnitOfWork-u sistemə tanıdırıq (Dependency Injection)
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped(typeof(IRepositoryAsync<>), typeof(EfRepositoryAsync<>));

            // Digər standart servisler...
            builder.Services.AddControllersWithViews();

            builder.WebHost.UseUrls(
    "https://0.0.0.0:7172",
    "http://0.0.0.0:5172"
);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
