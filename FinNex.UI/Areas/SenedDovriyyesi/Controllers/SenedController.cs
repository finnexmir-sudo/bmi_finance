using FinNex.Application.Common.Paged;
using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.DTOs.SenedDovriyyesi.Fayl;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Application.DTOs.SenedDovriyyesi.SenedNovu;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.SenedDovriyyesi.ViewModels;
using FinNex.UI.ViewModels.SenedDovriyyesi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace FinNex.UI.Areas.SenedDovriyyesi.Controllers;

[Area("SenedDovriyyesi")]
[Authorize]
public class SenedController : Controller
{
    private readonly ISenedService _senedService;
    private readonly ISenedNovuService _novuService;
    private readonly IDepartmentService _departmentService;
    private readonly IUserDepartmentService _userDepartmentService;
    private readonly ISenedDovriyyesiIstifadeciIcazesiService _icazeService;
    private readonly IUnitOfWork _unitOf;
    private readonly IWebHostEnvironment _environment;

    public SenedController(
        ISenedService senedService,
        ISenedNovuService novuService,
        IDepartmentService departmentService,
        IUserDepartmentService userDepartmentService,
        ISenedDovriyyesiIstifadeciIcazesiService icazeService,
        IUnitOfWork unitOf,
        IWebHostEnvironment environment)
    {
        _senedService = senedService;
        _novuService = novuService;
        _departmentService = departmentService;
        _userDepartmentService = userDepartmentService;
        _icazeService = icazeService;
        _unitOf = unitOf;
        _environment = environment;
    }

    // ── Köməkçi metodlar ─────────────────────────────────────────
    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string? GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private bool IsAdmin() =>
        User.IsInRole(RoleNames.Admin);

    private async Task<List<int>> GetIcazeliSobeIdleriAsync()
    {
        var userId = GetUserId();
        if (IsAdmin())
        {
            var butun = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);
            return butun.Data?.Select(x => x.Id).Distinct().ToList() ?? new();
        }

        var netice = new List<int>();
        var userDepts = await _userDepartmentService.GetUserDepartmentsAsync(userId);
        if (userDepts != null)
            netice.AddRange(userDepts.Select(x => x.DepartmentId));

        var elaveIcazeler = await _icazeService.IstifadeciyeGoreGetirAsync(userId);
        if (elaveIcazeler.Success && elaveIcazeler.Data != null)
            netice.AddRange(elaveIcazeler.Data.Select(x => x.SobeId));

