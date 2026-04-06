using FinNex.Domain;
using FinNex.UI.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            TempData["Error"] = "Role name cannot be empty.";
            return RedirectToAction(nameof(Index));
        }

        roleName = roleName.Trim();

        if (await _roleManager.RoleExistsAsync(roleName))
        {
            TempData["Error"] = $"Role '{roleName}' already exists.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _roleManager.CreateAsync(new AppRole { Name = roleName });
        if (result.Succeeded)
            TempData["Success"] = $"Role '{roleName}' created successfully.";
        else
            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            TempData["Error"] = "Role name cannot be empty.";
            return RedirectToAction(nameof(Index));
        }

        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            return NotFound();

        roleName = roleName.Trim();

        var existing = await _roleManager.FindByNameAsync(roleName);
        if (existing != null && existing.Id != id)
        {
            TempData["Error"] = $"Role '{roleName}' already exists.";
            return RedirectToAction(nameof(Index));
        }

        role.Name = roleName;
        var result = await _roleManager.UpdateAsync(role);
        if (result.Succeeded)
            TempData["Success"] = $"Role renamed to '{roleName}' successfully.";
        else
            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            return NotFound();

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Any())
        {
            TempData["Error"] = $"Cannot delete role '{role.Name}' because it has {usersInRole.Count} user(s) assigned.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _roleManager.DeleteAsync(role);
        if (result.Succeeded)
            TempData["Success"] = $"Role '{role.Name}' deleted successfully.";
        else
            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));

        return RedirectToAction(nameof(Index));
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

        // Get all users NOT in this role for the "Add User" dropdown
        var allUsers = await _userManager.Users.ToListAsync();
        var usersNotInRole = new List<UserListVM>();
        foreach (var u in allUsers)
        {
            if (!usersInRole.Any(r => r.Id == u.Id))
            {
                usersNotInRole.Add(new UserListVM
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    FullName = $"{u.Ad} {u.Soyad}"
                });
            }
        }

        ViewData["Title"] = $"Role: {role.Name}";
        ViewData["RoleName"] = role.Name;
        ViewData["RoleId"] = role.Id;
        ViewData["AvailableUsers"] = usersNotInRole;
        return View(userVMs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveUserFromRole(int roleId, int userId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
            return NotFound();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return NotFound();

        var result = await _userManager.RemoveFromRoleAsync(user, role.Name!);
        if (result.Succeeded)
            TempData["Success"] = $"User '{user.UserName}' removed from role '{role.Name}'.";
        else
            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));

        return RedirectToAction(nameof(Detail), new { id = roleId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUserToRole(int roleId, int userId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
            return NotFound();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return NotFound();

        if (await _userManager.IsInRoleAsync(user, role.Name!))
        {
            TempData["Error"] = $"User '{user.UserName}' is already in role '{role.Name}'.";
            return RedirectToAction(nameof(Detail), new { id = roleId });
        }

        var result = await _userManager.AddToRoleAsync(user, role.Name!);
        if (result.Succeeded)
            TempData["Success"] = $"User '{user.UserName}' added to role '{role.Name}'.";
        else
            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));

        return RedirectToAction(nameof(Detail), new { id = roleId });
    }
}
