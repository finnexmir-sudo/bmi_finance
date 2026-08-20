using System.Security.Claims;
using FinNex.Application.Interfaces;
using FinNex.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinNex.UI.Areas.Avtopark;

/// <summary>
/// Avtopark idarəetmə səhifələri (Maşınlar, Müddətlər) üçün giriş yoxlaması.
///
/// **Admin həmişə girir.** Ondan başqa — Admin panel → Permissions /
/// UserPermissions bölməsindən <see cref="Kod"/> icazəsi verilmiş istifadəçi.
///
/// NİYƏ ROL DEYİL: əvvəl bu iki səhifə `[Authorize(Roles = Admin)]` idi. Yəni
/// təsərrüfat işçisinə maşın kartını açmaq üçün TAM ADMİN vermək lazım gəlirdi —
/// o isə maaşdan tutmuş sistem ayarlarına qədər hər şeyi açır. İndi ayrıca
/// icazə var: yalnız Avtopark idarəetməsi açılır, qalan səlahiyyətlər dəyişmir.
///
/// Eyni yanaşma layihədə artıq işlənir: Mühasibat paneli
/// `muhasibat_dashboard_bax` icazəsi ilə paylaşılır.
///
/// ⚠️ Sidebar-dakı şərt (`_UserLayout.cshtml` → `hasAvtoparkIdare`) BUNUNLA
/// EYNİ olmalıdır — linki görüb sonra 403 almaq istifadəçini çaşdırır.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AvtoparkIdareIcazesiAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>İcazə kodu — `Permissions` cədvəlindəki `Kod` sütunu ilə eynidir.</summary>
    public const string Kod = "avtopark_idare";

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
