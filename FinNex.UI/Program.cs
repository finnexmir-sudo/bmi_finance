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
using Serilog;
using Serilog.Events;

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

        // Sessiya müddəti: 30 dəqiqə, aktiv istifadədə uzanır
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

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
            // 5. Logging (Serilog – Console + File)
            // ==================================================
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(builder.Environment.ContentRootPath, "Logs", "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            builder.Host.UseSerilog();

            // ==================================================
            // 6. Background Services
            // ==================================================
            builder.Services.AddHostedService<FinNex.Application.BackgroundJobs.ZkTecoSdkService>();
            builder.Services.AddHostedService<FinNex.Infrastructure.BackgroundJobs.XatirlatmaBackgroundService>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Icaze.EvezEdenIsciId nullable etmə (əvəzedici işçi artıq məcburi deyil)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF EXISTS (
                            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                            WHERE TABLE_NAME = 'Icazeler'
                              AND COLUMN_NAME = 'EvezEdenIsciId'
                              AND IS_NULLABLE = 'NO'
                        )
                        BEGIN
                            ALTER TABLE [Icazeler] DROP CONSTRAINT IF EXISTS [FK_Icazeler_Isciler_EvezEdenIsciId];
                            ALTER TABLE [Icazeler] ALTER COLUMN [EvezEdenIsciId] INT NULL;
                            ALTER TABLE [Icazeler] ADD CONSTRAINT [FK_Icazeler_Isciler_EvezEdenIsciId]
                                FOREIGN KEY ([EvezEdenIsciId]) REFERENCES [Isciler]([Id]);
                        END
                    ");
                }
                catch { /* artıq tətbiq olunub */ }

                // Senedler.SenedNomresi sütununu əlavə etmə (avtomatik sənəd nömrələməsi)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (
                            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                            WHERE TABLE_NAME = 'Senedler'
                              AND COLUMN_NAME = 'SenedNomresi'
                        )
                        BEGIN
                            ALTER TABLE [Senedler] ADD [SenedNomresi] NVARCHAR(MAX) NULL;
                        END
                    ");
                }
                catch { /* artıq tətbiq olunub */ }

                var pendingMigrations = db.Database.GetPendingMigrations().ToList();
                var productVersion = typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "9.0.0";

                foreach (var migration in pendingMigrations)
                {
                    try
                    {
                        db.Database.Migrate();
                        break; // uğurlu olsa, hamısını tətbiq edib — çıx
                    }
                    catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2714 or 3701 or 1913)
                    {
                        // Bu migration artıq tətbiq olunub və ya indeks yoxdur — qeyd et və növbətiyə keç
                        db.Database.ExecuteSqlRaw(
                            "IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = {0}) INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({0}, {1})",
                            migration, productVersion);
                    }
                }
            }

            // ==================================================
            // 6. Middleware pipeline (ORDER IS CRITICAL)
            // ==================================================

            // 🔥 Global exception handler
            app.UseMiddleware<GlobalExceptionMiddleware>();

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
