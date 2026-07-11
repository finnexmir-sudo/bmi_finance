using System.Security.Claims;
using FinNex.Application.DTOs.Hevale;
using FinNex.Application.Interfaces.Hevale;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FinNex.UI.Areas.Emeliyyat.Controllers;

[Area("Emeliyyat")]
[Authorize]
public class GelenHevaleController : Controller
{
    private readonly IGelenHevaleService _service;
    private readonly IConfiguration _config;

    public GelenHevaleController(IGelenHevaleService service, IConfiguration config)
    {
        _service = service;
        _config = config;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin() => User.IsInRole(RoleNames.Admin);

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

    public async Task<IActionResult> Index(int? il)
    {
        var model = await _service.HamisiniGetirAsync(il);
        ViewBag.Il = il;
        ViewBag.UserId = GetUserId();
        ViewBag.IsAdmin = IsAdmin();
        return View(model);
    }

    [HttpGet]
    public IActionResult Yarat() =>
        View(new GelenHevaleCreateDto { Tarix = DateTime.Today, ValTip = "AZN" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Yarat(GelenHevaleCreateDto dto, IFormFile? fayl)
    {
        if (string.IsNullOrWhiteSpace(dto.Saa))
        {
            TempData["Error"] = "Soyad Ad Ata (S.A.A.) boş ola bilməz.";
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
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Redakte(GelenHevaleEditDto dto, IFormFile? fayl)
    {
        var yeniFaylYolu = await QosmaYazAsync(fayl);
        var res = await _service.YenileAsync(dto, GetUserId(), IsAdmin(), yeniFaylYolu);
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        if (!res.Success)
            return View(dto);
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
