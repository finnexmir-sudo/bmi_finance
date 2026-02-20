using Microsoft.AspNetCore.Identity;

namespace FinNex.Domain
{
    public class AppRole : IdentityRole<int>
    {
        // Bura əlavə nəsə yazmağa ehtiyac yoxdur, 
        // amma gələcəkdə rolun təsviri kimi sütunlar lazım olsa bura əlavə edəcəyik.
    }
}
