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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
    })
    .AddJwtBearer(options =>
    {
        var cfg = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = cfg["Issuer"],
            ValidAudience = cfg["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(cfg["Key"]!))
        };

        // SignalR WebSocket sorğularında token query-string-dən götürülür
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/notificationHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

            // ==================================================
            // 3. Authorization
            // ==================================================
            builder.Services.AddAuthorization();

            // ==================================================
            // 4. MVC / Razor Pages
            // ==================================================
            builder.Services.AddControllersWithViews();

            // ==================================================
            // 5. Rate Limiting
            // ==================================================
            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("LoginPolicy", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });
            });

            // ==================================================
            // 5.1 Logging (Serilog)
            // ==================================================
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14)
                .CreateLogger();

            builder.Host.UseSerilog();

            // ==================================================
            // 5.5 SignalR
            // ==================================================
            builder.Services.AddSignalR(options =>
            {
                // VM resurs məhdudiyyəti nəzərə alınaraq timeout artırıldı.
                // Default ClientTimeoutInterval = 30s, KeepAliveInterval = 15s
                // VM yük altında 30s-də cavab verə bilmir → bağlantı kəsilir.
                options.ClientTimeoutInterval  = TimeSpan.FromSeconds(120); // 30s → 120s
                options.KeepAliveInterval      = TimeSpan.FromSeconds(30);  // 15s → 30s
            });

            // ==================================================
            // 5.6 Gələn Mail / AI Servisləri
            // ==================================================
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
            builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("Gemini"));
            builder.Services.AddHttpClient();

            // ==================================================
            // 6. Swagger (yalnız Development mühitdə)
            // ==================================================
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // ==================================================
            // 7. FluentValidation
            // ==================================================
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            // ==================================================
            // 8. Build + Middleware pipeline
            // ==================================================
            var app = builder.Build();

            // Seed
            using (var scope = app.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                await seeder.SeedAsync();
            }

            app.UseRateLimiter();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<AuditMiddleware>();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapHub<FinNex.UI.Hubs.NotificationHub>("/notificationHub")
               .RequireAuthorization();

            app.MapControllers();

            await app.Services.GetRequiredService<IServiceScopeFactory>()
                .CreateScope()
                .ServiceProvider
                .GetRequiredService<FinNex.DataAccess.Contexts.AppDbContext>()
                .Database
                .MigrateAsync();

            app.Run();
        }
    }
}