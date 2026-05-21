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
            // 5.6 Gələn Mail / AI Servisləri
            // ==================================================
            builder.Services.AddHttpClient("Anthropic");

            // ── Oracle (yalnız oxuma) ──────────────────────────
            builder.Services.AddScoped<FinNex.Application.Interfaces.Oracle.IOracleService,
                                        FinNex.Application.Services.Oracle.OracleService>();
            builder.Services.AddScoped<FinNex.Application.Interfaces.Sorgular.IOracleSorguService,
                                        FinNex.Application.Services.Sorgular.OracleSorguService>();
            builder.Services.AddScoped<FinNex.Application.Interfaces.ISistemAyarService,
                                        FinNex.Application.Services.SistemAyarService>();

            // ── Göyərçin (Posta Güvercini) SMS Gateway ────────
            builder.Services.Configure<FinNex.Application.Services.Sms.GoyercinSettings>(
                builder.Configuration.GetSection("Goyercin"));
            builder.Services.AddHttpClient<FinNex.Application.Interfaces.Sms.ISmsGateway,
                                            FinNex.Application.Services.Sms.GoyercinSmsGateway>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddScoped<FinNex.Application.Interfaces.Communication.IGelenMailService,
                                        FinNex.Application.Services.Communication.GelenMailService>();
            builder.Services.AddScoped<FinNex.Application.Interfaces.Communication.IAnthropicService,
                                        FinNex.Application.Services.Communication.AnthropicService>();
            builder.Services.AddScoped<FinNex.Application.Interfaces.Communication.IAttachmentTextExtractor,
                                        FinNex.Application.Services.Communication.AttachmentTextExtractor>();
            builder.Services.AddScoped<FinNex.Application.Interfaces.Communication.ISmtpMailService,
                                        FinNex.Application.Services.Communication.SmtpMailService>();
            builder.Services.AddScoped<FinNex.Application.Interfaces.Communication.IGelenMailImapSyncer,
                                        FinNex.Application.Services.Communication.GelenMailImapSyncer>();

            // ==================================================
            // 6. Background Services
            // ==================================================
            builder.Services.AddHostedService<FinNex.Application.BackgroundJobs.ZkTecoSdkService>();
            builder.Services.AddHostedService<FinNex.Infrastructure.BackgroundJobs.XatirlatmaBackgroundService>();
            builder.Services.AddHostedService<FinNex.Infrastructure.BackgroundJobs.MezuniyyetOdenisSchedulerService>();
            builder.Services.AddHostedService<FinNex.Infrastructure.BackgroundJobs.QayibMarkerBackgroundService>();
            builder.Services.AddHostedService<FinNex.Infrastructure.BackgroundJobs.GelenMailSyncService>();
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

                // Performans — Rehber2 və MenecerKriteriyalari sütunları
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansQiymetlendirmeler' AND COLUMN_NAME='Rehber2Id')
                            ALTER TABLE PerformansQiymetlendirmeler ADD Rehber2Id INT NULL;
                        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_Performans_Isciler_Rehber2Id')
                        BEGIN
                            IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansQiymetlendirmeler' AND COLUMN_NAME='Rehber2Id')
                                ALTER TABLE PerformansQiymetlendirmeler ADD CONSTRAINT FK_Performans_Isciler_Rehber2Id FOREIGN KEY (Rehber2Id) REFERENCES Isciler(Id);
                        END
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansQiymetlendirmeler' AND COLUMN_NAME='Rehber2OrtalamaQiymet')
                            ALTER TABLE PerformansQiymetlendirmeler ADD Rehber2OrtalamaQiymet DECIMAL(5,2) NOT NULL DEFAULT 0;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansQiymetlendirmeler' AND COLUMN_NAME='Rehber2Sherhi')
                            ALTER TABLE PerformansQiymetlendirmeler ADD Rehber2Sherhi NVARCHAR(MAX) NULL;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansQiymetlendirmeler' AND COLUMN_NAME='Rehber2QiymetlendirmeTarixi')
                            ALTER TABLE PerformansQiymetlendirmeler ADD Rehber2QiymetlendirmeTarixi DATETIME2 NULL;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansQiymetlendirmeler' AND COLUMN_NAME='MenecerKriteriyalari')
                            ALTER TABLE PerformansQiymetlendirmeler ADD MenecerKriteriyalari BIT NOT NULL DEFAULT 0;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansKriteriyalar' AND COLUMN_NAME='Rehber2Qiymeti')
                            ALTER TABLE PerformansKriteriyalar ADD Rehber2Qiymeti DECIMAL(5,2) NULL;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansKriteriyalar' AND COLUMN_NAME='Rehber2Sherhi')
                            ALTER TABLE PerformansKriteriyalar ADD Rehber2Sherhi NVARCHAR(MAX) NULL;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PerformansQiymetlendirmeler' AND COLUMN_NAME='KampaniyaTipi')
                            ALTER TABLE PerformansQiymetlendirmeler ADD KampaniyaTipi INT NOT NULL DEFAULT 1;
                    ");
                }
                catch { }

                // PerformansKriteriyaSablonlar cədvəlini yarat + default məlumatlar
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PerformansKriteriyaSablonlar')
                        BEGIN
                            CREATE TABLE PerformansKriteriyaSablonlar (
                                Id                  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                Ad                  NVARCHAR(200) NOT NULL,
                                Ceki                DECIMAL(5,2)  NOT NULL,
                                KampaniyaTipi       INT           NOT NULL,
                                Aktiv               BIT           NOT NULL DEFAULT 1,
                                Sira                INT           NOT NULL DEFAULT 0,
                                YaradilmaTarixi     DATETIME2     NOT NULL DEFAULT GETDATE(),
                                YaradanIcraciId     INT           NULL,
                                YenileyenIcraciId   INT           NULL,
                                SilenIcraciId       INT           NULL,
                                YenilenmeTarixi     DATETIME2     NULL,
                                Silinib             BIT           NOT NULL DEFAULT 0,
                                SilinmeTarixi       DATETIME2     NULL
                            );
                            INSERT INTO PerformansKriteriyaSablonlar (Ad, Ceki, KampaniyaTipi, Aktiv, Sira) VALUES
                            (N'İş keyfiyyəti',   30, 1, 1, 1),
                            (N'Vaxtında icra',   20, 1, 1, 2),
                            (N'Komanda işi',     20, 1, 1, 3),
                            (N'Təşəbbüskarlıq',  15, 1, 1, 4),
                            (N'Peşəkar inkişaf', 15, 1, 1, 5),
                            (N'Liderlik',            25, 2, 1, 1),
                            (N'Komanda idarəetməsi', 25, 2, 1, 2),
                            (N'Nəticəyönümlülük',    20, 2, 1, 3),
                            (N'Ünsiyyət',            15, 2, 1, 4),
                            (N'İnkişaf dəstəyi',     15, 2, 1, 5);
                        END
                    ");
                }
                catch { }

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

                // DsmfTarixce cədvəlini yarat və keçmiş məlumatları yüklə
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DsmfTarixceler')
                        BEGIN
                            CREATE TABLE DsmfTarixceler (
                                Id         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                IsciId     INT NOT NULL,
                                Il         INT NOT NULL,
                                Ay         INT NOT NULL,
                                IsciDsmf   DECIMAL(10,2) NOT NULL DEFAULT 0,
                                SirketDsmf DECIMAL(10,2) NOT NULL DEFAULT 0,
                                CONSTRAINT FK_DsmfTarixceler_Isciler FOREIGN KEY (IsciId) REFERENCES Isciler(Id),
                                CONSTRAINT UQ_DsmfTarixceler_IsciIlAy UNIQUE (IsciId, Il, Ay)
                            );
                            CREATE INDEX IX_DsmfTarixceler_IsciId ON DsmfTarixceler(IsciId);
                            CREATE INDEX IX_DsmfTarixceler_IlAy   ON DsmfTarixceler(Il, Ay);
                        END
                    ");
                }
                catch { }

                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM DsmfTarixceler)
                        BEGIN
                            INSERT INTO DsmfTarixceler (IsciId, Il, Ay, IsciDsmf, SirketDsmf) VALUES
                            (1,2026,4,181.11,306.66),(1,2026,3,330.3,530.45),(1,2026,2,150.2,260.3),(1,2026,1,89.33,169),
                            (1,2025,12,225.63,373.45),(1,2025,11,237.69,391.53),(1,2025,10,191.77,322.66),(1,2025,9,80.28,155.43),
                            (1,2025,8,80.43,155.64),(1,2025,7,78.3,152.45),(1,2025,6,74.24,146.36),(1,2025,5,78.3,152.45),
                            (2,2026,4,336,539),(2,2026,3,712.4,1103.6),(2,2026,2,413.5,655.25),(2,2026,1,336,539),
                            (2,2025,12,551.9,862.85),(2,2025,11,761.33,1177),(2,2025,10,320.59,515.88),(2,2025,9,320.11,515.16),
                            (2,2025,8,544.19,851.28),(2,2025,7,316,509),(2,2025,6,510.3,800.45),(2,2025,5,441.8,697.7),
                            (3,2026,4,453.79,715.68),(3,2026,3,887.05,1325.16),(3,2026,2,679.6,1054.41),(3,2026,1,461.27,726.91),
                            (3,2025,12,800.67,1236.01),(3,2025,11,580.35,905.53),(3,2025,10,418.44,662.66),(3,2025,9,415.2,657.8),
                            (3,2025,8,418.85,663.27),(3,2025,7,420.49,665.74),(3,2025,6,588.37,917.56),(3,2025,5,439.77,694.66),
                            (4,2026,4,280.55,455.82),(4,2026,3,550.8,861.2),(4,2026,2,273.33,445),(4,2026,1,256,419),
                            (4,2025,12,441.7,697.55),(4,2025,11,458.8,723.2),(4,2025,10,237.51,391.26),(4,2025,9,237.84,391.77),
                            (4,2025,8,335.65,538.47),(4,2025,7,236,389),(4,2025,6,236,389),(4,2025,5,236,389),
                            (5,2026,4,366,584),(5,2026,3,787.36,1216.04),(5,2026,2,415.73,658.6),(5,2026,1,361.64,577.46),
                            (5,2025,12,621.17,966.76),(5,2025,11,612.8,954.2),(5,2025,10,336,539),(5,2025,9,355.07,567.6),
                            (5,2025,8,402.09,638.14),(5,2025,7,346,554),(5,2025,6,346,554),(5,2025,5,346,554),
                            (6,2026,4,278.24,452.36),(6,2026,3,629.4,979.11),(6,2026,2,444.43,701.64),(6,2026,1,236,389),
                            (6,2025,12,586.7,915.05),(6,2025,11,599.9,934.85),(6,2025,10,309.68,499.52),(6,2025,9,309.29,498.94),
                            (6,2025,8,351.94,562.91),(6,2025,7,416.87,660.3),(6,2025,6,500.3,785.45),(6,2025,5,473.84,745.76),
                            (7,2026,4,337.48,541.21),(7,2026,3,713.68,1105.52),(7,2026,2,412.24,653.36),(7,2026,1,528.32,827.48),
                            (7,2025,12,587.96,916.94),(7,2025,11,618.2,962.3),(7,2025,10,328.16,527.25),(7,2025,9,349.24,558.86),
                            (7,2025,8,322.11,518.16),(7,2025,7,310.02,500.03),(7,2025,6,499.29,783.94),(7,2025,5,431.8,682.7),
                            (8,2026,4,234.39,386.59),(8,2026,3,519.2,813.8),(8,2026,2,283.05,459.57),(8,2026,1,233.1,384.65),
                            (8,2025,12,432.23,683.34),(8,2025,11,452.1,713.15),(8,2025,10,277.88,451.81),(8,2025,9,189.29,318.94),
                            (8,2025,8,219.42,364.13),(8,2025,7,354.68,567.02),(8,2025,6,335.68,538.52),(8,2025,5,301.54,487.32),
                            (9,2026,4,191,321.5),(9,2026,3,465.6,733.4),(9,2026,2,252.3,413.45),(9,2026,1,191,321.5),
                            (9,2025,12,422.51,668.77),(9,2025,11,388.75,618.12),(9,2025,10,179.35,304.02),(9,2025,9,193.93,325.9),
                            (9,2025,8,178.73,303.1),(9,2025,7,176,299),(9,2025,6,313.2,504.8),(9,2025,5,176,299),
                            (10,2026,4,168.83,288.24),(10,2026,3,333.44,535.16),(10,2026,2,183.4,310.1),(10,2026,1,200.36,335.54),
                            (10,2025,12,252.3,413.45),(10,2025,11,280.58,455.88),(10,2025,10,126.62,224.93),(10,2025,9,126.34,224.51),
                            (10,2025,8,165.02,282.53),(10,2025,7,145.62,253.43),(10,2025,6,251.1,411.65),(10,2025,5,125.3,222.95),
                            (11,2026,4,149,258.5),(11,2026,3,387.34,616.01),(11,2026,2,207.76,346.64),(11,2026,1,174.33,296.49),
                            (11,2025,12,325.89,523.84),(11,2025,11,314.8,507.2),(11,2025,10,146.2,254.3),(11,2025,9,209.66,349.48),
                            (11,2025,8,163.22,279.83),(11,2025,7,146.46,254.69),(11,2025,6,272.32,443.48),(11,2025,5,146.65,254.98),
                            (12,2026,4,186,314),(12,2026,3,349.51,559.26),(12,2026,2,186,314),(12,2026,1,182.35,308.53),
                            (12,2025,12,261.1,426.64),(12,2025,11,288.06,467.1),(12,2025,10,166.09,284.13),(12,2025,9,160.77,276.15),
                            (12,2025,8,169.22,288.83),(12,2025,7,164.44,281.66),(12,2025,6,166.46,284.69),(12,2025,5,161.88,277.82),
                            (13,2026,4,210.55,350.82),(13,2026,3,394.8,627.2),(13,2026,2,282.7,459.05),(13,2026,1,176,299),
                            (13,2025,12,307.3,495.95),(13,2025,11,293.58,475.37),(13,2025,10,181.58,307.37),(13,2025,9,146,254),
                            (13,2025,8,178.52,302.77),(13,2025,7,146,254),(13,2025,6,223.9,370.85),(13,2025,5,146,254),
                            (14,2026,4,162.5,278.75),(14,2026,3,380.39,605.58),(14,2026,2,170.98,291.47),(14,2026,1,160.03,275.05),
                            (14,2025,12,319.7,514.55),(14,2025,11,282.71,459.06),(14,2025,10,190.87,321.31),(14,2025,9,168.97,288.45),
                            (14,2025,8,188.71,318.06),(14,2025,7,171.9,292.86),(14,2025,6,166,284),(14,2025,5,166,284),
                            (15,2026,4,276,449),(15,2026,3,591.2,921.8),(15,2026,2,276,449),(15,2026,1,276,449),
                            (15,2025,12,475,747.5),(15,2025,11,478.28,752.43),(15,2025,10,273.02,444.53),(15,2025,9,259.95,424.92),
                            (15,2025,8,259.56,424.34),(15,2025,7,290.86,471.29),(15,2025,6,250.53,410.8),(15,2025,5,256.58,419.87),
                            (16,2026,4,172.35,293.52),(16,2026,3,387.4,616.1),(16,2026,2,218.71,363.06),(16,2026,1,171.16,291.74),
                            (16,2025,12,293.5,475.25),(16,2025,11,311,501.5),(16,2025,10,160.61,275.91),(16,2025,9,143.55,250.32),
                            (16,2025,8,143.23,249.84),(16,2025,7,200.12,335.17),(16,2025,6,209.6,349.4),(16,2025,5,141,246.5),
                            (17,2026,4,92.36,173.55),(17,2026,3,168.4,287.6),(17,2026,2,76,149),(17,2026,1,80.7,156.05),
                            (17,2025,12,147.7,256.55),(17,2025,11,118.3,212.45),(17,2025,10,80.23,155.34),(17,2025,9,72.89,144.34),
                            (17,2025,8,61,126.5),(17,2025,7,67.02,135.53),(17,2025,6,59.69,124.53),(17,2025,5,61,126.5),
                            (18,2026,4,81.46,157.18),(18,2026,3,199.35,334.03),(18,2026,2,90.05,170.07),(18,2026,1,91,171.5),
                            (18,2025,12,175.6,298.4),(18,2025,11,133.2,234.8),(18,2025,10,76.52,149.78),(18,2025,9,76,149),
                            (18,2025,8,85.55,163.33),(18,2025,7,75.28,147.91),(18,2025,6,76,149),(18,2025,5,76,149),
                            (19,2026,4,204.19,341.29),(19,2026,3,490.69,771.04),(19,2026,2,379.79,604.69),(19,2026,1,204.19,341.29),
                            (19,2025,12,422.89,669.33),(19,2025,11,340.8,546.2),(19,2025,10,268.03,437.04),(19,2025,9,252.53,413.79),
                            (19,2025,8,198.98,333.47),(19,2025,7,292.7,474.05),(19,2025,6,387.09,615.63),(19,2025,5,196.39,329.58),
                            (20,2026,4,51,111.5),(20,2026,3,111.51,202.26),(20,2026,2,49.69,109.54),(20,2026,1,51,111.5),
                            (20,2025,12,68.8,138.2),(20,2025,11,68.9,138.35),(20,2025,10,46,104),(20,2025,9,46,104),
                            (20,2025,8,46,104),(20,2025,7,46,104),(20,2025,6,46,104),(20,2025,5,46,104),
                            (21,2026,4,76,149),(21,2026,3,107.35,196.03),(21,2026,2,74.65,146.97),(21,2026,1,76,149),
                            (21,2025,12,87.1,165.65),(21,2025,11,88.9,168.35),(21,2025,10,66,134),(21,2025,9,62.36,128.55),
                            (21,2025,8,66,134),(21,2025,7,66,134),(21,2025,6,66,134),
                            (22,2026,4,61,126.5),(22,2026,3,93.16,174.73),(22,2026,2,61,126.5),(22,2026,1,58.84,123.27),
                            (22,2025,12,68.8,138.2),(22,2025,11,68.9,138.35),(22,2025,10,46,104),(22,2025,9,26,74),
                            (22,2025,8,26,74),(22,2025,7,17.3,60.96),
                            (23,2026,4,61,126.5),(23,2026,3,91,171.5),(23,2026,2,61,126.5),(23,2026,1,61,126.5),
                            (23,2025,12,68.8,138.2),(23,2025,11,68.9,138.35),(23,2025,10,43.39,100.09),(23,2025,9,26,74),
                            (23,2025,8,26,74),(23,2025,7,3.65,26.78),
                            (24,2026,4,31,81.5),(24,2026,3,41,96.5),(24,2026,2,31,81.5),
                            (25,2026,4,885.99,1323.99),(25,2026,3,2096.66,2655.72),(25,2026,2,816.77,1247.85),(25,2026,1,862.15,1297.77),
                            (25,2025,12,846.04,1304.06),(25,2025,11,858.16,1322.25),(25,2025,10,710,1100),(25,2025,9,749.87,1159.8),
                            (25,2025,8,877.3,1350.94),(25,2025,7,777.76,1201.65),(25,2025,6,899.16,1383.74),(25,2025,5,713.92,1105.88);
                        END
                    ");
                }
                catch { }

                // DsmfTarixceler → IsciAyliqQazanclar sinxronizasiya (xəstəlik hesablaması üçün)
                try
                {
                    // 1. Yeni qeydlər — IsciAyliqQazanclar-da olmayan ayları əlavə et
                    db.Database.ExecuteSqlRaw(@"
                        INSERT INTO IsciAyliqQazanclar
                            (IsciId, Il, Ay, Qazanc, DsmfIsci, DsmfIsegoturen,
                             ElIleDaxilEdilib, Silinib, YaradilmaTarixi)
                        SELECT d.IsciId, d.Il, d.Ay, 0, d.IsciDsmf, d.SirketDsmf,
                               1, 0, GETDATE()
                        FROM DsmfTarixceler d
                        WHERE NOT EXISTS (
                            SELECT 1 FROM IsciAyliqQazanclar q
                            WHERE q.IsciId = d.IsciId
                              AND q.Il     = d.Il
                              AND q.Ay     = d.Ay
                              AND q.Silinib = 0
                        )
                    ");

                    // 2. Mövcud qeydlər DsmfIsci=0 isə DsmfTarixceler-dən yenilə
                    db.Database.ExecuteSqlRaw(@"
                        UPDATE q
                        SET q.DsmfIsci        = d.IsciDsmf,
                            q.DsmfIsegoturen  = d.SirketDsmf,
                            q.YenilenmeTarixi = GETDATE()
                        FROM IsciAyliqQazanclar q
                        JOIN DsmfTarixceler d
                          ON d.IsciId = q.IsciId
                         AND d.Il     = q.Il
                         AND d.Ay     = q.Ay
                        WHERE q.Silinib    = 0
                          AND q.DsmfIsci   = 0
                          AND q.DsmfIsegoturen = 0
                    ");
                }
                catch { }

                // MaasNovleri — koreksiya növlərini əlavə et (idempotent)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        SET IDENTITY_INSERT MaasNovleri ON;
                        IF NOT EXISTS (SELECT 1 FROM MaasNovleri WHERE Ad = N'Əvvəlki Ay Artıq Ödəniş Kəsintisi')
                            INSERT INTO MaasNovleri (Id, Ad, Tip, Aktivdir, Silinib, YaradilmaTarixi)
                            VALUES (19, N'Əvvəlki Ay Artıq Ödəniş Kəsintisi', 2, 1, 0, GETDATE());
                        IF NOT EXISTS (SELECT 1 FROM MaasNovleri WHERE Ad = N'Əvvəlki Ay Kompensasiyası')
                            INSERT INTO MaasNovleri (Id, Ad, Tip, Aktivdir, Silinib, YaradilmaTarixi)
                            VALUES (20, N'Əvvəlki Ay Kompensasiyası', 1, 1, 0, GETDATE());
                        SET IDENTITY_INSERT MaasNovleri OFF;
                    ");
                }
                catch { }

                // IsParametrleri — NaharBaslamaSaati / NaharMuddetDeqiqe sütunları
                // (migration-da cədvəl adı səhv idi, bu blok birbaşa düzəldir)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                                       WHERE TABLE_NAME = 'IsParametrleri' AND COLUMN_NAME = 'NaharBaslamaSaati')
                            ALTER TABLE IsParametrleri ADD NaharBaslamaSaati TIME NOT NULL DEFAULT '13:00:00';

                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                                       WHERE TABLE_NAME = 'IsParametrleri' AND COLUMN_NAME = 'NaharMuddetDeqiqe')
                            ALTER TABLE IsParametrleri ADD NaharMuddetDeqiqe INT NOT NULL DEFAULT 45;
                    ");
                }
                catch { /* artıq mövcuddur */ }

                // Teklifler cədvəlini yarat (migration ilə eyni SQL, startup guard kimi)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Teklifler')
