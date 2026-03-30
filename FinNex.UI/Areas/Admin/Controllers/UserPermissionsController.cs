// Areas/Admin/Controllers/UserPermissionsController.cs
using FinNex.Application.DTOs.SenedDovriyyesi.Icaze;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain;
using FinNex.UI.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserPermissionsController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IDepartmentService _departmentService;
    private readonly ISenedDovriyyesiIstifadeciIcazesiService _icazeService;

    public UserPermissionsController(
        UserManager<AppUser> userManager,
        IDepartmentService departmentService,
        ISenedDovriyyesiIstifadeciIcazesiService icazeService)
    {
        _userManager = userManager;
        _departmentService = departmentService;
        _icazeService = icazeService;
    }

    // İstifadəçi siyahısı
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Ad)
            .ToListAsync();

        var vms = new List<UserListVM>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            vms.Add(new UserListVM
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

        ViewData["Title"] = "Document Permissions";
        return View(vms);
    }

    // İcazə idarəetmə səhifəsi
    public async Task<IActionResult> Manage(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var departmentsResult = await _departmentService.HamisiniGetirAsync();
        if (!departmentsResult.Success || departmentsResult.Data == null)
        {
            TempData["Error"] = "Şöbələr yüklənmədi.";
            return RedirectToAction(nameof(Index));
        }

        var existingResult = await _icazeService.IstifadeciyeGoreGetirAsync(id);
        var existing = existingResult.Success ? existingResult.Data ?? new List<SenedDovriyyesiIstifadeciIcazesiDto>()
                                              : new List<SenedDovriyyesiIstifadeciIcazesiDto>();

        var rows = departmentsResult.Data.Select(dept =>
        {
            var rec = existing.FirstOrDefault(e => e.SobeId == dept.Id);
            return new DepartmentPermissionRow
            {
                DepartmentId = dept.Id,
                DepartmentName = dept.Ad,
                CurrentPermission = rec?.IcazeNovu,
                ExistingRecordId = rec?.Id ?? 0
            };
        }).ToList();

        var vm = new ManagePermissionsVM
        {
            UserId = id,
            FullName = $"{user.Ad} {user.Soyad}",
            UserName = user.UserName ?? "",
            Rows = rows
        };

        ViewData["Title"] = "Manage Permissions";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SavePermissionsVM model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId.ToString());
        if (user == null) return NotFound();

        // Mövcud icazələri sil
        var existingResult = await _icazeService.IstifadeciyeGoreGetirAsync(model.UserId);
        if (existingResult.Success && existingResult.Data != null)
        {
            foreach (var rec in existingResult.Data)
                await _icazeService.SilAsync(rec.Id);
        }

        // Yeni icazələri yarat (0 = yoxdur, keçirilir)
        foreach (var kv in model.Permissions)
        {
            if (kv.Value == 0) continue;

            await _icazeService.YaratAsync(new SenedDovriyyesiIstifadeciIcazesiCreateDto
            {
                IstifadeciId = model.UserId,
                SobeId = kv.Key,
                IcazeNovu = kv.Value
            });
        }

        TempData["StatusMessage"] = $"'{user.UserName}' üçün icazələr yeniləndi.";
        return RedirectToAction(nameof(Manage), new { id = model.UserId });
    }
}