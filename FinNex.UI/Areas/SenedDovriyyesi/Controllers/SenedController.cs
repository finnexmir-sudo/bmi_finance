using FinNex.Application.Common.Paged;
using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Application.DTOs.SenedDovriyyesi.SenedNovu;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Application.Interfaces.Structur;
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
    private readonly IUnitOfWork _unitOf;
    private readonly IWebHostEnvironment _environment;


    public SenedController(
        ISenedService senedService,
        ISenedNovuService novuService,
        IDepartmentService departmentService,
        IUserDepartmentService userDepartmentService, IUnitOfWork unitOf, IWebHostEnvironment environment)
    {
        _senedService = senedService;
        _novuService = novuService;
        _departmentService = departmentService;
        _userDepartmentService = userDepartmentService;
        _unitOf = unitOf;
        _environment = environment;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string? GetIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
    [HttpGet]

    public async Task<IActionResult> Download(int id)
    {
        var fayl = await _unitOf.Repository<SenedFayl>()
            .GetirAsync(x => x.Id == id && !x.Silinib);

        if (fayl == null)
            return NotFound();

        // 👇 D disk root
        var rootPath = @"C:\FinNex_DMS";

        var safeRelativePath = fayl.Yol
            .Replace("/", Path.DirectorySeparatorChar.ToString())
            .Replace("\\", Path.DirectorySeparatorChar.ToString());

        var fullPath = Path.Combine(rootPath, safeRelativePath);

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
        var safeRelativePath = fayl.Yol
            .Replace("/", Path.DirectorySeparatorChar.ToString())
            .Replace("\\", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine(rootPath, safeRelativePath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound("Fayl tapılmadı.");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);

        // iframe-ə icazə ver
        Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'";
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fayl.OriginalAd}\"";

        return File(bytes, fayl.ContentType);
    }


    // =========================
    // INDEX
    // =========================
    public async Task<IActionResult> Index(
        int? sobeId,
        int? senedNovuId,
        SenedStatusu? status,
        string? q,
        int page = 1,
        int pageSize = 20)
    {
        var result = await _senedService.GetPagedAsync(
            new PagedRequest { Page = page, PageSize = pageSize },
            sobeId, senedNovuId, status, q);

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

            vm.Senedler = result.Data.Items
                .Select(x => new SenedListItemVM
                {
                    Id = x.Id,
                    Basliq = x.Basliq,
                    AcarSoz = x.AcarSoz,
                    Status = x.Status,
                    Sobe = x.Sobe,
                    SenedNovu = x.SenedNovu,
                    FaylSayi = x.FaylSayi,
                    YaradilmaTarixi = x.YaradilmaTarixi
                })
                .ToList();
        }


        await LoadDropdowns(vm);

        return View(vm);
    }

    public async Task<IActionResult> Silinmisler(
        int? sobeId,
        int? senedNovuId,
        SenedStatusu? status,
        string? q,
        int page = 1,
        int pageSize = 20)
    {
        var result = await _senedService.GetSilinmisPagedAsync(
            new PagedRequest { Page = page, PageSize = pageSize },
            sobeId, senedNovuId, status, q);

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

            vm.Senedler = result.Data.Items
                .Select(x => new SenedListItemVM
                {
                    Id = x.Id,
                    Basliq = x.Basliq,
                    AcarSoz = x.AcarSoz,
                    Status = x.Status,
                    Sobe = x.Sobe,
                    SenedNovu = x.SenedNovu,
                    FaylSayi = x.FaylSayi,
                    YaradilmaTarixi = x.YaradilmaTarixi
                })
                .ToList();
        }


        await LoadDropdowns(vm);

        return View(vm);
    }

    // =========================
    // YARAT GET
    // =========================
    public async Task<IActionResult> Yarat()
    {
        var vm = new SenedCreateVM();
        await LoadCreateDropdowns(vm);
        return View(vm);
    }

    // =========================
    // YARAT POST
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(SenedCreateVM vm)
    {
        if (!ModelState.IsValid)
        {
            await LoadCreateDropdowns(vm);
            return View(vm);
        }

        // ==========================
        // 1️⃣ Metadata DTO
        // ==========================
        var createDto = new SenedCreateDto
        {
            SobeId = vm.SobeId,
            SenedNovuId = vm.SenedNovuId,
            Basliq = vm.Basliq,
            AcarSoz = vm.AcarSoz,
            //TagIds = vm.TagIds ?? new List<int>()
        };

        // ==========================
        // 2️⃣ Upload DTO
        // ==========================
        var uploadDto = new SenedUploadDto
        {
            SobeId = vm.SobeId,
            SenedNovuId = vm.SenedNovuId,
            Basliq = vm.Basliq,
            AcarSoz = vm.AcarSoz,
            Fayl = vm.Fayl // 👈 BU VACİBDİR
        };

        var result = await _senedService.CreateAsync(
            createDto,
            uploadDto,
            GetUserId(),
            GetIp());

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message ?? "Xəta baş verdi");
            await LoadCreateDropdowns(vm);
            return View(vm);
        }

        TempData["Success"] = "Sənəd yaradıldı";
        return RedirectToAction(nameof(Index));
    }


    // =========================
    // DETAL
    // =========================
    public async Task<IActionResult> Detal(int id)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");

        var result = await _senedService.GetDetailAsync(id, userId, isAdmin);

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message ?? "Tapılmadı";
            return RedirectToAction(nameof(Index));
        }

        var dto = result.Data;

        var vm = new SenedDetailVM
        {
            Id = dto.Id,
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

        return View(vm);


    }

    public async Task<IActionResult> DetalSilinmis(int id)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");

        var result = await _senedService.GetDetailSilinmisAsync(id, userId, isAdmin);

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message ?? "Tapılmadı";
            return RedirectToAction(nameof(Index));
        }

        var dto = result.Data;

        var vm = new SenedDetailVM
        {
            Id = dto.Id,
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

        return View(vm);


    }


    // =========================
    // SƏNƏD NÖVLƏRİ
    // =========================
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
            Sobeler = sobelerResult.Data!
        .Select(s => new DropdownItemVM
        {
            Id = s.Id,
            Ad = s.Ad
        })
        .ToList(),

            Novler = novlerResult.Data!
        .Select(n => new SenedNovuItemVM
        {
            Id = n.Id,
            Kod = n.Kod,
            Ad = n.Ad,
            SobeId = n.DepartmentId,
            DepartmentAd = n.DepartmentAd,
            Aktiv = n.Aktiv,
            YaradilmaTarixi = n.YaradilmaTarixi
        })
        .ToList()
        };


        return View(vm);
    }

    // =========================
    // YENİ SƏNƏD NÖVÜ
    // =========================
    [HttpPost]
    //[ValidateAntiForgeryToken]
    public async Task<IActionResult> YeniSenedNovu([FromBody] SenedNovuCreateDto dto)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Məlumatlar tam deyil." });

        var result = await _novuService.CreateAsync(dto, GetUserId());

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            id = result.Data
        });
    }


    // =========================
    // ŞÖBƏYƏ GÖRƏ NÖVLƏR (AJAX)
    // =========================
    [HttpGet]
    public async Task<IActionResult> SenedNovleriByShobe(int sobeId)
    {
        var result = await _novuService
            .HamisiniGetirAsync(x => x.DepartmentId == sobeId && x.Aktiv);

        if (!result.Success || result.Data == null)
            return Json(new List<object>());

        var data = result.Data
            .Select(x => new
            {
                id = x.Id,
                ad = x.Ad
            });

        return Json(data);
    }

    // =========================
    // SİL
    // =========================

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
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");

        // IP ünvanını Request-dən götürürük
        string ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var icazeYoxlamasi = await _senedService.silmeİCazeSorgusuAsync(id, userId, isAdmin);

        if (!icazeYoxlamasi.Success || icazeYoxlamasi.Data == null)
        {
            TempData["Error"] = icazeYoxlamasi.Message ?? "Tapılmadı";
            return RedirectToAction(nameof(Index));
        }

        var result = await _senedService.SoftDeleteAsync(id, GetUserId(), ip);

        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // =========================
    // SİL
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SenedNovuSil(int id)
    {
        var result = await _novuService.SoftDeleteAsync(id, GetUserId());

        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(SenedNovleri));
    }

    // =========================
    // AKTİV DEYİŞ
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SenedNovuAktivDeyis(int id)
    {
        var result = await _novuService.ToggleAktivAsync(id, GetUserId());

        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(SenedNovleri));
    }

    [HttpGet]
    public async Task<IActionResult> Redakte(int id)
    {
        var sened = await _senedService.IdIleGetirAsync(id);
        if (sened == null)
            return NotFound();

        var vm = new SenedUpdateVM
        {
            Id = sened.Data!.Id,
            SobeId = sened.Data!.DepartmentId,
            SenedNovuId = sened.Data!.SenedNovuId,
            Basliq = sened.Data!.Basliq,
            AcarSoz = sened.Data!.AcarSoz
            //TagIds = sened.Data!.TagIds ?? new List<int>()
        };

        // Cari fayl məlumatları View üçün
        var aktivFayl = sened.Data?.Fayllar?.FirstOrDefault(x => x.AktivVersiya);
        ViewBag.CurrentFileName = aktivFayl!.OriginalAd;
        ViewBag.CurrentFileSize = aktivFayl.OlcuBytes;
        ViewBag.YaradilmaTarixi = sened.Data!.YaradilmaTarixi.ToString("dd.MM.yyyy HH:mm");
        ViewBag.SonDeyisiklik = sened.Data!.YenilenmeTarixi?.ToString("dd.MM.yyyy HH:mm");

        await LoadUpdateDropdowns(vm);
        return View(vm);
    }

    // ============================================================
    // POST: /Sened/Redakte
    // ============================================================
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

    [HttpGet]
    public async Task<IActionResult> Restore(int id)
    {
        if (!ModelState.IsValid)
        {

            return View();
        }

        //var yoxla = await _senedService.IdIleGetirAsync(id);
        //if (yoxla.Data == null)
        //    return RedirectToAction(nameof(Index));


        var result = await _senedService.RestoreAsync(id, GetUserId(), GetIp());

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message ?? "Xəta baş verdi");
            return View();
        }

        TempData["Success"] = "Əməliyyat uğurla qaytarıldı";
        return RedirectToAction(nameof(Index));
    }


    // =========================
    // DROPDOWNS
    // =========================
    private async Task LoadDropdowns(SenedListVM vm)
    {
        var sobeler = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);

        if (sobeler.Success && sobeler.Data != null)
        {
            vm.Sobeler = sobeler.Data
                .Select(s => new DropdownItemVM
                {
                    Id = s.Id,
                    Ad = s.Ad
                })
                .ToList();
        }

    }

    private async Task LoadCreateDropdowns(SenedCreateVM vm)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");

        var sobeler = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);
        var novler = await _novuService.HamisiniGetirAsync(x => x.Aktiv);
        // var tagler = await _tagService.HamisiniGetirAsync(x => !x.Silinib); // 👈 BU YOXDURDU

        if (sobeler.Success && sobeler.Data != null)
        {
            vm.Sobeler = sobeler.Data
                .Select(s => new DropdownItemVM
                {
                    Id = s.Id,
                    Ad = s.Ad
                })
                .ToList();
        }

        if (novler.Success && novler.Data != null)
        {
            vm.SenedNovleri = novler.Data
                .Select(n => new DropdownItemVM
                {
                    Id = n.Id,
                    Ad = n.Ad
                })
                .ToList();
        }

        //// 👇 ƏSAS HİSSƏ
        //if (tagler.Success && tagler.Data != null)
        //{
        //    vm.Tagler = tagler.Data
        //        .Select(t => new DropdownItemVM
        //        {
        //            Id = t.Id,
        //            Ad = t.Ad
        //        })
        //        .ToList();
        //}
    }

    // ============================================================
    // Helper: dropdownları yüklə (update üçün)
    // ============================================================
    private async Task LoadUpdateDropdowns(SenedUpdateVM vm)
    {


        var sobeler = await _departmentService.HamisiniGetirAsync(x => !x.Silinib);
        var senedNovleri = await _novuService.HamisiniGetirAsync(x => x.Aktiv);
        //var tags = await _tagService.GetAllAsync();

        ViewBag.Sobeler = new SelectList(sobeler.Data, "Id", "Ad", vm.SobeId);
        ViewBag.SenedNovleri = new SelectList(senedNovleri.Data, "Id", "Ad", vm.SenedNovuId);
        //ViewBag.Tags = tags; // List<dynamic> və ya List<TagDto>
    }
}
