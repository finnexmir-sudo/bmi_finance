using FinNex.Application.Common.Paged;
using FinNex.Application.DTOs.SenedDovriyyesi.Fayl;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Application.Interfaces.SenedDovriyyesi;
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
    private readonly IAuditLogService _auditLogService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _uow;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "image/jpeg",
        "image/png",
        "image/gif",
        "text/plain",
        "application/zip",
        "application/x-rar-compressed"
    };

    private const long MaxFileSize = 25 * 1024 * 1024; // 25MB

    public SenedController(
        ISenedService senedService,
        IAuditLogService auditLogService,
        IFileStorageService fileStorageService,
        IUnitOfWork uow)
    {
        _senedService = senedService;
        _auditLogService = auditLogService;
        _fileStorageService = fileStorageService;
        _uow = uow;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    // GET: /SenedDovriyyesi/Sened
    public async Task<IActionResult> Index(int? sobeId, int? senedNovuId, SenedStatusu? status,
        string? q, int page = 1, int pageSize = 20)
    {
        ViewData["Title"] = "Sənəd Siyahısı";
        ViewData["ActivePage"] = "Siyahi";

        var result = await _senedService.GetPagedAsync(
            new PagedRequest { Page = page, PageSize = pageSize },
            sobeId, senedNovuId, status, q);

        var vm = new SenedListVM
        {
            SobeId = sobeId,
            SenedNovuId = senedNovuId,
            Status = status,
            AxtarisKelimesi = q,
            Page = page,
            PageSize = pageSize,
            Silinmisler = false
        };

        if (result.Success && result.Data != null)
        {
            vm.TotalCount = result.Data.TotalCount;
            vm.Senedler = result.Data.Items.Select(s => new SenedListItemVM
            {
                Id = s.Id,
                Basliq = s.Basliq,
                AcarSoz = s.AcarSoz,
                Status = s.Status,
                Sobe = s.Sobe,
                SenedNovu = s.SenedNovu,
                FaylSayi = s.FaylSayi,
                YaradilmaTarixi = s.YaradilmaTarixi
            }).ToList();
        }

        await LoadDropdowns(vm);
        return View(vm);
    }

    // GET: /SenedDovriyyesi/Sened/Silinmisler
    public async Task<IActionResult> Silinmisler(int page = 1, int pageSize = 20)
    {
        ViewData["Title"] = "Silinmiş Sənədlər";
        ViewData["ActivePage"] = "Silinmisler";

        var result = await _senedService.GetDeletedPagedAsync(
            new PagedRequest { Page = page, PageSize = pageSize });

        var vm = new SenedListVM
        {
            Page = page,
            PageSize = pageSize,
            Silinmisler = true
        };

        if (result.Success && result.Data != null)
        {
            vm.TotalCount = result.Data.TotalCount;
            vm.Senedler = result.Data.Items.Select(s => new SenedListItemVM
            {
                Id = s.Id,
                Basliq = s.Basliq,
                AcarSoz = s.AcarSoz,
                Status = s.Status,
                Sobe = s.Sobe,
                SenedNovu = s.SenedNovu,
                FaylSayi = s.FaylSayi,
                YaradilmaTarixi = s.YaradilmaTarixi,
                Silinib = true
            }).ToList();
        }

        return View("Index", vm);
    }

    // GET: /SenedDovriyyesi/Sened/Yarat
    public async Task<IActionResult> Yarat()
    {
        ViewData["Title"] = "Yeni Sənəd";
        ViewData["ActivePage"] = "Yarat";

        var vm = new SenedCreateVM();
        await LoadCreateDropdowns(vm);
        return View(vm);
    }

    // POST: /SenedDovriyyesi/Sened/Yarat
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Yarat(SenedCreateVM vm, IFormFile Fayl)
    {
        ViewData["Title"] = "Yeni Sənəd";
        ViewData["ActivePage"] = "Yarat";

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
            AcarSoz = vm.AcarSoz,
            TagIds = vm.TagIds
        };
        if (Fayl == null || Fayl.Length == 0)
        {
            ModelState.AddModelError("", "Fayl seçilməlidir.");
            await LoadCreateDropdowns(vm);
            return View(vm);
        }

        if (Fayl.Length > MaxFileSize)
        {
            ModelState.AddModelError("", "Fayl ölçüsü 25MB-dan böyük ola bilməz.");
            await LoadCreateDropdowns(vm);
            return View(vm);
        }

        if (!AllowedContentTypes.Contains(Fayl.ContentType))
        {
            ModelState.AddModelError("", "Bu fayl növü dəstəklənmir.");
            await LoadCreateDropdowns(vm);
            return View(vm);
        }

        using var stream = Fayl.OpenReadStream();
            
        var result = await _senedService.CreateWithFileAsync(
            dto,
            stream,
            Fayl.FileName,
            Fayl.ContentType,
            Fayl.Length,
            GetUserId(),
            GetIp());

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message ?? "Xəta baş verdi.");
            await LoadCreateDropdowns(vm);
            return View(vm);
        }

        TempData["Success"] = "Sənəd uğurla yaradıldı.";
        return RedirectToAction("Detal", new { id = result.Data });
    }

    // GET: /SenedDovriyyesi/Sened/Detal/5
    public async Task<IActionResult> Detal(int id)
    {
        ViewData["Title"] = "Sənəd Detalları";
        ViewData["ActivePage"] = "Siyahi";

        var result = await _senedService.GetDetailAsync(id);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.Message ?? "Sənəd tapılmadı.";
            return RedirectToAction("Index");
        }

        var auditLogs = await _auditLogService.GetBySenedIdAsync(id);

        var vm = new SenedDetailVM
        {
            Id = result.Data.Id,
            Basliq = result.Data.Basliq,
            AcarSoz = result.Data.AcarSoz,
            Status = result.Data.Status,
            Sobe = result.Data.Sobe,
            SenedNovu = result.Data.SenedNovu,
            YaradilmaTarixi = result.Data.YaradilmaTarixi,
            YenilenmeTarixi = result.Data.YenilenmeTarixi,
            Tags = result.Data.Tags,
            Fayllar = result.Data.Fayllar.Select(f => new SenedFaylItemVM
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
            AuditLogs = auditLogs.Select(a => new AuditLogItemVM
            {
                Action = a.Action,
                DetailsJson = a.DetailsJson,
                Ip = a.Ip,
                YaradilmaTarixi = a.YaradilmaTarixi,
                UserId = a.UserId
            }).ToList()
        };

        return View(vm);
    }

    // POST: /SenedDovriyyesi/Sened/Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Upload(int senedId, IFormFile fayl)
    {
        if (fayl == null || fayl.Length == 0)
        {
            TempData["Error"] = "Fayl seçilməyib.";
            return RedirectToAction("Detal", new { id = senedId });
        }

        if (fayl.Length > MaxFileSize)
        {
            TempData["Error"] = "Fayl ölçüsü 25MB-dan böyük ola bilməz.";
            return RedirectToAction("Detal", new { id = senedId });
        }

        if (!AllowedContentTypes.Contains(fayl.ContentType))
        {
            TempData["Error"] = "Bu fayl növü dəstəklənmir.";
            return RedirectToAction("Detal", new { id = senedId });
        }

        using var stream = fayl.OpenReadStream();
        var dto = new SenedFaylUploadDto
        {
            SenedId = senedId,
            OriginalAd = fayl.FileName,
            ContentType = fayl.ContentType,
            OlcuBytes = fayl.Length,
            Stream = stream
        };

        var result = await _senedService.UploadNewVersionAsync(dto, GetUserId(), GetIp());

        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction("Detal", new { id = senedId });
    }

    // GET: /SenedDovriyyesi/Sened/Download/5
    public async Task<IActionResult> Download(int id)
    {
        var result = await _senedService.GetFileEntityAsync(id);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = "Fayl tapılmadı.";
            return RedirectToAction("Index");
        }

        try
        {
            var stream = await _fileStorageService.OpenReadAsync(result.Data.Yol);
            return File(stream, result.Data.ContentType, result.Data.OriginalAd);
        }
        catch
        {
            TempData["Error"] = "Fayl oxuna bilmədi.";
            return RedirectToAction("Index");
        }
    }

    // POST: /SenedDovriyyesi/Sened/StatusDeyis
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StatusDeyis(int senedId, SenedStatusu status)
    {
        var result = await _senedService.UpdateStatusAsync(senedId, status, GetUserId(), GetIp());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction("Detal", new { id = senedId });
    }

    // POST: /SenedDovriyyesi/Sened/Sil
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var result = await _senedService.SoftDeleteAsync(id, GetUserId(), GetIp());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction("Index");
    }

    // POST: /SenedDovriyyesi/Sened/BerpaEt
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BerpaEt(int id)
    {
        var result = await _senedService.RestoreAsync(id, GetUserId(), GetIp());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction("Silinmisler");
    }

    // GET: /SenedDovriyyesi/Sened/SenedNovleriByShobe?sobeId=1
    [HttpGet]
    public async Task<IActionResult> SenedNovleriByShobe(int sobeId)
    {
        var novler = await _uow.Repository<SenedNovu>()
            .HamisiniGetirAsync(x => x.SobeId == sobeId && x.Aktiv && !x.Silinib);

        var items = novler.Select(n => new { n.Id, n.Ad }).ToList();
        return Json(items);
    }

    private async Task LoadDropdowns(SenedListVM vm)
    {
        var sobeler = await _uow.Repository<Sobe>().HamisiniGetirAsync(x => x.Aktiv && !x.Silinib);
        vm.Sobeler = sobeler.Select(s => new DropdownItemVM { Id = s.Id, Ad = s.Ad }).ToList();

        var novler = await _uow.Repository<SenedNovu>().HamisiniGetirAsync(x => x.Aktiv && !x.Silinib);
        vm.SenedNovleri = novler.Select(n => new DropdownItemVM { Id = n.Id, Ad = n.Ad }).ToList();
    }

    private async Task LoadCreateDropdowns(SenedCreateVM vm)
    {
        var sobeler = await _uow.Repository<Sobe>().HamisiniGetirAsync(x => x.Aktiv && !x.Silinib);
        vm.Sobeler = sobeler.Select(s => new DropdownItemVM { Id = s.Id, Ad = s.Ad }).ToList();

        var novler = await _uow.Repository<SenedNovu>().HamisiniGetirAsync(x => x.Aktiv && !x.Silinib);
        vm.SenedNovleri = novler.Select(n => new DropdownItemVM { Id = n.Id, Ad = n.Ad }).ToList();

        var tagler = await _uow.Repository<Tag>().HamisiniGetirAsync(x => !x.Silinib);
        vm.Tagler = tagler.Select(t => new DropdownItemVM { Id = t.Id, Ad = t.Ad }).ToList();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YeniSenedNovu([FromBody] SenedNovuElaveEtRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Kod) || string.IsNullOrWhiteSpace(request.Ad))
            return Json(new { success = false, message = "Kod və ad boş ola bilməz." });

        var sobe = await _uow.Repository<Sobe>()
            .GetirAsync(x => x.Id == request.SobeId && x.Aktiv && !x.Silinib);

        if (sobe is null)
            return Json(new { success = false, message = "Şöbə tapılmadı." });

        var exists = await _uow.Repository<SenedNovu>()
            .GetirAsync(x => x.SobeId == request.SobeId && x.Kod == request.Kod.ToUpper() && !x.Silinib);

        if (exists != null)
            return Json(new { success = false, message = "Bu kod artıq mövcuddur." });

        var entity = new SenedNovu
        {
            SobeId = request.SobeId,
            Kod = request.Kod.Trim().ToUpper(),
            Ad = request.Ad.Trim(),
            Aktiv = true,
            YaradanIcraciId = GetUserId()
        };

        await _uow.Repository<SenedNovu>().YaratAsync(entity);
        await _uow.YaddaSaxlaAsync();

        return Json(new
        {
            success = true,
            id = entity.Id,
            ad = entity.Ad
        });
    }

    // GET: /SenedDovriyyesi/Sened/SenedNovleri
    public async Task<IActionResult> SenedNovleri()
    {
        ViewData["Title"] = "Sənəd Növləri";
        ViewData["ActivePage"] = "SenedNovleri";

        var sobeler = await _uow.Repository<Sobe>().HamisiniGetirAsync(x => x.Aktiv && !x.Silinib);
        var novler = await _uow.Repository<SenedNovu>().HamisiniGetirAsync(x => !x.Silinib);

        var vm = new SenedNovleriVM
        {
            Sobeler = sobeler.Select(s => new DropdownItemVM { Id = s.Id, Ad = s.Ad }).ToList(),
            Novler = novler.Select(n => new SenedNovuItemVM
            {
                Id = n.Id,
                Kod = n.Kod,
                Ad = n.Ad,
                SobeId = n.SobeId,
                SobeAd = sobeler.FirstOrDefault(s => s.Id == n.SobeId)?.Ad ?? "-",
                Aktiv = n.Aktiv,
                YaradilmaTarixi = n.YaradilmaTarixi
            }).ToList()
        };

        return View(vm);
    }

    // POST: /SenedDovriyyesi/Sened/SenedNovuSil
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SenedNovuSil(int id)
    {
        var entity = await _uow.Repository<SenedNovu>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (entity is null)
        {
            TempData["Error"] = "Sənəd növü tapılmadı.";
            return RedirectToAction("SenedNovleri");
        }

        entity.Silinib = true;
        entity.SilinmeTarixi = DateTime.Now;
        entity.SilenIcraciId = GetUserId();
        await _uow.YaddaSaxlaAsync();

        TempData["Success"] = "Sənəd növü silindi.";
        return RedirectToAction("SenedNovleri");
    }

    // POST: /SenedDovriyyesi/Sened/SenedNovuAktivDeyis
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SenedNovuAktivDeyis(int id)
    {
        var entity = await _uow.Repository<SenedNovu>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (entity is null)
        {
            TempData["Error"] = "Sənəd növü tapılmadı.";
            return RedirectToAction("SenedNovleri");
        }

        entity.Aktiv = !entity.Aktiv;
        entity.YenileyenIcraciId = GetUserId();
        entity.YenilenmeTarixi = DateTime.Now;
        await _uow.YaddaSaxlaAsync();

        TempData["Success"] = entity.Aktiv ? "Sənəd növü aktivləşdirildi." : "Sənəd növü deaktiv edildi.";
        return RedirectToAction("SenedNovleri");
    }

    public class SenedNovuElaveEtRequest
    {
        public int SobeId { get; set; }
        public string Kod { get; set; } = null!;
        public string Ad { get; set; } = null!;
    }


}
