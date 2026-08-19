using System.Security.Claims;
using FinNex.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Avtopark.Controllers
{
    /// <summary>
    /// Avtopark controller-lərinin ortaq bazası.
    ///
    /// İki fərqli identifikator var və qarışdırılmamalıdır:
    ///   <see cref="GetUserId"/>       — Identity `AppUser.Id` (audit sahələri:
    ///                                    YaradanIcraciId, YenileyenIcraciId…)
    ///   <see cref="GetIsciIdAsync"/>  — HR `Isci.Id` (biznes sahələri: müraciətçi,
    ///                                    təsdiqləyən rəhbər, açarı verən kassa)
    ///
    /// İkisi FƏRQLİ cədvəllərdir; birini o birinin yerinə yazsaq jurnalda başqa
    /// adamın adı görünər və heç bir xəta çıxmaz.
    /// </summary>
    public abstract class AvtoparkControllerBase : Controller
    {
        protected readonly UserManager<AppUser> UserManager;

        protected AvtoparkControllerBase(UserManager<AppUser> userManager)
            => UserManager = userManager;

        /// <summary>Identity istifadəçi Id-si — audit sahələri üçün.</summary>
        protected int GetUserId()
            => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        /// <summary>
        /// Giriş etmiş istifadəçinin HR işçi Id-si.
        /// `null` — istifadəçiyə işçi profili bağlanmayıb (məs. `admin` hesabı).
        /// </summary>
        protected async Task<int?> GetIsciIdAsync()
        {
            var u = await UserManager.GetUserAsync(User);
            return u?.IsciId;
        }

        /// <summary>Rəhbər addımı ilə bağlı marşrutu həll edən yeganə bayraq.</summary>
        protected bool Rehberdirmi() => User.IsInRole(RoleNames.Rehber);

        /// <summary>«İşçi profili yoxdur» halında ortaq cavab.</summary>
        protected IActionResult IsciProfiliYoxdur(string action = "Index")
        {
            TempData["Error"] = "Hesabınıza işçi profili bağlanmayıb — Admin ilə əlaqə saxlayın.";
            return RedirectToAction(action);
        }
    }
}
