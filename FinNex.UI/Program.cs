using FinNex.Application;
using FinNex.DataAccess;
using FinNex.DataAccess.Contexts;
using FinNex.DataAccess.Seed;
using FinNex.UI.Configurations;
using FinNex.UI.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

namespace FinNex.UI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Tarix formatı: dd.MM.yyyy (Azərbaycan)
            var azCulture = new CultureInfo("az-Latn-AZ");
            CultureInfo.DefaultThreadCurrentCulture = azCulture;
            CultureInfo.DefaultThreadCurrentUICulture = azCulture;

            //bunu sileceyik
            //builder.WebHost.ConfigureKestrel(options =>
            //{
            //    options.ListenAnyIP(7172); // HTTPS
            //    options.ListenAnyIP(5172); // HTTP (şəbəkə üçün rahat)
            //});
            //

            // ==================================================
            // 1. DataAccess + Identity
            // ==================================================
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDataAccessServices(builder.Configuration);
            builder.Services.AddApplicationServices();

            // ==================================================
            // 2. Authentication (Cookie-based)
            // ==================================================
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        // 🔥 RememberMe üçün
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = false; // 🔥 VACİB

        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

            builder.Services.AddAppAuthorization();

            // ==================================================
            // 3. Rate Limiting (Login protection)
            // ==================================================
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("login", context =>
                {
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ip,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,                   // 5 cəhd
                            Window = TimeSpan.FromMinutes(1),  // 1 dəqiqə
                            QueueLimit = 0,                    // gözlətmə YOX
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            // ==================================================
            // 4. MVC + FluentValidation (.NET 8 way)
            // ==================================================
            builder.Services.AddControllersWithViews();

            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddFluentValidationClientsideAdapters();
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            // ==================================================
            // 5. Logging (basic – Console + Debug)
            // ==================================================
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            // ==================================================
            // 6. Background Services
            // ==================================================
            builder.Services.AddHostedService<FinNex.Application.BackgroundJobs.ZkTecoSdkService>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var pendingMigrations = db.Database.GetPendingMigrations().ToList();
                if (pendingMigrations.Any())
                {
                    try
                    {
                        db.Database.Migrate();
                    }
                    catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2714)
                    {
                        // Cədvəllər artıq mövcuddur - migration tarixçəsinə əlavə et
                        var productVersion = typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "9.0.0";
                        foreach (var migration in pendingMigrations)
                        {
                            db.Database.ExecuteSqlRaw(
                                "IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = {0}) INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({0}, {1})",
                                migration, productVersion);
                        }
                    }
                }
            }

            // ==================================================
            // 6. Middleware pipeline (ORDER IS CRITICAL)
            // ==================================================

            // 🔥 Global exception handler
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Tarix/rəqəm formatı Azərbaycan
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("az-Latn-AZ"),
                SupportedCultures = new[] { new CultureInfo("az-Latn-AZ") },
                SupportedUICultures = new[] { new CultureInfo("az-Latn-AZ") }
            });

            // 🔐 Security headers
            app.UseMiddleware<SecurityHeadersMiddleware>();

            app.UseStaticFiles();

            app.UseRouting();

            // 🔐 Rate Limiting (auth-dan əvvəl)
            app.UseRateLimiter();

            // 🔐 Authentication / Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            // ==================================================
            // 7. Identity Seed (Admin + Roles)
            // ==================================================
            await IdentitySeed.SeedAsync(app.Services);

            app.Run();
        }
    }
}
