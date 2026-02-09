using System.Net;
using System.Text.Json;

namespace FinNex.UI.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                // 🔍 API yoxsa UI?
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "Sistemdə xəta baş verdi. Zəhmət olmasa administratora müraciət edin."
                    });
                }
                else
                {
                    // MVC üçün
                    context.Response.Redirect("/Home/Error");
                }
            }
        }
    }

}
