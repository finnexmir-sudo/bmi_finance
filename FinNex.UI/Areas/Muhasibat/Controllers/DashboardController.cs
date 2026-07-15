using System.Globalization;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Muhasibat;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Muhasibat.Controllers;

// Mühasibat / Maliyyə Dashboard.
// Giriş: icazə əsaslı — Admin və Muhasib avtomatik; digər şöbələr (audit, risk...)
// "muhasibat_dashboard_bax" icazəsi ilə (Admin panel → Permissions/UserPermissions).
[Area("Muhasibat")]
[Authorize]
public class DashboardController : Controller
{
    public const string IcazeKod = "muhasibat_dashboard_bax";

    private readonly IMuhasibatService _service;
    private readonly IUserPermissionService _perm;
    private readonly UserManager<AppUser> _userManager;

    public DashboardController(
        IMuhasibatService service,
        IUserPermissionService perm,
        UserManager<AppUser> userManager)
    {
        _service = service;
        _perm = perm;
        _userManager = userManager;
    }

    private async Task<bool> IcazeVarAsync()
    {
        if (User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Muhasib))
            return true;

        var u = await _userManager.GetUserAsync(User);
        if (u == null) return false;

        var res = await _perm.HasPermissionAsync(u.Id, IcazeKod);
        return res.Success && res.Data == true;
    }

    // Balans İcmalı. t = seçilmiş tarix (dd-MM-yyyy / yyyy-MM-dd).
    public async Task<IActionResult> Index(string? t)
    {
        if (!await IcazeVarAsync())
            return Forbid();

        DateTime? tarix = null;
        if (!string.IsNullOrWhiteSpace(t) &&
            DateTime.TryParseExact(t.Trim(),
                new[] { "dd-MM-yyyy", "yyyy-MM-dd", "dd/MM/yyyy" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            tarix = parsed;
        }

        var model = await _service.BalansAsync(tarix);
        return View(model);
    }
}
