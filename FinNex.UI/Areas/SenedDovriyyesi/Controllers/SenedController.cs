using FinNex.Application.Common.Paged;
using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.DTOs.SenedDovriyyesi.Fayl;
using FinNex.Application.DTOs.SenedDovriyyesi.Icaze;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Application.DTOs.SenedDovriyyesi.SenedNovu;
using FinNex.Application.DTOs.SenedDovriyyesi.SenedSablon;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.SenedDovriyyesi.ViewModels;
using FinNex.UI.ViewModels.SenedDovriyyesi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
    private readonly ISenedSablonService _sablonService;
    private readonly IUnitOfWork _unitOf;
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<AppUser> _userManager;

    public SenedController(
        ISenedService senedService,
        ISenedNovuService novuService,
        IDepartmentService departmentService,
        IUserDepartmentService userDepartmentService,
        ISenedDovriyyesiIstifadeciIcazesiService icazeService,
        ISenedSablonService sablonService,
        IUnitOfWork unitOf,
        IWebHostEnvironment environment,
        UserManager<AppUser> userManager)
    {
        _senedService = senedService;
        _novuService = novuService;
        _departmentService = departmentService;
        _userDepartmentService = userDepartmentService;
        _icazeService = icazeService;
        _sablonService = sablonService;
        _unitOf = unitOf;
        _environment = environment;
        _userManager = userManager;
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
        int? sobeId, int? senedNovuId, int? tagId, SenedStatusu? status,
        string? q, int page = 1, int pageSize = 20)
    {
        var icazeliSobeIdleri = await GetIcazeliSobeIdleriAsync();

        if (!IsAdmin() && sobeId.HasValue && !icazeliSobeIdleri.Contains(sobeId.Value))
            return Forbid();

        var result = await _senedService.GetPagedAsync(
            new PagedRequest { Page = page, PageSize = pageSize },
            icazeliSobeIdleri, sobeId, senedNovuId, status, q, tagId);

        var vm = new SenedListVM
        {
            Page = page,
            PageSize = pageSize,
            SobeId = sobeId,
            SenedNovuId = senedNovuId,
            TagId = tagId,
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
            AcarSoz = vm.AcarSoz,
            TagIds = vm.TagIds ?? new List<int>()
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

    // ── TOPLU STATUS DƏYİŞ ─────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TopluStatusDeyis([FromBody] TopluStatusDeyisRequest request)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return Json(new { success = false, message = "Heç bir sənəd seçilməyib." });

        var tamIcazeliSobeler = await GetTamIcazeliSobeIdleriAsync();
        var ugurlu = 0;
        var icazesiz = 0;

        foreach (var id in request.Ids)
        {
            var sened = await _senedService.IdIleGetirAsync(id);
            if (!sened.Success || sened.Data == null) continue;

            if (!IsAdmin() && !tamIcazeliSobeler.Contains(sened.Data.DepartmentId))
            {
                icazesiz++;
                continue;
            }

            var result = await _senedService.UpdateStatusAsync(id, (SenedStatusu)request.NewStatus, GetUserId(), GetIp());
            if (result.Success) ugurlu++;
        }

        var message = $"{ugurlu} sənədin statusu dəyişdirildi.";
        if (icazesiz > 0)
            message += $" {icazesiz} sənəd üçün icazə yoxdur.";

        return Json(new { success = ugurlu > 0, message });
    }

    // ── TOPLU SİL ────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TopluSil([FromBody] TopluSilRequest request)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return Json(new { success = false, message = "Heç bir sənəd seçilməyib." });

        var tamIcazeliSobeler = await GetTamIcazeliSobeIdleriAsync();
        var ugurlu = 0;
        var icazesiz = 0;

        foreach (var id in request.Ids)
        {
            var sened = await _senedService.IdIleGetirAsync(id);
            if (!sened.Success || sened.Data == null) continue;

            if (!IsAdmin() && !tamIcazeliSobeler.Contains(sened.Data.DepartmentId))
            {
                icazesiz++;
                continue;
            }

            var result = await _senedService.SoftDeleteAsync(id, GetUserId(), GetIp());
            if (result.Success) ugurlu++;
        }

        var message = $"{ugurlu} sənəd silindi.";
        if (icazesiz > 0)
            message += $" {icazesiz} sənəd üçün icazə yoxdur.";

        return Json(new { success = ugurlu > 0, message });
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

    // ── ETİKETLƏR (TAG İDARƏSİ) ─────────────────────────────────
    public async Task<IActionResult> Etiketler()
    {
        var tags = await _unitOf.Repository<Tag>()
            .HamisiniGetirAsync(x => !x.Silinib,
                include: q => q.Include(t => t.SenedTagMaps));

        var vm = new EtiketlerVM
        {
            Etiketler = tags.Select(t => new EtiketItemVM
            {
                Id = t.Id,
                Ad = t.Ad,
                SenedSayi = t.SenedTagMaps?.Count(m => !m.Silinib) ?? 0,
                YaradilmaTarixi = t.YaradilmaTarixi
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> EtiketYarat([FromBody] EtiketYaratRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.Ad))
            return Json(new { success = false, message = "Etiket adi bos ola bilmez." });

        var ad = req.Ad.Trim();

        var movcud = await _unitOf.Repository<Tag>()
            .AnyAsync(x => x.Ad == ad && !x.Silinib);

        if (movcud)
            return Json(new { success = false, message = "Bu adda etiket artiq movcuddur." });

        var tag = new Tag
        {
            Ad = ad,
            YaradanIcraciId = GetUserId()
        };

        await _unitOf.Repository<Tag>().YaratAsync(tag);
        await _unitOf.YaddaSaxlaAsync();

        return Json(new { success = true, message = "Etiket yaradildi.", id = tag.Id, ad = tag.Ad });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EtiketSil(int id)
    {
        var tag = await _unitOf.Repository<Tag>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (tag == null)
        {
            TempData["Error"] = "Etiket tapilmadi.";
            return RedirectToAction(nameof(Etiketler));
        }

        tag.Silinib = true;
        tag.SilenIcraciId = GetUserId();
        tag.SilinmeTarixi = DateTime.UtcNow;

        await _unitOf.Repository<Tag>().YenileAsync(tag);
        await _unitOf.YaddaSaxlaAsync();

        TempData["Success"] = "Etiket silindi.";
        return RedirectToAction(nameof(Etiketler));
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

        // Sənədin mövcud tag-lərini yüklə
        var senedTagMaps = await _unitOf.Repository<SenedTagMap>()
            .HamisiniGetirAsync(x => x.SenedId == id && !x.Silinib);

        var vm = new SenedUpdateVM
        {
            Id = sened.Data.Id,
            SobeId = sened.Data.DepartmentId,
            SenedNovuId = sened.Data.SenedNovuId,
            Basliq = sened.Data.Basliq,
            AcarSoz = sened.Data.AcarSoz,
            TagIds = senedTagMaps.Select(x => x.TagId).ToList()
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

        var tags = await _unitOf.Repository<Tag>()
            .HamisiniGetirAsync(x => !x.Silinib);
        vm.Tagler = tags
            .Select(x => new DropdownItemVM { Id = x.Id, Ad = x.Ad })
            .ToList();
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

        var tags = await _unitOf.Repository<Tag>()
            .HamisiniGetirAsync(x => !x.Silinib);
        vm.Tagler = tags
            .Select(x => new DropdownItemVM { Id = x.Id, Ad = x.Ad })
            .ToList();
    }

    private async Task LoadUpdateDropdowns(SenedUpdateVM vm)
    {
        var sobeler = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);
        var senedNovleri = await _novuService.HamisiniGetirAsync(x => x.Aktiv);

        ViewBag.Sobeler = new SelectList(sobeler.Data, "Id", "Ad", vm.SobeId);
        ViewBag.SenedNovleri = new SelectList(senedNovleri.Data, "Id", "Ad", vm.SenedNovuId);

        var tags = await _unitOf.Repository<Tag>()
            .HamisiniGetirAsync(x => !x.Silinib);
        ViewBag.Tags = tags
            .Select(x => new { Id = x.Id, Ad = x.Ad })
            .ToList<dynamic>();
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

    // ── İCAZƏLƏR (Admin only) ────────────────────────────────────
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Icazeler()
    {
        var icazelerResult = await _icazeService.HamisiniGetirAsync(
            x => !x.Silinib,
            include: q => q
                .Include(i => i.Istifadeci!)
                .Include(i => i.Departament!));

        var sobelerResult = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);

        var users = await _userManager.Users
            .Where(u => u.Aktivdir)
            .OrderBy(u => u.Ad)
            .ToListAsync();

        var vm = new IcazelerVM
        {
            Icazeler = icazelerResult.Success && icazelerResult.Data != null
                ? icazelerResult.Data.Select(x => new IcazeItemVM
                {
                    Id = x.Id,
                    IstifadeciId = x.IstifadeciId,
                    IstifadeciAd = x.IstifadeciAd,
                    SobeId = x.SobeId,
                    SobeAd = x.SobeAd,
                    IcazeNovu = x.IcazeNovu
                }).ToList()
                : new(),
            Istifadeciler = users
                .Select(u => new DropdownItemVM { Id = u.Id, Ad = $"{u.Ad} {u.Soyad}" })
                .ToList(),
            Sobeler = sobelerResult.Success && sobelerResult.Data != null
                ? sobelerResult.Data.Select(s => new DropdownItemVM { Id = s.Id, Ad = s.Ad }).ToList()
                : new()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> IcazeEkle(int istifadeciId, int sobeId, int icazeNovu)
    {
        if (istifadeciId == 0 || sobeId == 0 || (icazeNovu != 1 && icazeNovu != 2))
        {
            TempData["Error"] = "Bütün sahələr doldurulmalıdır.";
            return RedirectToAction(nameof(Icazeler));
        }

        // Eyni istifadeci + sobe ucun movcud icaze yoxla
        var movcud = await _icazeService.MovcuddurmuAsync(
            x => x.IstifadeciId == istifadeciId && x.SobeId == sobeId && !x.Silinib);

        if (movcud.Success && movcud.Data)
        {
            TempData["Error"] = "Bu istifadəçinin bu şöbə üçün artıq icazəsi var.";
            return RedirectToAction(nameof(Icazeler));
        }

        var dto = new SenedDovriyyesiIstifadeciIcazesiCreateDto
        {
            IstifadeciId = istifadeciId,
            SobeId = sobeId,
            IcazeNovu = icazeNovu
        };

        var result = await _icazeService.YaratAsync(dto);
        TempData[result.Success ? "Success" : "Error"] =
            result.Success ? "İcazə uğurla əlavə edildi." : (result.Message ?? "Xəta baş verdi.");

        return RedirectToAction(nameof(Icazeler));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> IcazeSil(int id)
    {
        var result = await _icazeService.SilAsync(id);
        TempData[result.Success ? "Success" : "Error"] =
            result.Success ? "İcazə silindi." : (result.Message ?? "Xəta baş verdi.");

        return RedirectToAction(nameof(Icazeler));
    }

    // ── ŞABLONLAR ─────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Sablonlar(int? senedNovuId)
    {
        var result = await _sablonService.GetListAsync(senedNovuId);
        var novlerResult = await _novuService.HamisiniGetirAsync(x => x.Aktiv);

        var vm = new SenedSablonlarVM
        {
            SenedNovuId = senedNovuId,
            Sablonlar = result.Success && result.Data != null
                ? result.Data.Select(x => new SenedSablonItemVM
                {
                    Id = x.Id,
                    Ad = x.Ad,
                    Tesvir = x.Tesvir,
                    SenedNovuId = x.SenedNovuId,
                    SenedNovuAd = x.SenedNovuAd,
                    FaylAdi = x.FaylAdi,
                    Aktiv = x.Aktiv,
                    YaradilmaTarixi = x.YaradilmaTarixi
                }).ToList()
                : new(),
            SenedNovleri = novlerResult.Success && novlerResult.Data != null
                ? novlerResult.Data.Select(x => new DropdownItemVM { Id = x.Id, Ad = x.Ad }).ToList()
                : new()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> SablonYarat()
    {
        var vm = new SenedSablonCreateVM();
        var novlerResult = await _novuService.HamisiniGetirAsync(x => x.Aktiv);
        if (novlerResult.Success && novlerResult.Data != null)
            vm.SenedNovleri = novlerResult.Data
                .Select(x => new DropdownItemVM { Id = x.Id, Ad = x.Ad })
                .ToList();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SablonYarat(SenedSablonCreateVM vm)
    {
        if (!ModelState.IsValid)
        {
            var novlerResult = await _novuService.HamisiniGetirAsync(x => x.Aktiv);
            if (novlerResult.Success && novlerResult.Data != null)
                vm.SenedNovleri = novlerResult.Data
                    .Select(x => new DropdownItemVM { Id = x.Id, Ad = x.Ad })
                    .ToList();
            return View(vm);
        }

        var dto = new SenedSablonCreateDto
        {
            Ad = vm.Ad,
            Tesvir = vm.Tesvir,
            SenedNovuId = vm.SenedNovuId
        };

        using var stream = vm.Fayl.OpenReadStream();
        var result = await _sablonService.CreateAsync(
            dto, stream, vm.Fayl.FileName, vm.Fayl.ContentType, GetUserId());

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message ?? "Xəta baş verdi");
            var novlerResult = await _novuService.HamisiniGetirAsync(x => x.Aktiv);
            if (novlerResult.Success && novlerResult.Data != null)
                vm.SenedNovleri = novlerResult.Data
                    .Select(x => new DropdownItemVM { Id = x.Id, Ad = x.Ad })
                    .ToList();
            return View(vm);
        }

        TempData["Success"] = "Şablon yaradıldı";
        return RedirectToAction(nameof(Sablonlar));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SablonSil(int id)
    {
        var result = await _sablonService.DeleteAsync(id, GetUserId());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Sablonlar));
    }

    [HttpGet]
    public async Task<IActionResult> SablonEndir(int id)
    {
        var result = await _sablonService.GetFaylAsync(id);
        if (!result.Success || result.Data.faylYolu == null)
        {
            TempData["Error"] = result.Message ?? "Fayl tapılmadı.";
            return RedirectToAction(nameof(Sablonlar));
        }

        var rootPath = @"C:\FinNex_DMS";
        var fullPath = Path.Combine(rootPath,
            result.Data.faylYolu.Replace("/", Path.DirectorySeparatorChar.ToString())
                    .Replace("\\", Path.DirectorySeparatorChar.ToString()));

        if (!System.IO.File.Exists(fullPath))
            return NotFound("Fayl tapılmadı.");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        var contentType = "application/octet-stream";
        return File(bytes, contentType, result.Data.faylAdi);
    }

    [HttpGet]
    public async Task<IActionResult> SablonlarByNov(int senedNovuId)
    {
        var result = await _sablonService.GetListAsync(senedNovuId);
        if (!result.Success || result.Data == null)
            return Json(new List<object>());

        return Json(result.Data.Select(x => new { id = x.Id, ad = x.Ad, faylAdi = x.FaylAdi }));
    }
}