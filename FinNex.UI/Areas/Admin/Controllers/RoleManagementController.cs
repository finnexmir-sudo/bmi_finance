using FinNex.Domain;
using FinNex.UI.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class RoleManagementController : Controller
{
    private readonly RoleManager<AppRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;

    public RoleManagementController(
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var roles = await _roleManager.Roles.ToListAsync();

        var roleVMs = new List<RoleListVM>();
        foreach (var role in roles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            roleVMs.Add(new RoleListVM
            {
                Id = role.Id,
                Name = role.Name ?? "",
                UserCount = usersInRole.Count
            });
        }

        ViewData["Title"] = "Role Management";
        return View(roleVMs);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            return NotFound();

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

        var userVMs = usersInRole.Select(u => new UserListVM
        {
            Id = u.Id,
            UserName = u.UserName ?? "",
            FullName = $"{u.Ad} {u.Soyad}",
            Email = u.Email ?? "",
            IsActive = u.Aktivdir,
            RegisteredAt = u.QeydiyyatTarixi
        }).ToList();

        ViewData["Title"] = $"Role: {role.Name}";
        ViewData["RoleName"] = role.Name;
        ViewData["RoleId"] = role.Id;
        return View(userVMs);
    }
}
