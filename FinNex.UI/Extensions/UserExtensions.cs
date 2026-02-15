namespace FinNex.UI.Extensions
{
    using System.Security.Claims;

    namespace FinNex.UI.Extensions
    {
        public static class UserExtensions
        {
            public static int GetUserId(this ClaimsPrincipal user)
            {
                var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return string.IsNullOrEmpty(id) ? 0 : int.Parse(id);
            }

            public static string? GetUserName(this ClaimsPrincipal user)
            {
                return user.Identity?.Name;
            }
            // 🔥 Bütün rolları qaytarır
            public static List<string> GetRoles(this ClaimsPrincipal user)
            {
                return user.FindAll(ClaimTypes.Role)
                           .Select(x => x.Value)
                           .ToList();
            }

            // 🔥 Müəyyən rol varmı?
            public static bool IsInRoleCustom(this ClaimsPrincipal user, string role)
            {
                return user.IsInRole(role);
            }
            public static bool IsAdmin(this ClaimsPrincipal user)
            {
                return user.IsInRole("Admin");
            }
        }
    }

}
