using FinNex.DataAccess;
using FinNex.DataAccess.Seed;
using FinNex.UI.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Threading.RateLimiting;

namespace FinNex.UI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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
            builder.Services.AddDataAccessServices(builder.Configuration);

            // ==================================================
            // 2. Authentication (Cookie-based)
            // ==================================================
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                });

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



            var app = builder.Build();

            // ==================================================
            // 6. Middleware pipeline (ORDER IS CRITICAL)
            // ==================================================

            // 🔥 Global exception handler (ən yuxarı)
            //app.UseMiddleware<GlobalExceptionMiddleware>();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            //mehemmed gormesi ucun bunu kommente alarsa

            app.UseHttpsRedirection();

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
