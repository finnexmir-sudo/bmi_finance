using System.Security.Claims;
using FinNex.Application.DTOs.Mektub;
using FinNex.Application.Interfaces.Mektub;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FinNex.UI.Areas.SenedDovriyyesi.Controllers;

[Area("SenedDovriyyesi")]
[Authorize]
public class XaricMektubController : Controller
{
    private readonly IXaricMektubService _service;
    private readonly IConfiguration _config;

    public XaricMektubController(IXaricMektubService service, IConfiguration config)
    {
        _service = service;
        _config = config;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin() => User.IsInRole(RoleNames.Admin);

    // GET: /SenedDovriyyesi/XaricMektub
    public async Task<IActionResult> Index(int? il)
    {
        var model = await _service.HamisiniGetirAsync(il);
        ViewBag.Il = il;
        ViewBag.UserId = GetUserId();
        ViewBag.IsAdmin = IsAdmin();
        return View(model);
    }

    // GET: /SenedDovriyyesi/XaricMektub/Yarat
    [HttpGet]
    public IActionResult Yarat() =>
        View(new XaricMektubCreateDto { Tarix = DateTime.Today });

    // POST: /SenedDovriyyesi/XaricMektub/Yarat
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Yarat(XaricMektubCreateDto dto, IFormFile? fayl)
    {
        if (string.IsNullOrWhiteSpace(dto.GonYer))
        {
            TempData["Error"] = "Göndərilən yer (təyinat) boş ola bilməz.";
            return View(dto);
        }

        // İstəyə bağlı qoşma — DMS-ə (C:\FinNex_DMS\mektublar\), DB-yə yalnız nisbi yol
        string? faylYolu = null;
        if (fayl != null && fayl.Length > 0)
        {
            var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
            var dir = Path.Combine(dmsRoot, "mektublar");
            Directory.CreateDirectory(dir);
            var ext = Path.GetExtension(fayl.FileName);
            var ad = $"{Guid.NewGuid()}{ext}";
            await using var fs = new FileStream(Path.Combine(dir, ad), FileMode.Create);
            await fayl.CopyToAsync(fs);
            faylYolu = $"mektublar/{ad}";
        }

        var res = await _service.YaratAsync(dto, GetUserId(), faylYolu);
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }
}
