using System.Security.Claims;
using FinNex.Application.DTOs.Hevale;
using FinNex.Application.Interfaces.Hevale;
using FinNex.Application.Interfaces.Kurval;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FinNex.UI.Areas.SenedDovriyyesi.Controllers;

[Area("SenedDovriyyesi")]
[Authorize]
public class GedenHevaleController : Controller
{
    private readonly IGedenHevaleService _service;
    private readonly IConfiguration _config;
    private readonly IBmiValyutaService _valyuta;

    public GedenHevaleController(
        IGedenHevaleService service,
        IConfiguration config,
        IBmiValyutaService valyuta)
    {
        _service = service;
        _config = config;
        _valyuta = valyuta;
    }

    // Valyuta siyahısı BMI `kurval`-dan gəlir (kod + ad).
    // FORMA QAYTARILAN HƏR YOLDA çağırılmalıdır — POST xətasında da.
    // Unudulsa view-dakı ViewBag.Valyutalar null olar və səhifə sınar.
    private async Task ValyutalariDoldurAsync(CancellationToken ct = default)
        => ViewBag.Valyutalar = await _valyuta.SiyahiAsync(ct);

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin() => User.IsInRole(RoleNames.Admin);

    // İstəyə bağlı qoşmanı DMS-ə yazır, nisbi yolu qaytarır (fayl yoxdursa null)
    private async Task<string?> QosmaYazAsync(IFormFile? fayl)
    {
        if (fayl == null || fayl.Length == 0) return null;
        var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
        var dir = Path.Combine(dmsRoot, "hevaleler");
        Directory.CreateDirectory(dir);
        var ad = $"{Guid.NewGuid()}{Path.GetExtension(fayl.FileName)}";
        await using var fs = new FileStream(Path.Combine(dir, ad), FileMode.Create);
        await fayl.CopyToAsync(fs);
        return $"hevaleler/{ad}";
    }

    public async Task<IActionResult> Index(HevaleFiltrDto filtr)
    {
        var f = HevaleFiltrDto.Normalla(filtr);
        var model = await _service.HamisiniGetirAsync(f);

        ViewBag.Filtr   = f;
        ViewBag.Menbe   = await _service.FiltrMenbeleriAsync();
        ViewBag.UserId  = GetUserId();
        ViewBag.IsAdmin = IsAdmin();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Yarat(CancellationToken ct)
    {
        await ValyutalariDoldurAsync(ct);
        // Defolt valyuta — BMI kodu "00" (AZƏRBAYCAN MANATI).
        // Əvvəl "AZN" mətni idi; indi seçilən dəyər KODDUR (kurval.SOKNAMEVALUT).
        return View(new GedenHevaleCreateDto { Tarix = DateTime.Today, ValTip = "00" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Yarat(GedenHevaleCreateDto dto, IFormFile? fayl)
    {
        if (string.IsNullOrWhiteSpace(dto.Saa))
        {
            TempData["Error"] = "Soyad Ad Ata (S.A.A.) boş ola bilməz.";
            await ValyutalariDoldurAsync();
            return View(dto);
        }
        var faylYolu = await QosmaYazAsync(fayl);
        var res = await _service.YaratAsync(dto, GetUserId(), faylYolu);
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Redakte(int id)
    {
        var dto = await _service.RedakteMelumatiAsync(id);
        if (dto == null)
        {
            TempData["Error"] = "Həvalə tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        if (!IsAdmin() && dto.YaradanId != GetUserId())
        {
            TempData["Error"] = "Bu həvaləni yalnız yaradan və ya Admin dəyişə bilər.";
            return RedirectToAction(nameof(Index));
        }
        await ValyutalariDoldurAsync();
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Redakte(GedenHevaleEditDto dto, IFormFile? fayl)
    {
        var yeniFaylYolu = await QosmaYazAsync(fayl);
        var res = await _service.YenileAsync(dto, GetUserId(), IsAdmin(), yeniFaylYolu);
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        if (!res.Success)
        {
            await ValyutalariDoldurAsync();
            return View(dto);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var res = await _service.SilAsync(id, GetUserId(), IsAdmin());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }
}
