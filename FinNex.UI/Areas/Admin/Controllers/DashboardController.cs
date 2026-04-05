using FinNex.Domain;
using FinNex.UI.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class DashboardController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;

    public DashboardController(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var roles = await _roleManager.Roles.ToListAsync();

        var recentUsers = users
            .OrderByDescending(u => u.QeydiyyatTarixi)
            .Take(5)
            .ToList();

        var recentUserVMs = new List<RecentUserVM>();
        foreach (var user in recentUsers)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            recentUserVMs.Add(new RecentUserVM
            {
                Id = user.Id,
                FullName = $"{user.Ad} {user.Soyad}",
                Email = user.Email ?? "",
                Role = userRoles.FirstOrDefault() ?? "No Role",
                RegisteredAt = user.QeydiyyatTarixi,
                IsActive = user.Aktivdir
            });
        }

        var vm = new DashboardVM
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.Aktivdir),
            InactiveUsers = users.Count(u => !u.Aktivdir),
            TotalRoles = roles.Count,
            RecentUsers = recentUserVMs
        };

        ViewData["Title"] = "Dashboard";
        return View(vm);
    }
}
