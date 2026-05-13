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

                // ChangePassword brute-force qoruması (cari parolu axtaran istifadəçilərə qarşı)
                options.AddPolicy("changepassword", context =>
                {
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ip,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(5),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            // ==================================================
            // 4. MVC + FluentValidation (.NET 8 way)
            // ==================================================
            builder.Services.AddControllersWithViews(options =>
            {
                // Decimal field-lər həm "." həm "," qəbul etsin
                // (az-AZ kültürü "." -i minlik kimi parse etməsin deyə)
                options.ModelBinderProviders.Insert(0, new Configurations.FlexibleDecimalModelBinderProvider());
            });

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
            // 5.5 SignalR
            // ==================================================
            builder.Services.AddSignalR();

            // ==================================================
            // 6. Background Services
            // ==================================================
            builder.Services.AddHostedService<FinNex.Application.BackgroundJobs.ZkTecoSdkService>();
            builder.Services.AddHostedService<FinNex.Infrastructure.BackgroundJobs.XatirlatmaBackgroundService>();
            builder.Services.AddHostedService<FinNex.Infrastructure.BackgroundJobs.MezuniyyetOdenisSchedulerService>();
            builder.Services.AddHostedService<FinNex.Infrastructure.BackgroundJobs.QayibMarkerBackgroundService>();
            // builder.Services.AddHostedService<FinNex.Infrastructure.BackgroundJobs.ChatCleanupBackgroundService>();
            // builder.Services.AddHostedService<FinNex.Infrastructure.BackgroundJobs.KreditMailBackgroundService>();

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

                // Mezuniyyetler.PlanliOdenisTarixi sütununu əlavə etmə
                // (qabaqcadan ödənişin faktiki bank köçürməsi tarixini saxlayır)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (
                            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                            WHERE TABLE_NAME = 'Mezuniyyetler'
                              AND COLUMN_NAME = 'PlanliOdenisTarixi'
                        )
                        BEGIN
                            ALTER TABLE [Mezuniyyetler] ADD [PlanliOdenisTarixi] DATETIME2 NULL;
                        END
                    ");
                }
                catch { /* artıq tətbiq olunub */ }

                // Mezuniyyetler.OdenenMeblegBrut — qabaqcadan ödəniş brütü (vergi bazası üçün)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (
                            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                            WHERE TABLE_NAME = 'Mezuniyyetler'
                              AND COLUMN_NAME = 'OdenenMeblegBrut'
                        )
                        BEGIN
                            ALTER TABLE [Mezuniyyetler] ADD [OdenenMeblegBrut] DECIMAL(18,2) NULL;
                        END
                    ");
                }
                catch { /* artıq tətbiq olunub */ }

                // SenedSablonlar cədvəlini yaratmaq (şablon sistemi)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (
                            SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                            WHERE TABLE_NAME = 'SenedSablonlar'
                        )
                        BEGIN
                            CREATE TABLE [SenedSablonlar] (
                                [Id] INT IDENTITY(1,1) NOT NULL,
                                [Ad] NVARCHAR(300) NOT NULL,
                                [Tesvir] NVARCHAR(1000) NULL,
                                [SenedNovuId] INT NOT NULL,
                                [FaylYolu] NVARCHAR(500) NOT NULL,
                                [FaylAdi] NVARCHAR(500) NOT NULL,
                                [Aktiv] BIT NOT NULL DEFAULT(1),
                                [YaradilmaTarixi] DATETIME2 NOT NULL DEFAULT(GETDATE()),
                                [YaradanIcraciId] INT NULL,
                                [YenileyenIcraciId] INT NULL,
                                [SilenIcraciId] INT NULL,
                                [YenilenmeTarixi] DATETIME2 NULL,
                                [Silinib] BIT NOT NULL DEFAULT(0),
                                [SilinmeTarixi] DATETIME2 NULL,
                                CONSTRAINT [PK_SenedSablonlar] PRIMARY KEY ([Id]),
                                CONSTRAINT [FK_SenedSablonlar_SenedNovleri_SenedNovuId]
                                    FOREIGN KEY ([SenedNovuId]) REFERENCES [SenedNovleri]([Id])
                                    ON DELETE NO ACTION
                            );
                        END
                    ");
                }
                catch { /* artıq tətbiq olunub */ }

                // Performans çox səviyyəli qiymətləndirmə — SobeReisi sahələri
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansQiymetlendirmeler' AND COLUMN_NAME='SobeReisiId')
                            ALTER TABLE PerformansQiymetlendirmeler ADD SobeReisiId INT NULL;
                        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_PerformansQiymetlendirmeler_Isciler_SobeReisiId')
                            ALTER TABLE PerformansQiymetlendirmeler ADD CONSTRAINT FK_PerformansQiymetlendirmeler_Isciler_SobeReisiId FOREIGN KEY (SobeReisiId) REFERENCES Isciler(Id);
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansQiymetlendirmeler' AND COLUMN_NAME='SobeReisiOrtalamaQiymet')
                            ALTER TABLE PerformansQiymetlendirmeler ADD SobeReisiOrtalamaQiymet DECIMAL(5,2) NOT NULL DEFAULT 0;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansQiymetlendirmeler' AND COLUMN_NAME='SobeReisiSherhi')
                            ALTER TABLE PerformansQiymetlendirmeler ADD SobeReisiSherhi NVARCHAR(MAX) NULL;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansQiymetlendirmeler' AND COLUMN_NAME='SobeReisiQiymetlendirmeTarixi')
                            ALTER TABLE PerformansQiymetlendirmeler ADD SobeReisiQiymetlendirmeTarixi DATETIME2 NULL;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansKriteriyalar' AND COLUMN_NAME='SobeReisiQiymeti')
                            ALTER TABLE PerformansKriteriyalar ADD SobeReisiQiymeti DECIMAL(5,2) NULL;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansKriteriyalar' AND COLUMN_NAME='SobeReisiSherhi')
                            ALTER TABLE PerformansKriteriyalar ADD SobeReisiSherhi NVARCHAR(MAX) NULL;
                    ");
                }
                catch { /* artıq tətbiq olunub */ }

                // Icazeler.JetonOdenenSaat sütununu əlavə etmə (jeton ilə ödənilən icazə saatı)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (
                            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                            WHERE TABLE_NAME = 'Icazeler'
                              AND COLUMN_NAME = 'JetonOdenenSaat'
                        )
                        BEGIN
                            ALTER TABLE [Icazeler] ADD [JetonOdenenSaat] DECIMAL(5,2) NOT NULL DEFAULT 0;
                        END
                    ");
                }
                catch { /* artıq tətbiq olunub */ }

                // FealiyyetJurnali cədvəlini yarat (mövcud deyilsə)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FealiyyetJurnali')
                        BEGIN
                            CREATE TABLE FealiyyetJurnali (
                                Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                UserId      INT NULL,
                                Emeliyyat   NVARCHAR(1) NOT NULL,
                                CedvelAdi   NVARCHAR(100) NOT NULL,
                                CedvelFarsi NVARCHAR(100) NOT NULL,
                                RecordId    INT NOT NULL DEFAULT 0,
                                Acıqlama    NVARCHAR(500) NULL,
                                Tarix       DATETIME2 NOT NULL DEFAULT GETDATE(),
                                CONSTRAINT FK_FealiyyetJurnali_Users FOREIGN KEY (UserId)
                                    REFERENCES AspNetUsers(Id) ON DELETE SET NULL
                            );
                            CREATE INDEX IX_FealiyyetJurnali_UserId  ON FealiyyetJurnali(UserId);
                            CREATE INDEX IX_FealiyyetJurnali_Tarix   ON FealiyyetJurnali(Tarix DESC);
                            CREATE INDEX IX_FealiyyetJurnali_Cedvel  ON FealiyyetJurnali(CedvelAdi);
                        END
                    ");
                }
                catch { /* artıq mövcuddur */ }

                // Avtomatik migration — sadəcə Migrate() çağır, xəta olsa logla amma crash etmə
                try
                {
                    var pending = db.Database.GetPendingMigrations().ToList();
                    if (pending.Any())
                    {
                        Console.WriteLine($"[Migration] {pending.Count} pending migration tapıldı: {string.Join(", ", pending)}");
                        db.Database.Migrate();
                        Console.WriteLine("[Migration] Bütün migration-lar uğurla tətbiq olundu.");
                    }
                }
                catch (Exception ex)
                {
                    // Migration xətası app-ı crash etməsin — amma logla
                    Console.WriteLine($"[Migration XƏTA] {ex.Message}");
                    Console.WriteLine($"[Migration XƏTA] Əl ilə 'Update-Database' və ya SQL script işlədin.");
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

            app.MapHub<FinNex.UI.Hubs.ChatHub>("/chatHub");

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
