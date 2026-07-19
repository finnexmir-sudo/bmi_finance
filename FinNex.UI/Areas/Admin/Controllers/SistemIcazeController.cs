using FinNex.Application.DTOs.Structure.Permission;
using FinNex.Application.DTOs.Structure.UserPermission;
using FinNex.Application.Interfaces;
using FinNex.Domain;
using FinNex.UI.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

// Sistem İcazələri — general Permission/UserPermission idarəetməsi.
// İcazə seç → hansı işçilərin görəcəyini aç/bağla. Yeni icazə də əlavə oluna bilər.
// (Sənəd dövriyyəsi üçün ayrı "UserPermissions" səhifəsi var — bu ondan fərqlidir.)
[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class SistemIcazeController : Controller
{
    // Muhasibat / Maliyyə & Risk Paneli icazəsi — səhifə açılanda mövcudluğu təmin olunur.
    private const string PanelKod = "muhasibat_dashboard_bax";

    private readonly IPermissionService _perm;
    private readonly IUserPermissionService _userPerm;
    private readonly UserManager<AppUser> _userManager;

    public SistemIcazeController(
        IPermissionService perm,
        IUserPermissionService userPerm,
        UserManager<AppUser> userManager)
    {
        _perm = perm;
        _userPerm = userPerm;
        _userManager = userManager;
    }

    // İcazə kataloqu + hər birinin neçə istifadəçidə olduğu.
    public async Task<IActionResult> Index()
    {
        // Panel icazəsi həmişə mövcud olsun (turnkey).
        var panelPerm = await _perm.GetByKodAsync(PanelKod);
        if (panelPerm.Data == null)
            await _perm.YaratAsync(new PermissionCreateDto
            {
                Kod = PanelKod,
                Ad = "Maliyyə & Risk Paneli",
                Aciqlama = "Maliyyə & Risk Panelinə (Mühasibat Dashboard) baxış icazəsi"
            });

        var perms = (await _perm.HamisiniGetirAsync()).Data ?? new List<PermissionDto>();
        var ups = (await _userPerm.HamisiniGetirAsync()).Data ?? new List<UserPermissionDto>();
        var sayMap = ups.Where(x => x.Allowed)
                        .GroupBy(x => x.PermissionId)
                        .ToDictionary(g => g.Key, g => g.Count());

        var vm = new SistemIcazeIndexVM
        {
            Icazeler = perms.Select(p => new PermissionSatirVM
            {
                Id = p.Id,
                Kod = p.Kod,
                Ad = p.Ad,
                Aciqlama = p.Aciqlama,
                UserSayi = sayMap.GetValueOrDefault(p.Id)
            }).OrderBy(x => x.Ad).ToList()
        };

        ViewData["Title"] = "Sistem İcazələri";
        return View(vm);
    }

    // Yeni icazə əlavə et (gələcək sahələr üçün).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(string kod, string ad, string? aciqlama)
    {
        kod = (kod ?? "").Trim();
        ad = (ad ?? "").Trim();
        if (string.IsNullOrEmpty(kod) || string.IsNullOrEmpty(ad))
        {
            TempData["Error"] = "Kod və ad tələb olunur.";
            return RedirectToAction(nameof(Index));
        }

        var exists = await _perm.GetByKodAsync(kod);
        if (exists.Data != null)
        {
            TempData["Error"] = $"'{kod}' kodlu icazə artıq mövcuddur.";
            return RedirectToAction(nameof(Index));
        }

        await _perm.YaratAsync(new PermissionCreateDto { Kod = kod, Ad = ad, Aciqlama = aciqlama });
        TempData["Status"] = $"'{ad}' icazəsi əlavə olundu.";
        return RedirectToAction(nameof(Index));
    }

    // Bir icazəni idarə et — hansı işçilərdə var (checkbox).
    public async Task<IActionResult> Idare(int id)
    {
        var p = (await _perm.IdIleGetirAsync(id)).Data;
        if (p == null) return NotFound();

        var users = await _userManager.Users
            .Where(u => u.Aktivdir)
            .OrderBy(u => u.Ad).ThenBy(u => u.Soyad)
            .ToListAsync();

        var ups = (await _userPerm.HamisiniGetirAsync()).Data ?? new List<UserPermissionDto>();
        var varSet = ups.Where(x => x.PermissionId == id && x.Allowed)
                        .Select(x => x.UserId).ToHashSet();

        var vm = new SistemIcazeIdareVM
        {
            PermissionId = p.Id,
            PermissionKod = p.Kod,
            PermissionAd = p.Ad
        };

        foreach (var u in users)
        {
            var roller = await _userManager.GetRolesAsync(u);
            vm.Users.Add(new IcazeUserVM
            {
                UserId = u.Id,
                AdSoyad = $"{u.Ad} {u.Soyad}".Trim(),
                UserName = u.UserName ?? "",
                Roller = roller,
                Var = varSet.Contains(u.Id)
            });
        }

        ViewData["Title"] = $"İcazə — {p.Ad}";
        return View(vm);
    }

    // Seçilmiş işçilərə icazəni ver / seçilməyənlərdən götür.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Saxla(int permissionId, int[] userIds)
    {
        var p = (await _perm.IdIleGetirAsync(permissionId)).Data;
        if (p == null) return NotFound();

        var ups = (await _userPerm.HamisiniGetirAsync()).Data ?? new List<UserPermissionDto>();
        var current = ups.Where(x => x.PermissionId == permissionId && x.Allowed).ToList();
        var selected = (userIds ?? Array.Empty<int>()).ToHashSet();

        // Yeni seçilənlərə ver.
        foreach (var uid in selected)
            if (!current.Any(x => x.UserId == uid))
                await _userPerm.YaratAsync(new UserPermissionCreateDto
                {
                    UserId = uid,
                    PermissionId = permissionId,
                    Allowed = true
                });

        // Seçimdən çıxarılanlardan götür.
        foreach (var rec in current)
            if (!selected.Contains(rec.UserId))
                await _userPerm.SilAsync(rec.Id);

        TempData["Status"] = $"'{p.Ad}' üçün giriş yeniləndi ({selected.Count} istifadəçi).";
        return RedirectToAction(nameof(Idare), new { id = permissionId });
    }
}