        return netice.Distinct().ToList();
    }

    private async Task<List<int>> GetTamIcazeliSobeIdleriAsync()
    {
        var userId = GetUserId();
        if (IsAdmin())
        {
            var butun = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);
            return butun.Data?.Select(x => x.Id).Distinct().ToList() ?? new();
        }

        var netice = new List<int>();
        var elaveIcazeler = await _icazeService.IstifadeciyeGoreGetirAsync(userId);
        if (elaveIcazeler.Success && elaveIcazeler.Data != null)
        {
            netice.AddRange(elaveIcazeler.Data
                .Where(x => x.IcazeNovu == (int)IcazeNovu.Full)
                .Select(x => x.SobeId));
        }
        return netice.Distinct().ToList();
    }

    private async Task<bool> BaxisIcazesiVarAsync(int sobeId)
    {
        if (IsAdmin()) return true;
        var icazeliSobeler = await GetIcazeliSobeIdleriAsync();
        return icazeliSobeler.Contains(sobeId);
    }

    private async Task<bool> TamIcazeVarAsync(int sobeId)
    {
        if (IsAdmin()) return true;
        var tamIcazeler = await GetTamIcazeliSobeIdleriAsync();
        return tamIcazeler.Contains(sobeId);
    }

    // ── Fayl endirmə ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var fayl = await _unitOf.Repository<SenedFayl>()
            .GetirAsync(x => x.Id == id && !x.Silinib);
        if (fayl == null) return NotFound();

        var rootPath = @"C:\FinNex_DMS";
        var fullPath = Path.Combine(rootPath,
            fayl.Yol.Replace("/", Path.DirectorySeparatorChar.ToString())
                    .Replace("\\", Path.DirectorySeparatorChar.ToString()));

        if (!System.IO.File.Exists(fullPath))
            return NotFound("Fayl tapılmadı.");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return File(bytes, fayl.ContentType, fayl.OriginalAd);
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id)
    {
        var fayl = await _unitOf.Repository<SenedFayl>()
            .GetirAsync(x => x.Id == id && !x.Silinib);
        if (fayl == null) return NotFound();

        var rootPath = @"C:\FinNex_DMS";
        var fullPath = Path.Combine(rootPath,
            fayl.Yol.Replace("/", Path.DirectorySeparatorChar.ToString())
                    .Replace("\\", Path.DirectorySeparatorChar.ToString()));

        if (!System.IO.File.Exists(fullPath))
            return NotFound("Fayl tapılmadı.");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'";
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fayl.OriginalAd}\"";
        return File(bytes, fayl.ContentType);
    }

    // ── INDEX ─────────────────────────────────────────────────────
    public async Task<IActionResult> Index(
        int? sobeId, int? senedNovuId, SenedStatusu? status,
        string? q, int page = 1, int pageSize = 20)
    {
        var icazeliSobeIdleri = await GetIcazeliSobeIdleriAsync();

        if (!IsAdmin() && sobeId.HasValue && !icazeliSobeIdleri.Contains(sobeId.Value))
            return Forbid();

        var result = await _senedService.GetPagedAsync(
            new PagedRequest { Page = page, PageSize = pageSize },
            icazeliSobeIdleri, sobeId, senedNovuId, status, q);

        var vm = new SenedListVM
        {
            Page = page,
            PageSize = pageSize,
            SobeId = sobeId,
            SenedNovuId = senedNovuId,
            Status = status,
            AxtarisKelimesi = q
        };

        if (result.Success && result.Data != null)
        {
            vm.TotalCount = result.Data.TotalCount;
            vm.Senedler = result.Data.Items.Select(x => new SenedListItemVM
            {
                Id = x.Id,
                SenedNomresi = x.SenedNomresi,
                Basliq = x.Basliq,
                AcarSoz = x.AcarSoz,
                Status = x.Status,
                Sobe = x.Sobe,
                SenedNovu = x.SenedNovu,
                FaylSayi = x.FaylSayi,
                YaradilmaTarixi = x.YaradilmaTarixi
            }).ToList();
        }

        await LoadDropdowns(vm, icazeliSobeIdleri);
        return View(vm);
    }

    // ── SİLİNMİŞLƏR ──────────────────────────────────────────────
    public async Task<IActionResult> Silinmisler(
        int? sobeId, int? senedNovuId, SenedStatusu? status,
        string? q, int page = 1, int pageSize = 20)
    {
        if (!IsAdmin()) return Forbid(); // Yalnız Admin

        var icazeliSobeIdleri = await GetIcazeliSobeIdleriAsync();

        var result = await _senedService.GetSilinmisPagedAsync(
            new PagedRequest { Page = page, PageSize = pageSize },
            icazeliSobeIdleri, sobeId, senedNovuId, status, q);

        var vm = new SenedListVM
        {
            Page = page,
            PageSize = pageSize,
            SobeId = sobeId,
            SenedNovuId = senedNovuId,
            Status = status,
            AxtarisKelimesi = q,
            Silinmisler = true
        };

        if (result.Success && result.Data != null)
        {
            vm.TotalCount = result.Data.TotalCount;
            vm.Senedler = result.Data.Items.Select(x => new SenedListItemVM
            {
                Id = x.Id,
                SenedNomresi = x.SenedNomresi,
                Basliq = x.Basliq,
                AcarSoz = x.AcarSoz,
                Status = x.Status,
                Sobe = x.Sobe,
                SenedNovu = x.SenedNovu,
                FaylSayi = x.FaylSayi,
                YaradilmaTarixi = x.YaradilmaTarixi
            }).ToList();
        }

        await LoadDropdowns(vm, icazeliSobeIdleri);
        return View(vm);
    }

    // ── YARAT GET ────────────────────────────────────────────────
    public async Task<IActionResult> Yarat()
    {
        var vm = new SenedCreateVM();
        await LoadCreateDropdowns(vm);
        return View(vm);
    }

    // ── YARAT POST ───────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(SenedCreateVM vm)
    {
        if (!ModelState.IsValid)
        {
            await LoadCreateDropdowns(vm);
            return View(vm);
        }

        // Şöbəyə icazəni yoxla
        if (!await BaxisIcazesiVarAsync(vm.SobeId))
        {
            TempData["Error"] = "Bu şöbəyə sənəd əlavə etmək icazəniz yoxdur.";
            await LoadCreateDropdowns(vm);
            return View(vm);
        }

        var createDto = new SenedCreateDto
        {
            SobeId = vm.SobeId,
            SenedNovuId = vm.SenedNovuId,
            Basliq = vm.Basliq,
            AcarSoz = vm.AcarSoz
        };

        var uploadDto = new SenedUploadDto
        {
            SobeId = vm.SobeId,
            SenedNovuId = vm.SenedNovuId,
            Basliq = vm.Basliq,
            AcarSoz = vm.AcarSoz,
            Fayl = vm.Fayl
        };

        var result = await _senedService.CreateAsync(createDto, uploadDto, GetUserId(), GetIp());

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message ?? "Xəta baş verdi");
            await LoadCreateDropdowns(vm);
            return View(vm);
        }

        TempData["Success"] = "Sənəd yaradıldı";
        return RedirectToAction(nameof(Index));
    }

    // ── DETAL ────────────────────────────────────────────────────
    public async Task<IActionResult> Detal(int id)
    {
        var icazeliSobeIdleri = await GetIcazeliSobeIdleriAsync();
        var result = await _senedService.GetDetailAsync(id, icazeliSobeIdleri, IsAdmin());

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message ?? "Tapılmadı";
            return RedirectToAction(nameof(Index));
        }

        var dto = result.Data;
        var vm = MapToDetailVM(dto);
        return View(vm);
    }

    // ── YENİ VERSİYA YÜKLƏ ─────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YeniVersiya(int senedId, IFormFile fayl)
    {
        if (fayl == null || fayl.Length == 0)
        {
            TempData["Error"] = "Fayl seçilməlidir.";
            return RedirectToAction(nameof(Detal), new { id = senedId });
        }

        // Sənədi tapıb icazəni yoxla
        var sened = await _senedService.IdIleGetirAsync(senedId);
        if (!sened.Success || sened.Data == null)
        {
            TempData["Error"] = "Sənəd tapılmadı.";
            return RedirectToAction(nameof(Index));
        }

        if (!await TamIcazeVarAsync(sened.Data.DepartmentId))
        {
            TempData["Error"] = "Bu sənədə fayl yükləmək icazəniz yoxdur.";
            return RedirectToAction(nameof(Detal), new { id = senedId });
        }

        using var stream = fayl.OpenReadStream();
        var uploadDto = new SenedFaylUploadDto
        {
            SenedId = senedId,
            OriginalAd = fayl.FileName,
            ContentType = fayl.ContentType,
            OlcuBytes = fayl.Length,
            Stream = stream
        };

        var result = await _senedService.UploadNewVersionAsync(uploadDto, GetUserId(), GetIp());

        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Detal), new { id = senedId });
    }

    // ── STATUS DƏYİŞDİR ──────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StatusDeyis(int id, SenedStatusu status)
    {
        // Tam icazə yoxla
        var sened = await _senedService.IdIleGetirAsync(id);
        if (!sened.Success || sened.Data == null)
        {
            TempData["Error"] = "Sənəd tapılmadı.";
            return RedirectToAction(nameof(Index));
        }

        if (!await TamIcazeVarAsync(sened.Data.DepartmentId))
        {
            TempData["Error"] = "Bu əməliyyat üçün icazəniz yoxdur.";
            return RedirectToAction(nameof(Detal), new { id });
        }

        var result = await _senedService.UpdateStatusAsync(id, status, GetUserId(), GetIp());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Detal), new { id });
    }

    // ── DETAL SİLİNMİŞ ───────────────────────────────────────────
    public async Task<IActionResult> DetalSilinmis(int id)
    {
        if (!IsAdmin()) return Forbid();

        var icazeliSobeIdleri = await GetIcazeliSobeIdleriAsync();
        var result = await _senedService.GetDetailSilinmisAsync(id, icazeliSobeIdleri, IsAdmin());

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message ?? "Tapılmadı";
            return RedirectToAction(nameof(Silinmisler));
        }

        return View(MapToDetailVM(result.Data));
    }

    // ── SİL ──────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult SilTesdiq(int id)
    {
        ViewBag.Id = id;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var icazeliSobeIdleri = await GetTamIcazeliSobeIdleriAsync();
        var icazeYoxlamasi = await _senedService.silmeİCazeSorgusuAsync(id, icazeliSobeIdleri, IsAdmin());

        if (!icazeYoxlamasi.Success || icazeYoxlamasi.Data == null)
        {
            TempData["Error"] = icazeYoxlamasi.Message ?? "Tapılmadı";
            return RedirectToAction(nameof(Index));
        }

        var result = await _senedService.SoftDeleteAsync(id, GetUserId(), GetIp());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // ── BƏRPA ET (GET + POST) ─────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BerpaEt(int id)
    {
        if (!IsAdmin())
        {
            TempData["Error"] = "Bərpa əməliyyatı yalnız Admin tərəfindən icra edilə bilər.";
            return RedirectToAction(nameof(Silinmisler));
        }

        var result = await _senedService.RestoreAsync(id, GetUserId(), GetIp());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // Köhnə GET Restore — uyğunluq üçün saxlanılır
    [HttpGet]
    public async Task<IActionResult> Restore(int id)
    {
        if (!IsAdmin()) return Forbid();

        var result = await _senedService.RestoreAsync(id, GetUserId(), GetIp());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // ── SƏNƏD NÖVLƏRİ ────────────────────────────────────────────
    public async Task<IActionResult> SenedNovleri()
    {
        var sobelerResult = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);
        var novlerResult = await _novuService.HamisiniGetirAsync(x => !x.Silinib);

        if (!sobelerResult.Success || !novlerResult.Success)
        {
            TempData["Error"] = "Məlumatlar yüklənmədi";
            return View(new SenedNovleriVM());
        }

        var vm = new SenedNovleriVM
        {
            Sobeler = sobelerResult.Data!.Select(s => new DropdownItemVM { Id = s.Id, Ad = s.Ad }).ToList(),
            Novler = novlerResult.Data!.Select(n => new SenedNovuItemVM
            {
                Id = n.Id,
                Kod = n.Kod,
                Ad = n.Ad,
                SobeId = n.DepartmentId,
                DepartmentAd = n.DepartmentAd,
                Aktiv = n.Aktiv,
                YaradilmaTarixi = n.YaradilmaTarixi
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> YeniSenedNovu([FromBody] SenedNovuCreateDto dto)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Məlumatlar tam deyil." });

        var result = await _novuService.CreateAsync(dto, GetUserId());
        return Json(new { success = result.Success, message = result.Message, id = result.Data });
    }

    [HttpGet]
    public async Task<IActionResult> SenedNovleriByShobe(int sobeId)
    {
        var result = await _novuService.HamisiniGetirAsync(x => x.DepartmentId == sobeId && x.Aktiv);
        if (!result.Success || result.Data == null)
            return Json(new List<object>());

        return Json(result.Data.Select(x => new { id = x.Id, ad = x.Ad }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SenedNovuSil(int id)
    {
        var result = await _novuService.SoftDeleteAsync(id, GetUserId());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(SenedNovleri));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SenedNovuAktivDeyis(int id)
    {
        var result = await _novuService.ToggleAktivAsync(id, GetUserId());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(SenedNovleri));
    }

    // ── REDAKTƏ ───────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Redakte(int id)
    {
        var sened = await _senedService.IdIleGetirAsync(id);
        if (sened == null || !sened.Success || sened.Data == null)
            return NotFound();

        // İcazə yoxla
        if (!await TamIcazeVarAsync(sened.Data.DepartmentId))
        {
            TempData["Error"] = "Bu sənədi redaktə etmək icazəniz yoxdur.";
            return RedirectToAction(nameof(Detal), new { id });
        }

        var vm = new SenedUpdateVM
        {
            Id = sened.Data.Id,
            SobeId = sened.Data.DepartmentId,
            SenedNovuId = sened.Data.SenedNovuId,
            Basliq = sened.Data.Basliq,
            AcarSoz = sened.Data.AcarSoz
        };

        var aktivFayl = sened.Data?.Fayllar?.FirstOrDefault(x => x.AktivVersiya);
        ViewBag.CurrentFileName = aktivFayl?.OriginalAd;
        ViewBag.CurrentFileSize = aktivFayl?.OlcuBytes;
        ViewBag.YaradilmaTarixi = sened.Data!.YaradilmaTarixi.ToString("dd.MM.yyyy HH:mm");
        ViewBag.SonDeyisiklik = sened.Data!.YenilenmeTarixi?.ToString("dd.MM.yyyy HH:mm");

        await LoadUpdateDropdowns(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Redakte(SenedUpdateVM vm)
    {
        if (!ModelState.IsValid)
        {
            await LoadUpdateDropdowns(vm);
            return View(vm);
        }

        var updateDto = new SenedUpdateDto
        {
            Id = vm.Id,
            SobeId = vm.SobeId,
            SenedNovuId = vm.SenedNovuId,
            Basliq = vm.Basliq,
            AcarSoz = vm.AcarSoz,
            TagIds = vm.TagIds ?? new List<int>()
        };

        var result = await _senedService.UpdateAsync(updateDto, GetUserId(), GetIp());

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message ?? "Xəta baş verdi");
            await LoadUpdateDropdowns(vm);
            return View(vm);
        }

        TempData["Success"] = "Sənəd uğurla yeniləndi";
        return RedirectToAction(nameof(Detal), new { id = vm.Id });
    }

    // ── DROPDOWN HELPERS ──────────────────────────────────────────
    private async Task LoadDropdowns(SenedListVM vm, List<int>? icazeliSobeIdleri = null)
    {
        icazeliSobeIdleri ??= await GetIcazeliSobeIdleriAsync();

        var sobeResult = await _departmentService.HamisiniGetirAsync(x =>
            !x.Silinib && icazeliSobeIdleri.Contains(x.Id));

        vm.Sobeler = sobeResult.Data?
            .Select(x => new DropdownItemVM { Id = x.Id, Ad = x.Ad })
            .ToList() ?? new();

        if (vm.SobeId.HasValue)
        {
            var novuResult = await _novuService.HamisiniGetirAsync(x =>
                x.DepartmentId == vm.SobeId.Value && x.Aktiv);
            vm.SenedNovleri = novuResult.Data?
                .Select(x => new DropdownItemVM { Id = x.Id, Ad = x.Ad })
                .ToList() ?? new();
        }
    }

    private async Task LoadCreateDropdowns(SenedCreateVM vm)
    {
        // ── DÜZƏLDILDI: yalnız icazəli şöbələr göstərilir ──
        var icazeliSobeIdleri = await GetIcazeliSobeIdleriAsync();

        var sobeler = await _departmentService.HamisiniGetirAsync(x =>
            !x.Silinib && icazeliSobeIdleri.Contains(x.Id));

        var novler = await _novuService.HamisiniGetirAsync(x => x.Aktiv);

        if (sobeler.Success && sobeler.Data != null)
            vm.Sobeler = sobeler.Data
                .Select(s => new DropdownItemVM { Id = s.Id, Ad = s.Ad })
                .ToList();

        if (novler.Success && novler.Data != null)
            vm.SenedNovleri = novler.Data
                .Select(n => new DropdownItemVM { Id = n.Id, Ad = n.Ad })
                .ToList();
    }

    private async Task LoadUpdateDropdowns(SenedUpdateVM vm)
    {
        var sobeler = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);
        var senedNovleri = await _novuService.HamisiniGetirAsync(x => x.Aktiv);

        ViewBag.Sobeler = new SelectList(sobeler.Data, "Id", "Ad", vm.SobeId);
        ViewBag.SenedNovleri = new SelectList(senedNovleri.Data, "Id", "Ad", vm.SenedNovuId);
    }

    // ── MAP HELPER ────────────────────────────────────────────────
    private static SenedDetailVM MapToDetailVM(SenedDetailDto dto) => new()
    {
        Id = dto.Id,
        SenedNomresi = dto.SenedNomresi,
        Basliq = dto.Basliq,
        AcarSoz = dto.AcarSoz,
        Status = dto.Status,
        Sobe = dto.Sobe,
        SenedNovu = dto.SenedNovu,
        YaradilmaTarixi = dto.YaradilmaTarixi,
        YenilenmeTarixi = dto.YenilenmeTarixi,
        Tags = dto.Tags,
        Fayllar = dto.Fayllar.Select(f => new SenedFaylItemVM
        {
            Id = f.Id,
            VersiyaNo = f.VersiyaNo,
            OriginalAd = f.OriginalAd,
            ContentType = f.ContentType,
            OlcuBytes = f.OlcuBytes,
            Sha256 = f.Sha256,
            AktivVersiya = f.AktivVersiya,
            YaradilmaTarixi = f.YaradilmaTarixi
        }).ToList(),
        AuditLogs = dto.AuditLogs.Select(l => new AuditLogItemVM
        {
            Id = l.Id,
            UserId = l.UserId,
            UserName = l.UserName,
            Action = l.Action,
            Ip = l.Ip,
            DetailsJson = l.DetailsJson,
            YaradilmaTarixi = l.YaradilmaTarixi
        }).ToList()
    };
}