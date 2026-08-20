using System.Security.Claims;
using FinNex.Application.Interfaces;
using FinNex.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinNex.UI.Filters;

/// <summary>
/// Səhifəni **Sistem İcazələri**ndəki bir koda bağlayır.
///
/// <code>
/// [Authorize]
/// [Icaze("modul_ehval_bax")]      // ← Admin panel → Sistem İcazələri-dəki KOD
/// public class EhvalController : Controller
/// </code>
///
/// Qayda:
///   · **Admin həmişə girir** — ona icazə verməyə ehtiyac yoxdur.
///   · Digərləri yalnız həmin icazə verilibsə girir.
///   · İcazəsi olmayan `403` alır.
///
/// ⚠️ Buradakı mətn (`kod`) `Permissions.Kod` sütunu ilə **hərfən eyni**
/// olmalıdır. Bir hərf fərq olsa icazə heç kimə işləməz və **heç bir xəta
/// çıxmaz** — hamı səssizcə 403 alar. Ona görə kodu Admin paneldən kopyala.
///
/// ⚠️ Sidebar-dakı «bu linki göstər/gizlət» şərti də eyni icazəyə baxmalıdır
/// (`_UserLayout.cshtml`). Yoxsa istifadəçi ya linki görüb 403 alar, ya da
/// icazəsi olduğu halda linki tapa bilməz.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class IcazeAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>`Permissions.Kod` ilə eyni olan icazə kodu.</summary>
    public string Kod { get; }

    public IcazeAttribute(string kod) => Kod = kod;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        if (user.IsInRole(RoleNames.Admin)) return;

        var uidStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(uidStr, out var uid))
        {
            var perm = context.HttpContext.RequestServices.GetRequiredService<IUserPermissionService>();
            var netice = await perm.HasPermissionAsync(uid, Kod);
            if (netice.Success && netice.Data) return;
        }

        context.Result = new ForbidResult();
    }
}
