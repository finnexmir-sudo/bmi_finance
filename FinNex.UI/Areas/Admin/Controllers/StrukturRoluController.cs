using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class StrukturRoluController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public StrukturRoluController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

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
            HrSayi = list.Count(x => x.RolTipi == StrukturRolTipi.Hr && x.Aktivdir)
        };

        ViewData["Title"] = "Struktur Rolları";
        return View(vm);
    }

    // ── GET /Admin/StrukturRolu/Create ──────────────────────
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new StrukturRoluFormVM
        {
            BaslamaTarixi = DateTime.Today,
            Aktivdir = true,
            Isciler = await GetIsciSelectListAsync(),
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

        entity.IsciId = vm.IsciId;
        entity.RolTipi = vm.RolTipi;
        entity.DepartamentId = vm.DepartamentId;
        entity.BaslamaTarixi = vm.BaslamaTarixi;
        entity.BitmeTarixi = vm.BitmeTarixi;
        entity.Aktivdir = vm.Aktivdir;

        await _unitOfWork.Repository<IsciStrukturRolu>().YenileAsync(entity);
        await _unitOfWork.YaddaSaxlaAsync();

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

        await _unitOfWork.Repository<IsciStrukturRolu>().YumshakSilAsync(id);
        await _unitOfWork.YaddaSaxlaAsync();

        TempData["Success"] = "Struktur rolu silindi.";
        return RedirectToAction(nameof(Index));
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