BEGIN
    CREATE TABLE Teklifler (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IsciId INT NOT NULL,
        Nov INT NOT NULL DEFAULT 0,
        Prioritet INT NOT NULL DEFAULT 1,
        Status INT NOT NULL DEFAULT 0,
        Bashliq NVARCHAR(200) NOT NULL,
        Mezmun NVARCHAR(MAX) NOT NULL,
        Cavab NVARCHAR(MAX) NULL,
        CavabVerenIsciId INT NULL,
        CavabTarixi DATETIME2 NULL,
        YaradilmaTarixi DATETIME2 NOT NULL DEFAULT GETDATE(),
        YaradanIcraciId INT NULL,
        YenileyenIcraciId INT NULL,
        SilenIcraciId INT NULL,
        YenilenmeTarixi DATETIME2 NULL,
        Silinib BIT NOT NULL DEFAULT 0,
        SilinmeTarixi DATETIME2 NULL,
        CONSTRAINT FK_Teklifler_Isciler_IsciId FOREIGN KEY (IsciId) REFERENCES Isciler(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Teklifler_Isciler_CavabVeren FOREIGN KEY (CavabVerenIsciId) REFERENCES Isciler(Id)
    );
    CREATE INDEX IX_Teklifler_IsciId ON Teklifler(IsciId);
    CREATE INDEX IX_Teklifler_Status ON Teklifler(Status);
END
");
                }
                catch { }

                // FealiyyetJurnali.Acıqlama sütununu NVARCHAR(MAX)-a genişləndir
                try
                {
                    db.Database.ExecuteSqlRaw(
                        "ALTER TABLE [FealiyyetJurnali] ALTER COLUMN [Acıqlama] NVARCHAR(MAX) NULL;");
                }
                catch { /* artıq MAX-dır */ }

                // ── Gələn Mail cədvəlləri ──────────────────────────────────────────
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GelenMailler')
                        BEGIN
                            CREATE TABLE [GelenMailler] (
                                [Id]                    INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                                [MessageId]             NVARCHAR(500)   NOT NULL DEFAULT '',
                                [KimdenAd]              NVARCHAR(200)   NOT NULL DEFAULT '',
                                [KimdenEmail]           NVARCHAR(320)   NOT NULL DEFAULT '',
                                [Movzu]                 NVARCHAR(1000)  NOT NULL DEFAULT '',
                                [MetinHtml]             NVARCHAR(MAX)   NOT NULL DEFAULT '',
                                [MetinDuz]              NVARCHAR(MAX)   NOT NULL DEFAULT '',
                                [AlinmaTarixi]          DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
                                [Oxunub]                BIT             NOT NULL DEFAULT 0,
                                [OxunmaTarixi]          DATETIME2       NULL,
                                [AIXulase]              NVARCHAR(MAX)   NULL,
                                [AITahlilTarixi]        DATETIME2       NULL,
                                [CavabVerildi]          BIT             NOT NULL DEFAULT 0,
                                [SenedId]               INT             NULL,
                                [TapalanIsciId]         INT             NULL,
                                [TapalanIsciTarafindan] INT             NULL,
                                [TapalanTarix]          DATETIME2       NULL,
                                [TapalanQeyd]           NVARCHAR(1000)  NULL,
                                [YaradilmaTarixi]       DATETIME2       NOT NULL DEFAULT GETDATE(),
                                [YenilenmeTarixi]       DATETIME2       NULL,
                                [SilinmeTarixi]         DATETIME2       NULL,
                                [YaradanIcraciId]       INT             NULL,
                                [YenileyenIcraciId]     INT             NULL,
                                [SilenIcraciId]         INT             NULL,
                                [Silinib]               BIT             NOT NULL DEFAULT 0
                            );
                            CREATE UNIQUE INDEX [IX_GelenMailler_MessageId] ON [GelenMailler] ([MessageId]);
                            CREATE INDEX [IX_GelenMailler_AlinmaTarixi]     ON [GelenMailler] ([AlinmaTarixi] DESC);
                            CREATE INDEX [IX_GelenMailler_Oxunub]           ON [GelenMailler] ([Oxunub]);
                        END
                    ");
                }
                catch { }

                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GelenMailQosmalar')
                        BEGIN
                            CREATE TABLE [GelenMailQosmalar] (
                                [Id]                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                                [GelenMailId]       INT             NOT NULL,
                                [FaylAdi]           NVARCHAR(500)   NOT NULL DEFAULT '',
                                [ContentType]       NVARCHAR(200)   NOT NULL DEFAULT '',
                                [OlcuBayt]          BIGINT          NOT NULL DEFAULT 0,
                                [FaylYolu]          NVARCHAR(1000)  NOT NULL DEFAULT '',
                                [CixarilmisMetin]   NVARCHAR(MAX)   NULL,
                                [YaradilmaTarixi]   DATETIME2       NOT NULL DEFAULT GETDATE(),
                                [YenilenmeTarixi]   DATETIME2       NULL,
                                [SilinmeTarixi]     DATETIME2       NULL,
                                [YaradanIcraciId]   INT             NULL,
                                [YenileyenIcraciId] INT             NULL,
                                [SilenIcraciId]     INT             NULL,
                                [Silinib]           BIT             NOT NULL DEFAULT 0,
                                CONSTRAINT [FK_GelenMailQosmalar_GelenMailler]
                                    FOREIGN KEY ([GelenMailId]) REFERENCES [GelenMailler]([Id]) ON DELETE CASCADE
                            );
                            CREATE INDEX [IX_GelenMailQosmalar_GelenMailId] ON [GelenMailQosmalar] ([GelenMailId]);
                        END
                    ");
                }
                catch { }

                // GelenMailler — yeni sütunlar (deadline + xatirlatma flag)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GelenMailler')
                        BEGIN
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailler' AND COLUMN_NAME='DedlaynTarix')
                                ALTER TABLE [GelenMailler] ADD [DedlaynTarix] DATETIME2 NULL;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailler' AND COLUMN_NAME='DedlaynNov')
                                ALTER TABLE [GelenMailler] ADD [DedlaynNov] NVARCHAR(50) NULL;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailler' AND COLUMN_NAME='DedlaynQeyd')
                                ALTER TABLE [GelenMailler] ADD [DedlaynQeyd] NVARCHAR(500) NULL;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailler' AND COLUMN_NAME='DedlaynXatirlatmaYaradildi')
                                ALTER TABLE [GelenMailler] ADD [DedlaynXatirlatmaYaradildi] BIT NOT NULL DEFAULT 0;
                        END
                    ");
                }
                catch { }

                // GelenMailIsciler cədvəli (çoxlu işçiyə tapşırma)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GelenMailIsciler')
                        BEGIN
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailIsciler' AND COLUMN_NAME='YaradanIcraciId')
                                ALTER TABLE [GelenMailIsciler] ADD [YaradanIcraciId] INT NULL;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailIsciler' AND COLUMN_NAME='YenileyenIcraciId')
                                ALTER TABLE [GelenMailIsciler] ADD [YenileyenIcraciId] INT NULL;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailIsciler' AND COLUMN_NAME='SilenIcraciId')
                                ALTER TABLE [GelenMailIsciler] ADD [SilenIcraciId] INT NULL;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailIsciler' AND COLUMN_NAME='YenilenmeTarixi')
                                ALTER TABLE [GelenMailIsciler] ADD [YenilenmeTarixi] DATETIME2 NULL;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailIsciler' AND COLUMN_NAME='SilinmeTarixi')
                                ALTER TABLE [GelenMailIsciler] ADD [SilinmeTarixi] DATETIME2 NULL;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailIsciler' AND COLUMN_NAME='IcraOlundu')
                                ALTER TABLE [GelenMailIsciler] ADD [IcraOlundu] BIT NOT NULL DEFAULT 0;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailIsciler' AND COLUMN_NAME='IcraOlunduTarix')
                                ALTER TABLE [GelenMailIsciler] ADD [IcraOlunduTarix] DATETIME2 NULL;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailIsciler' AND COLUMN_NAME='ImtinaEtdi')
                                ALTER TABLE [GelenMailIsciler] ADD [ImtinaEtdi] BIT NOT NULL DEFAULT 0;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailIsciler' AND COLUMN_NAME='ImtinaSebebi')
                                ALTER TABLE [GelenMailIsciler] ADD [ImtinaSebebi] NVARCHAR(500) NULL;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailIsciler' AND COLUMN_NAME='IsciOxudu')
                                ALTER TABLE [GelenMailIsciler] ADD [IsciOxudu] BIT NOT NULL DEFAULT 0;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='GelenMailIsciler' AND COLUMN_NAME='IsciOxuduTarix')
                                ALTER TABLE [GelenMailIsciler] ADD [IsciOxuduTarix] DATETIME2 NULL;
                        END

                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GelenMailIsciler')
                        BEGIN
                            CREATE TABLE [GelenMailIsciler] (
                                [Id]                      INT IDENTITY(1,1) PRIMARY KEY,
                                [GelenMailId]             INT NOT NULL,
                                [IsciId]                  INT NOT NULL,
                                [TapalanIsciTarafindan]   INT NOT NULL DEFAULT 0,
                                [TapalanTarix]            DATETIME2 NOT NULL,
                                [Qeyd]                    NVARCHAR(1000) NULL,
                                [YaradilmaTarixi]         DATETIME2 NOT NULL DEFAULT GETDATE(),
                                [YaradanIcraciId]         INT NULL,
                                [YenileyenIcraciId]       INT NULL,
                                [SilenIcraciId]           INT NULL,
                                [YenilenmeTarixi]         DATETIME2 NULL,
                                [SilinmeTarixi]           DATETIME2 NULL,
                                [IcraOlundu]              BIT NOT NULL DEFAULT 0,
                                [IcraOlunduTarix]         DATETIME2 NULL,
                                [ImtinaEtdi]              BIT NOT NULL DEFAULT 0,
                                [ImtinaSebebi]            NVARCHAR(500) NULL,
                                [IsciOxudu]               BIT NOT NULL DEFAULT 0,
                                [IsciOxuduTarix]          DATETIME2 NULL,
                                [Silinib]                 BIT NOT NULL DEFAULT 0,
                                CONSTRAINT [FK_GelenMailIsciler_Mail]  FOREIGN KEY ([GelenMailId]) REFERENCES [GelenMailler]([Id]) ON DELETE CASCADE,
                                CONSTRAINT [FK_GelenMailIsciler_Isci]  FOREIGN KEY ([IsciId])      REFERENCES [Isciler]([Id])
                            );
                            CREATE INDEX [IX_GelenMailIsciler_MailId] ON [GelenMailIsciler] ([GelenMailId]);
                            CREATE INDEX [IX_GelenMailIsciler_IsciId] ON [GelenMailIsciler] ([IsciId]);
                        END
                    ");
                }
                catch { }

                // TapalanIsci FK (ayrıca — GelenMailler cədvəli artıq mövcuddursa əlavə et)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GelenMailler')
                        AND NOT EXISTS (
                            SELECT 1 FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
                            WHERE CONSTRAINT_NAME = 'FK_GelenMailler_Isciler_TapalanIsciId'
                        )
                        BEGIN
                            ALTER TABLE [GelenMailler]
                                ADD CONSTRAINT [FK_GelenMailler_Isciler_TapalanIsciId]
                                FOREIGN KEY ([TapalanIsciId]) REFERENCES [Isciler]([Id]) ON DELETE SET NULL;
                        END
                    ");
                }
                catch { }

                // AppUser — MailSmtpEmail, MailSmtpParol sütunları
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AspNetUsers' AND COLUMN_NAME='MailSmtpHost')
                            ALTER TABLE [AspNetUsers] ADD [MailSmtpHost] NVARCHAR(256) NULL;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AspNetUsers' AND COLUMN_NAME='MailSmtpEmail')
                            ALTER TABLE [AspNetUsers] ADD [MailSmtpEmail] NVARCHAR(256) NULL;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AspNetUsers' AND COLUMN_NAME='MailSmtpParol')
                            ALTER TABLE [AspNetUsers] ADD [MailSmtpParol] NVARCHAR(MAX) NULL;
                    ");
                }
                catch { }

                // DovriOdenisler cədvəli
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DovriOdenisler')
                        BEGIN
                            CREATE TABLE [DovriOdenisler] (
                                [Id]                    INT            NOT NULL IDENTITY(1,1),
                                [Ad]                    NVARCHAR(200)  NOT NULL DEFAULT '',
                                [KateqoriyaId]          INT            NOT NULL,
                                [DepartamentId]         INT            NOT NULL,
                                [Mebleg]                DECIMAL(18,2)  NOT NULL DEFAULT 0,
                                [Dovruluq]              INT            NOT NULL DEFAULT 2,
                                [BaslamaTarixi]         DATETIME2      NOT NULL,
                                [BitmeTarixi]           DATETIME2      NULL,
                                [NovbatiOdenisTarixi]   DATETIME2      NOT NULL,
                                [Aktiv]                 BIT            NOT NULL DEFAULT 1,
                                [Qeyd]                  NVARCHAR(500)  NULL,
                                [YaradilmaTarixi]       DATETIME2      NOT NULL DEFAULT GETDATE(),
                                [YaradanIcraciId]       INT            NULL,
                                [YenileyenIcraciId]     INT            NULL,
                                [SilenIcraciId]         INT            NULL,
                                [YenilenmeTarixi]       DATETIME2      NULL,
                                [Silinib]               BIT            NOT NULL DEFAULT 0,
                                [SilinmeTarixi]         DATETIME2      NULL,
                                CONSTRAINT [PK_DovriOdenisler] PRIMARY KEY ([Id]),
                                CONSTRAINT [FK_DovriOdenisler_XercKateqoriyalari_KateqoriyaId]
                                    FOREIGN KEY ([KateqoriyaId]) REFERENCES [XercKateqoriyalari]([Id]) ON DELETE NO ACTION,
                                CONSTRAINT [FK_DovriOdenisler_Departments_DepartamentId]
                                    FOREIGN KEY ([DepartamentId]) REFERENCES [Departament]([Id]) ON DELETE NO ACTION
                            );
                        END
                    ");
                }
                catch { }

                // GozlenilenXercler cədvəli
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GozlenilenXercler')
                        BEGIN
                            CREATE TABLE [GozlenilenXercler] (
                                [Id]                INT            NOT NULL IDENTITY(1,1),
                                [Ad]                NVARCHAR(200)  NOT NULL DEFAULT '',
                                [Tesvir]            NVARCHAR(1000) NULL,
                                [KateqoriyaId]      INT            NOT NULL,
                                [DepartamentId]     INT            NULL,
                                [TahminiMebleg]     DECIMAL(18,2)  NOT NULL DEFAULT 0,
                                [GozlenilenTarix]   DATETIME2      NOT NULL,
                                [Prioritet]         INT            NOT NULL DEFAULT 2,
                                [Status]            INT            NOT NULL DEFAULT 0,
                                [XercId]            INT            NULL,
                                [Qeyd]              NVARCHAR(500)  NULL,
                                [YaradilmaTarixi]   DATETIME2      NOT NULL DEFAULT GETDATE(),
                                [YaradanIcraciId]   INT            NULL,
                                [YenileyenIcraciId] INT            NULL,
                                [SilenIcraciId]     INT            NULL,
                                [YenilenmeTarixi]   DATETIME2      NULL,
                                [Silinib]           BIT            NOT NULL DEFAULT 0,
                                [SilinmeTarixi]     DATETIME2      NULL,
                                CONSTRAINT [PK_GozlenilenXercler] PRIMARY KEY ([Id]),
                                CONSTRAINT [FK_GozlenilenXercler_XercKateqoriyalari_KateqoriyaId]
                                    FOREIGN KEY ([KateqoriyaId]) REFERENCES [XercKateqoriyalari]([Id]) ON DELETE NO ACTION,
                                CONSTRAINT [FK_GozlenilenXercler_Departments_DepartamentId]
                                    FOREIGN KEY ([DepartamentId]) REFERENCES [Departament]([Id]) ON DELETE NO ACTION,
                                CONSTRAINT [FK_GozlenilenXercler_Xercler_XercId]
                                    FOREIGN KEY ([XercId]) REFERENCES [Xercler]([Id]) ON DELETE NO ACTION
                            );
                        END
                    ");
                }
                catch { }

                // SistemAyarlari cədvəli
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SistemAyarlari')
                        BEGIN
                            CREATE TABLE [SistemAyarlari] (
                                [Id]                 INT           NOT NULL IDENTITY(1,1),
                                [KreditImapServer]   NVARCHAR(200) NOT NULL DEFAULT 'imap.titan.email',
                                [KreditImapPort]     INT           NOT NULL DEFAULT 993,
                                [KreditImapEmail]    NVARCHAR(200) NOT NULL DEFAULT '',
                                [KreditImapPassword] NVARCHAR(500) NOT NULL DEFAULT '',
                                CONSTRAINT [PK_SistemAyarlari] PRIMARY KEY ([Id])
                            );
                            INSERT INTO [SistemAyarlari] ([KreditImapServer],[KreditImapPort],[KreditImapEmail],[KreditImapPassword])
                            VALUES ('imap.titan.email', 993, '', '');
                        END
                    ");
                }
                catch { }

                // OracleSorgular cədvəli
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OracleSorgular')
                        BEGIN
                            CREATE TABLE [OracleSorgular] (
                                [Id]                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                                [SorguAdi]          NVARCHAR(200)   NOT NULL DEFAULT '',
                                [Mahiyyet]          NVARCHAR(500)   NULL,
                                [SorguMetni]        NVARCHAR(MAX)   NOT NULL DEFAULT '',
                                [Aktiv]             BIT             NOT NULL DEFAULT 1,
                                [DepartamentId]     INT             NOT NULL,
                                [YaradilmaTarixi]   DATETIME2       NOT NULL DEFAULT GETDATE(),
                                [YenilenmeTarixi]   DATETIME2       NULL,
                                [SilinmeTarixi]     DATETIME2       NULL,
                                [YaradanIcraciId]   INT             NULL,
                                [YenileyenIcraciId] INT             NULL,
                                [SilenIcraciId]     INT             NULL,
                                [Silinib]           BIT             NOT NULL DEFAULT 0,
                                CONSTRAINT [FK_OracleSorgular_Departament]
                                    FOREIGN KEY ([DepartamentId]) REFERENCES [Departament] ([Id])
                            );
                            CREATE INDEX [IX_OracleSorgular_DepartamentId] ON [OracleSorgular] ([DepartamentId]);
                            CREATE INDEX [IX_OracleSorgular_Aktiv]         ON [OracleSorgular] ([Aktiv]);
                        END

                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                                       WHERE TABLE_NAME = 'SistemAyarlari'
                                         AND COLUMN_NAME = 'PidTopluSmsOracleSorguId')
                        BEGIN
                            ALTER TABLE [SistemAyarlari] ADD [PidTopluSmsOracleSorguId] INT NULL;
                        END

                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PidSmsSablonlar')
                        BEGIN
                            CREATE TABLE [PidSmsSablonlar] (
                                [Id]                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                                [Ad]                NVARCHAR(200)   NOT NULL DEFAULT '',
                                [Metn]              NVARCHAR(1000)  NOT NULL DEFAULT '',
                                [Aciqlama]          NVARCHAR(500)   NULL,
                                [Aktiv]             BIT             NOT NULL DEFAULT 1,
                                [YaradilmaTarixi]   DATETIME2       NOT NULL DEFAULT GETDATE(),
                                [YenilenmeTarixi]   DATETIME2       NULL,
                                [SilinmeTarixi]     DATETIME2       NULL,
                                [YaradanIcraciId]   INT             NULL,
                                [YenileyenIcraciId] INT             NULL,
                                [SilenIcraciId]     INT             NULL,
                                [Silinib]           BIT             NOT NULL DEFAULT 0
                            );
                        END

                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PidSmsLoglar')
                        BEGIN
                            CREATE TABLE [PidSmsLoglar] (
                                [Id]                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
                                [Telefon]           NVARCHAR(20)    NOT NULL DEFAULT '',
                                [Metn]              NVARCHAR(1000)  NOT NULL DEFAULT '',
                                [SablonId]          INT             NULL,
                                [Status]            INT             NOT NULL DEFAULT 0,
                                [GonderilmeTarixi]  DATETIME2       NULL,
                                [GoyercinId]        NVARCHAR(100)   NULL,
                                [GatewayCavabi]     NVARCHAR(2000)  NULL,
                                [Xeta]              NVARCHAR(1000)  NULL,
                                [GonderenIsciId]    INT             NOT NULL,
                                [YaradilmaTarixi]   DATETIME2       NOT NULL DEFAULT GETDATE(),
                                [YenilenmeTarixi]   DATETIME2       NULL,
                                [SilinmeTarixi]     DATETIME2       NULL,
                                [YaradanIcraciId]   INT             NULL,
                                [YenileyenIcraciId] INT             NULL,
                                [SilenIcraciId]     INT             NULL,
                                [Silinib]           BIT             NOT NULL DEFAULT 0,
                                CONSTRAINT [FK_PidSmsLoglar_PidSmsSablonlar_SablonId]
                                    FOREIGN KEY ([SablonId]) REFERENCES [PidSmsSablonlar] ([Id]) ON DELETE SET NULL,
                                CONSTRAINT [FK_PidSmsLoglar_Isciler_GonderenIsciId]
                                    FOREIGN KEY ([GonderenIsciId]) REFERENCES [Isciler] ([Id]) ON DELETE NO ACTION
                            );
                        END
                    ");
                }
                catch { }

                // HRDaxiliQaydalar cədvəli
                try
                {
                    db.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HRDaxiliQaydalar')
BEGIN
    CREATE TABLE HRDaxiliQaydalar (
        Id               INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Kod              NVARCHAR(20)  NOT NULL DEFAULT '',
        Ad               NVARCHAR(300) NOT NULL,
        Mezmun           NVARCHAR(MAX) NOT NULL,
        Kateqoriya       NVARCHAR(100) NOT NULL DEFAULT '',
        Versiya          INT           NOT NULL DEFAULT 1,
        Aktiv            BIT           NOT NULL DEFAULT 1,
        SenedAd          NVARCHAR(500) NULL,
        SenedYolu        NVARCHAR(500) NULL,
        YazilanKimId     INT           NOT NULL,
        YaradilmaTarixi  DATETIME2     NOT NULL DEFAULT GETDATE(),
        YenilenmeTarixi  DATETIME2     NULL,
        Silinib          BIT           NOT NULL DEFAULT 0,
        SilinmeTarixi    DATETIME2     NULL,
        YaradanIcraciId  INT           NULL,
        YenileyenIcraciId INT          NULL,
        SilenIcraciId    INT           NULL,
        CONSTRAINT FK_HRDaxiliQaydalar_AppUser FOREIGN KEY (YazilanKimId) REFERENCES AspNetUsers(Id)
    );
    CREATE INDEX IX_HRDaxiliQaydalar_Kod    ON HRDaxiliQaydalar(Kod);
    CREATE INDEX IX_HRDaxiliQaydalar_Aktiv  ON HRDaxiliQaydalar(Aktiv);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='HRDaxiliQaydalar' AND COLUMN_NAME='SenedAd')
        ALTER TABLE HRDaxiliQaydalar ADD SenedAd NVARCHAR(500) NULL;
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='HRDaxiliQaydalar' AND COLUMN_NAME='SenedYolu')
        ALTER TABLE HRDaxiliQaydalar ADD SenedYolu NVARCHAR(500) NULL;
END
                    ");
                }
                catch { }

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
