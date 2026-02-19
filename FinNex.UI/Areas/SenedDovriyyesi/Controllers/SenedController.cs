using DocumentFormat.OpenXml.Spreadsheet;
using FinNex.Application.Common.Paged;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Application.DTOs.SenedDovriyyesi.SenedNovu;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain.Entities.SenedDovriyyesi;
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
    private readonly ISenedWorkflowService _workflow;

    public SenedController(
        ISenedService senedService,
        ISenedNovuService novuService,
        IDepartmentService departmentService,
        IUserDepartmentService userDepartmentService, ISenedWorkflowService workflow)
    {
        _senedService = senedService;
        _novuService = novuService;
        _departmentService = departmentService;
        _userDepartmentService = userDepartmentService;
        _workflow = workflow;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string? GetIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
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

        var dto = new SenedCreateDto
        {
            SobeId = vm.SobeId,
            SenedNovuId = vm.SenedNovuId,
            Basliq = vm.Basliq,
            AcarSoz = vm.AcarSoz
        };

        var result = await _senedService.CreateAsync(dto, GetUserId(), GetIp());


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
    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");

        var result = await _senedService.GetDetailAsync(id, userId, isAdmin);

        if (!result.Success || result.Data == null)
            return NotFound();



        var list = new SenedDetailVM
        {
            AcarSoz = result.Data.AcarSoz,
            Id = id,
            SenedNovu = result.Data.SenedNovu,
            Sobe = result.Data.Sobe,
            Status = result.Data.Status,
            Tags = result.Data.Tags,
            YaradilmaTarixi = result.Data.YaradilmaTarixi,
            AuditLogs = result.Data.AuditLogs
    .Select(x => new AuditLogItemVM
    {
        
       UserId = userId,
       Ip = x.Ip,
       DetailsJson = x.DetailsJson,
       Action = x.Action,
       YaradilmaTarixi = x.YaradilmaTarixi
    })
    .ToList(),

            Basliq = result.Data.Basliq,
            Fayllar = result.Data.Fayllar.Select(a=> new SenedFaylItemVM 
            {
                ContentType = a.ContentType,
                AktivVersiya=a.AktivVersiya,
                Id = a.Id,
                OlcuBytes = a.OlcuBytes,
                OriginalAd=a.OriginalAd,
                Sha256 = a.Sha256,
                VersiyaNo = a.VersiyaNo,
                YaradilmaTarixi = a.YaradilmaTarixi,
            }
            ).ToList()
        };

        return View(list);
    }



    // =========================
    // SƏNƏD NÖVLƏRİ
    // =========================
    public async Task<IActionResult> SenedNovleri()
    {
        var sobelerResult = await _departmentService.GetAllAsync(x => !x.Silinib);
        var novlerResult = await _novuService.GetAllAsync(x => !x.Silinib);

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
            SobeAd = sobelerResult.Data!.FirstOrDefault(s => s.Id == n.DepartmentId)?.Ad ?? "-",
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
            .GetAllAsync(x => x.DepartmentId == sobeId && x.Aktiv);

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
        var sobeler = await _departmentService.GetAllAsync(x => !x.Silinib);

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
        var sobeler = await _departmentService.GetAllAsync(x => !x.Silinib);
        var novler = await _novuService.GetAllAsync(x => x.Aktiv);

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

    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatus(
    int id,
    SenedStatusu status,
    string? comment)
    {
        var userId = GetUserId();

        var role =
            User.IsInRole("Admin") ? "Admin" :
            User.IsInRole("Manager") ? "Manager" :
            User.IsInRole("SobeRehberi") ? "SobeRehberi" :
            "Operator";

        var result = await _senedService.GetByIdAsync(id);

        if (!result.Success || result.Data == null)
            return NotFound();

        var sened = result.Data;

        if (!_workflow.CanChangeStatus(role, sened.Status, status))
            return Forbid();

        if (_workflow.RequiresRejectComment(sened.Status, status)
            && string.IsNullOrWhiteSpace(comment))
        {
            TempData["Error"] = "Rədd üçün səbəb yazılmalıdır.";
            return RedirectToAction("Detail", new { id });
        }

        await _senedService.ChangeStatusAsync(id, status, comment, userId);

        return RedirectToAction("Detail", new { id });
    }



}
