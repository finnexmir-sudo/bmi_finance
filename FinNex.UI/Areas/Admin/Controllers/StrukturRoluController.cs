using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class StrukturRoluController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;

    public StrukturRoluController(
        IUnitOfWork unitOfWork,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // Struktur rolundan Identity rolu adına map
    private static string? StrukturToIdentityRole(StrukturRolTipi r) => r switch
    {
        StrukturRolTipi.SobeReisi => RoleNames.SobeReisi,
        StrukturRolTipi.Rehber    => RoleNames.Rehber,
        StrukturRolTipi.Hr        => RoleNames.HR,
        StrukturRolTipi.Muhasib   => RoleNames.Muhasib,
        _ => null
    };

    // ── GET /Admin/StrukturRolu ─────────────────────────────
    public async Task<IActionResult> Index(StrukturRolTipi? rolFilter, int? departamentFilter)
    {
        var query = _unitOfWork.Repository<IsciStrukturRolu>()
            .Query()
            .Where(x => !x.Silinib)
            .Include(x => x.Isci)
            .Include(x => x.Departament)
            .AsQueryable();

        if (rolFilter.HasValue)
            query = query.Where(x => x.RolTipi == rolFilter.Value);

        if (departamentFilter.HasValue)
            query = query.Where(x => x.DepartamentId == departamentFilter.Value);

        var list = await query
            .OrderBy(x => x.RolTipi)
            .ThenBy(x => x.Departament!.Ad)
            .ThenBy(x => x.Isci.Soyad)
            .ToListAsync();

        var vm = new StrukturRoluIndexVM
        {
            Items = list.Select(x => new StrukturRoluItemVM
            {
                Id = x.Id,
                IsciId = x.IsciId,
                IsciAdSoyad = x.Isci.TamAd,
                RolTipi = x.RolTipi,
                DepartamentId = x.DepartamentId,
                DepartamentAd = x.Departament?.Ad,
                BaslamaTarixi = x.BaslamaTarixi,
                BitmeTarixi = x.BitmeTarixi,
                Aktivdir = x.Aktivdir
            }).ToList(),
            RolFilter = rolFilter,
            DepartamentFilter = departamentFilter,
            Departamentler = await GetDepartamentSelectListAsync(departamentFilter),
            UmumiSayi = list.Count,
            SobeReisiSayi = list.Count(x => x.RolTipi == StrukturRolTipi.SobeReisi && x.Aktivdir),
            RehberSayi = list.Count(x => x.RolTipi == StrukturRolTipi.Rehber && x.Aktivdir),
            HrSayi = list.Count(x => x.RolTipi == StrukturRolTipi.Hr && x.Aktivdir),
            MuhasibSayi = list.Count(x => x.RolTipi == StrukturRolTipi.Muhasib && x.Aktivdir)
        };

        ViewData["Title"] = "Struktur Rolları";
        return View(vm);
    }

    // ── GET /Admin/StrukturRolu/Create ──────────────────────
    // isciId query param ilə gəlsə, işçi avtomatik seçilir.
    [HttpGet]
    public async Task<IActionResult> Create(int? isciId)
    {
        var vm = new StrukturRoluFormVM
        {
            IsciId = isciId ?? 0,
            BaslamaTarixi = DateTime.Today,
            Aktivdir = true,
            Isciler = await GetIsciSelectListAsync(isciId),
            Departamentler = await GetDepartamentSelectListAsync()
        };

        ViewData["Title"] = "Yeni Struktur Rolu";
        return View(vm);
    }

    // ── POST /Admin/StrukturRolu/Create ─────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StrukturRoluFormVM vm)
    {
        // SobeReisi və Hr şöbə tələb edir; Rehber üçün şöbə ixtiyaridir
        if (vm.RolTipi == StrukturRolTipi.SobeReisi && !vm.DepartamentId.HasValue)
            ModelState.AddModelError(nameof(vm.DepartamentId),
                "Şöbə Rəisi rolu üçün departament seçilməlidir.");

        if (!ModelState.IsValid)
        {
            vm.Isciler = await GetIsciSelectListAsync();
            vm.Departamentler = await GetDepartamentSelectListAsync(vm.DepartamentId);
            return View(vm);
        }

        // Eyni İşçi + Rol + Şöbə + Aktiv kombinasiyası varmı?
        var dublikatVar = await _unitOfWork.Repository<IsciStrukturRolu>()
            .MovcuddurmuAsync(x =>
                !x.Silinib &&
                x.IsciId == vm.IsciId &&
                x.RolTipi == vm.RolTipi &&
                x.DepartamentId == vm.DepartamentId &&
                x.Aktivdir);

        if (dublikatVar)
        {
            TempData["Error"] = "Bu işçi üçün eyni rol və şöbə kombinasiyası artıq mövcuddur.";
            vm.Isciler = await GetIsciSelectListAsync();
            vm.Departamentler = await GetDepartamentSelectListAsync(vm.DepartamentId);
            return View(vm);
        }

        // Eyni şöbədə eyni növ rol birdən artıq aktiv olmamalıdır (SobeReisi/Hr)
        if (vm.RolTipi == StrukturRolTipi.SobeReisi && vm.DepartamentId.HasValue)
        {
            var sobeReisiVar = await _unitOfWork.Repository<IsciStrukturRolu>()
                .MovcuddurmuAsync(x =>
                    !x.Silinib &&
                    x.RolTipi == StrukturRolTipi.SobeReisi &&
                    x.DepartamentId == vm.DepartamentId &&
                    x.Aktivdir);

            if (sobeReisiVar)
            {
                TempData["Error"] = "Bu şöbə üçün artıq aktiv Şöbə Rəisi var. Əvvəlcə onu deaktiv edin.";
                vm.Isciler = await GetIsciSelectListAsync();
                vm.Departamentler = await GetDepartamentSelectListAsync(vm.DepartamentId);
                return View(vm);
            }
        }

        var entity = new IsciStrukturRolu
        {
            IsciId = vm.IsciId,
            RolTipi = vm.RolTipi,
            DepartamentId = vm.DepartamentId,
            BaslamaTarixi = vm.BaslamaTarixi,
            BitmeTarixi = vm.BitmeTarixi,
            Aktivdir = vm.Aktivdir
        };

        await _unitOfWork.Repository<IsciStrukturRolu>().YaratAsync(entity);
        await _unitOfWork.YaddaSaxlaAsync();

        // Auto-sync: Identity rolu əlavə et — struktur rolu artıq yaradılıb,
        // Identity sync uğursuz olsa belə istifadəçiyə success göstərilsin.
        if (vm.Aktivdir)
        {
            try
            {
                await EnsureIdentityRoleAsync(vm.IsciId, vm.RolTipi);
            }
            catch
            {
                TempData["Warning"] = "Struktur rolu yaradıldı, lakin Identity rolu avtomatik təyin edilmədi.";
            }
        }

        TempData["Success"] = "Struktur rolu uğurla əlavə edildi.";
        return RedirectToAction(nameof(Index));
    }

    // ── GET /Admin/StrukturRolu/Edit/5 ──────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _unitOfWork.Repository<IsciStrukturRolu>()
            .GetirAsync(x => x.Id == id, izlemeden: true);

        if (entity == null) return NotFound();

        var vm = new StrukturRoluFormVM
        {
            Id = entity.Id,
            IsciId = entity.IsciId,
            RolTipi = entity.RolTipi,
            DepartamentId = entity.DepartamentId,
            BaslamaTarixi = entity.BaslamaTarixi,
            BitmeTarixi = entity.BitmeTarixi,
            Aktivdir = entity.Aktivdir,
            Isciler = await GetIsciSelectListAsync(entity.IsciId),
            Departamentler = await GetDepartamentSelectListAsync(entity.DepartamentId)
        };

        ViewData["Title"] = "Struktur Rolunu Redaktə et";
        return View(vm);
    }

    // ── POST /Admin/StrukturRolu/Edit ───────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StrukturRoluFormVM vm)
    {
        if (vm.RolTipi == StrukturRolTipi.SobeReisi && !vm.DepartamentId.HasValue)
            ModelState.AddModelError(nameof(vm.DepartamentId),
                "Şöbə Rəisi rolu üçün departament seçilməlidir.");

        if (!ModelState.IsValid)
        {
            vm.Isciler = await GetIsciSelectListAsync(vm.IsciId);
            vm.Departamentler = await GetDepartamentSelectListAsync(vm.DepartamentId);
            return View(vm);
        }

        var entity = await _unitOfWork.Repository<IsciStrukturRolu>()
            .GetirAsync(x => x.Id == vm.Id);

        if (entity == null) return NotFound();

        // Aktivləşdirilirsə və başqa SobeReisi varsa qarşısını al
        if (vm.RolTipi == StrukturRolTipi.SobeReisi && vm.Aktivdir && vm.DepartamentId.HasValue)
        {
            var sobeReisiVar = await _unitOfWork.Repository<IsciStrukturRolu>()
                .MovcuddurmuAsync(x =>
                    !x.Silinib &&
                    x.Id != vm.Id &&
                    x.RolTipi == StrukturRolTipi.SobeReisi &&
                    x.DepartamentId == vm.DepartamentId &&
                    x.Aktivdir);

            if (sobeReisiVar)
            {
                TempData["Error"] = "Bu şöbə üçün başqa aktiv Şöbə Rəisi var. Əvvəlcə onu deaktiv edin.";
                vm.Isciler = await GetIsciSelectListAsync(vm.IsciId);
                vm.Departamentler = await GetDepartamentSelectListAsync(vm.DepartamentId);
                return View(vm);
            }
        }

        // Redaktə edilməmişdən əvvəlki dəyərləri yadda saxla (Identity sync üçün)
        var evvelkiIsciId = entity.IsciId;
        var evvelkiRolTipi = entity.RolTipi;
        var evvelkiAktiv = entity.Aktivdir;

        entity.IsciId = vm.IsciId;
        entity.RolTipi = vm.RolTipi;
        entity.DepartamentId = vm.DepartamentId;
        entity.BaslamaTarixi = vm.BaslamaTarixi;
        entity.BitmeTarixi = vm.BitmeTarixi;
        entity.Aktivdir = vm.Aktivdir;

        await _unitOfWork.Repository<IsciStrukturRolu>().YenileAsync(entity);
        await _unitOfWork.YaddaSaxlaAsync();

        // Identity sync — uğursuz olarsa əsas əməliyyatı bloklanmaz
        try
        {
            // 1. Əvvəl aktiv idi, indi həm işçi/rol dəyişib və ya deaktivdir → köhnə tərəfdə Identity rolunu yenidən hesabla
            if (evvelkiAktiv && (evvelkiIsciId != vm.IsciId || evvelkiRolTipi != vm.RolTipi || !vm.Aktivdir))
                await RemoveIdentityRoleIfUnusedAsync(evvelkiIsciId, evvelkiRolTipi, excludingId: entity.Id);

            // 2. Yeni vəziyyət aktivdirsə, yeni işçi + rol üçün Identity rolunu əlavə et
            if (vm.Aktivdir)
                await EnsureIdentityRoleAsync(vm.IsciId, vm.RolTipi);
        }
        catch
        {
            TempData["Warning"] = "Struktur rolu yeniləndi, lakin Identity rolu avtomatik sinxronizasiya edilmədi.";
        }

        TempData["Success"] = "Struktur rolu yeniləndi.";
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Admin/StrukturRolu/ToggleActive ───────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var entity = await _unitOfWork.Repository<IsciStrukturRolu>()
            .GetirAsync(x => x.Id == id);

        if (entity == null) return NotFound();

        entity.Aktivdir = !entity.Aktivdir;
        await _unitOfWork.Repository<IsciStrukturRolu>().YenileAsync(entity);
        await _unitOfWork.YaddaSaxlaAsync();

        try
        {
            if (entity.Aktivdir)
                await EnsureIdentityRoleAsync(entity.IsciId, entity.RolTipi);
            else
                await RemoveIdentityRoleIfUnusedAsync(entity.IsciId, entity.RolTipi, excludingId: entity.Id);
        }
        catch
        {
            TempData["Warning"] = "Əməliyyat uğurlu oldu, lakin Identity rolu sinxronizasiya edilmədi.";
        }

        TempData["Success"] = entity.Aktivdir ? "Aktiv edildi." : "Deaktiv edildi.";
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Admin/StrukturRolu/Delete ─────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _unitOfWork.Repository<IsciStrukturRolu>()
            .GetirAsync(x => x.Id == id);

        if (entity == null) return NotFound();

        var isciId = entity.IsciId;
        var rolTipi = entity.RolTipi;
        var aktiv = entity.Aktivdir;

        await _unitOfWork.Repository<IsciStrukturRolu>().YumshakSilAsync(id);
        await _unitOfWork.YaddaSaxlaAsync();

        try
        {
            if (aktiv)
                await RemoveIdentityRoleIfUnusedAsync(isciId, rolTipi, excludingId: id);
        }
        catch
        {
            TempData["Warning"] = "Struktur rolu silindi, lakin Identity rolu sinxronizasiya edilmədi.";
        }

        TempData["Success"] = "Struktur rolu silindi.";
        return RedirectToAction(nameof(Index));
    }

    // ════════════════════════════════════════════════════════
    // Identity rolu auto-sync
    // Struktur rolu dəyişəndə işçinin AppUser-inə Identity rolu
    // əlavə/silinir ki, səhifə girişləri də avtomatik uyğunlaşsın.
    // ════════════════════════════════════════════════════════
    private async Task EnsureIdentityRoleAsync(int isciId, StrukturRolTipi rolTipi)
    {
        var identityRole = StrukturToIdentityRole(rolTipi);
        if (identityRole == null) return;

        // Identity rolu AspNetRoles cədvəlində yoxdursa avtomatik yaradılır.
        // Əks halda AddToRoleAsync exception atar və DB commit-dən sonra səhifə
        // "Xəta baş verdi" göstərir (bu vəziyyət real istifadədə baş verirdi).
        if (!await _roleManager.RoleExistsAsync(identityRole))
        {
            var createResult = await _roleManager.CreateAsync(new AppRole { Name = identityRole });
            if (!createResult.Succeeded) return; // qorumaq — rol yaratmaq mümkün olmadı
        }

        var appUser = await _userManager.Users.FirstOrDefaultAsync(u => u.IsciId == isciId);
        if (appUser == null) return;

        if (!await _userManager.IsInRoleAsync(appUser, identityRole))
            await _userManager.AddToRoleAsync(appUser, identityRole);
    }

    private async Task RemoveIdentityRoleIfUnusedAsync(int isciId, StrukturRolTipi rolTipi, int? excludingId = null)
    {
        var identityRole = StrukturToIdentityRole(rolTipi);
        if (identityRole == null) return;

        // Eyni işçidə EYNİ növ başqa aktiv struktur rolu qalıbsa, Identity rolunu saxla
        var digerAktiv = await _unitOfWork.Repository<IsciStrukturRolu>()
            .MovcuddurmuAsync(x =>
                !x.Silinib &&
                x.IsciId == isciId &&
                x.RolTipi == rolTipi &&
                x.Aktivdir &&
                (excludingId == null || x.Id != excludingId.Value));

        if (digerAktiv) return;

        var appUser = await _userManager.Users.FirstOrDefaultAsync(u => u.IsciId == isciId);
        if (appUser == null) return;

        if (await _userManager.IsInRoleAsync(appUser, identityRole))
            await _userManager.RemoveFromRoleAsync(appUser, identityRole);
    }

    // ── Köməkçilər ─────────────────────────────────────────
    private async Task<List<SelectListItem>> GetIsciSelectListAsync(int? selected = null)
    {
        var list = await _unitOfWork.Repository<Isci>()
            .Query()
            .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
            .OrderBy(x => x.Soyad).ThenBy(x => x.Ad)
            .ToListAsync();

        return list.Select(x => new SelectListItem(
            $"{x.Soyad} {x.Ad}", x.Id.ToString(), x.Id == selected)).ToList();
    }

    private async Task<List<SelectListItem>> GetDepartamentSelectListAsync(int? selected = null)
    {
        var list = await _unitOfWork.Repository<Departament>()
            .Query()
            .Where(x => !x.Silinib)
            .OrderBy(x => x.Ad)
            .ToListAsync();

        return list.Select(x => new SelectListItem(
            x.Ad, x.Id.ToString(), x.Id == selected)).ToList();
    }
}
