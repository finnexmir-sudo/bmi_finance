
namespace FinNex.UI.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;

            // 1️⃣ Clickjacking qorunması
            headers["X-Frame-Options"] = "DENY";

            // 2️⃣ MIME sniffing qorunması
            headers["X-Content-Type-Options"] = "nosniff";

            // 3️⃣ Referrer məlumatının məhdudlaşdırılması
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // 4️⃣ XSS qorunması (legacy browsers üçün)
            headers["X-XSS-Protection"] = "1; mode=block";

            // 5️⃣ Content Security Policy (sadə, təhlükəsiz)
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self'; " +
                "img-src 'self' data:; " +
                "font-src 'self'; " +
                "object-src 'none'; " +
                "frame-ancestors 'none';";

            // 6️⃣ HTTPS məcburiyyəti (browser tərəfində)
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            await _next(context);
        }

    }
}
