using FinNex.Application.DTOs.Structur;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain;
using FinNex.UI.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class UserManagementController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IUserDepartmentService _userDepartmentService;
    private readonly IDepartmentService _departmentService;


    public UserManagementController(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IUserDepartmentService userDepartmentService,IDepartmentService departmentService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _userDepartmentService = userDepartmentService;
        _departmentService = departmentService;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.QeydiyyatTarixi)
            .ToListAsync();

        var userVMs = new List<UserListVM>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userVMs.Add(new UserListVM
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                FullName = $"{user.Ad} {user.Soyad}",
                Email = user.Email ?? "",
                Roles = roles,
                IsActive = user.Aktivdir,
                RegisteredAt = user.QeydiyyatTarixi,
                IsLockedOut = await _userManager.IsLockedOutAsync(user)
            });
        }

        ViewData["Title"] = "User Management";
        return View(userVMs);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        var departments = await _userDepartmentService
            .GetUserDepartmentsAsync(id);

        var vm = new UserDetailVM
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            FullName = $"{user.Ad} {user.Soyad}",
            Email = user.Email ?? "",
            Roles = roles,
            Departments = departments
        };

        return View(vm);
    }

    public async Task<IActionResult> AssignDepartment(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        var departmentsResult = await _departmentService.HamisiniGetirAsync();

        if (!departmentsResult.Success || departmentsResult.Data == null)
        {
            TempData["Error"] = departmentsResult.Message ?? "Şöbələr yüklənmədi.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new AssignDepartmentVM
        {
            UserId = id,
            Departments = departmentsResult.Data
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Ad
                })
                .ToList()
        };

        return View(vm);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        user.Aktivdir = !user.Aktivdir;
        await _userManager.UpdateAsync(user);

        TempData["StatusMessage"] = $"User '{user.UserName}' has been {(user.Aktivdir ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockUser(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        TempData["StatusMessage"] = $"User '{user.UserName}' has been unlocked.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> ChangeRole(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

        var vm = new ChangeUserRoleVM
        {
            UserId = user.Id,
            UserName = user.UserName ?? "",
            FullName = $"{user.Ad} {user.Soyad}",
            CurrentRoles = currentRoles,
            SelectedRoles = currentRoles.ToList(),
            AvailableRoles = allRoles
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(ChangeUserRoleVM vm)
    {
        var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        vm.AvailableRoles = allRoles;

        var user = await _userManager.FindByIdAsync(vm.UserId.ToString());
        if (user == null)
            return NotFound();

        if (vm.SelectedRoles == null || !vm.SelectedRoles.Any())
        {
            ModelState.AddModelError("SelectedRoles", "Ən azı 1 rol seçilməlidir.");
            vm.CurrentRoles = await _userManager.GetRolesAsync(user);
            return View(vm);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);

        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRolesAsync(user, vm.SelectedRoles);

        TempData["StatusMessage"] = $"'{user.UserName}' üçün rollar yeniləndi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ResetPassword(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        var vm = new ResetPasswordVM
        {
            UserId = user.Id,
            UserName = user.UserName ?? "",
            FullName = $"{user.Ad} {user.Soyad}"
        };

        ViewData["Title"] = "Reset Password";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVM vm)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Reset Password";
            return View(vm);
        }

        var user = await _userManager.FindByIdAsync(vm.UserId.ToString());
        if (user == null)
            return NotFound();

        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            foreach (var error in removeResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            ViewData["Title"] = "Reset Password";
            return View(vm);
        }

        var addResult = await _userManager.AddPasswordAsync(user, vm.NewPassword);
        if (!addResult.Succeeded)
        {
            foreach (var error in addResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            ViewData["Title"] = "Reset Password";
            return View(vm);
        }

        // Clear lockout on password reset
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        TempData["StatusMessage"] = $"Password for '{user.UserName}' has been reset successfully.";
        return RedirectToAction(nameof(Detail), new { id = vm.UserId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignDepartment(AssignDepartmentVM model)
    {
        if (!ModelState.IsValid)
            return View(model);
        

            var dto = new UserDepartmentCreateDto
        {
            UserId = model.UserId,
            DepartmentId = model.DepartmentId!
        };

        await _userDepartmentService.AssignAsync(dto);

        return RedirectToAction("Detail", new { id = model.UserId });
    }



}
