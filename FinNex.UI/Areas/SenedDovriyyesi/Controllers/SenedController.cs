using FinNex.Application.Common.Paged;
using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Application.DTOs.SenedDovriyyesi.SenedNovu;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Application.Interfaces.Structur;
using FinNex.DataAccess.UnitOfWorks;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Interfaces;
using FinNex.UI.ViewModels.SenedDovriyyesi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        IUserDepartmentService userDepartmentService,IUnitOfWork unitOf, IWebHostEnvironment environment)
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
        var rootPath = @"D:\FinNex_DMS";

        var safeRelativePath = fayl.Yol
            .Replace("/", Path.DirectorySeparatorChar.ToString())
            .Replace("\\", Path.DirectorySeparatorChar.ToString());

        var fullPath = Path.Combine(rootPath, safeRelativePath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound("Fayl tapılmadı.");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);

        return File(bytes, fayl.ContentType, fayl.OriginalAd);
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

            AuditLogs = new List<AuditLogItemVM>()
        };

        return View(vm);


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

}
