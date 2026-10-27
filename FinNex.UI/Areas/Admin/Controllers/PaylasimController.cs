using FinNex.Application.DTOs.Paylasim;
using FinNex.Application.Interfaces.Paylasim;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FinNex.UI.Areas.Admin.Controllers;

/// <summary>
/// "Paylaşımlar" — Admin xaricdən aldığı mənalı məlumatı rich-text (şəkilli)
/// formatda işçilərlə bölüşür (27.10.2026, pilot). Elan-dan AYRI modul.
/// </summary>
[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class PaylasimController : Controller
{
    private readonly IPaylasimService _paylasim;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _config;

    private static readonly string[] SekilTipler = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxSekilOlcusu = 10 * 1024 * 1024; // 10 MB

    public PaylasimController(IPaylasimService paylasim, UserManager<AppUser> userManager, IConfiguration config)
    {
        _paylasim = paylasim;
        _userManager = userManager;
        _config = config;
    }

    // ── GET /Admin/Paylasim ────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Paylaşımlar";
        var lista = await _paylasim.HamisiniGetirAsync();
        return View(lista);
    }

    // ── GET /Admin/Paylasim/Yarat ───────────────────────────────────
    public IActionResult Yarat()
    {
        ViewData["Title"] = "Yeni Paylaşım";
        return View(new PaylasimYaratDto());
    }

    // ── POST /Admin/Paylasim/Yarat ──────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(PaylasimYaratDto dto)
    {
        var isciId = await GetCurrentIsciIdAsync();
        if (isciId == null)
        {
            TempData["Error"] = "İşçi profiliniz tapılmadı.";
            return RedirectToAction(nameof(Index));
        }

        var netice = await _paylasim.YaratAsync(dto, isciId.Value);
        if (!netice.Success)
        {
            TempData["Error"] = netice.Message;
            return View(dto);
        }

        TempData["Success"] = netice.Message;
        return RedirectToAction(nameof(Index));
    }

    // ── GET /Admin/Paylasim/Redakte/5 ───────────────────────────────
    public async Task<IActionResult> Redakte(int id)
    {
        var post = await _paylasim.DetailGetirAsync(id);
        if (post == null)
        {
            TempData["Error"] = "Paylaşım tapılmadı.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "Paylaşımı Redaktə Et";
        ViewBag.Id = id;
        return View(new PaylasimYaratDto { Basliq = post.Basliq, MetnHtml = post.MetnHtml });
    }

    // ── POST /Admin/Paylasim/Redakte/5 ──────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Redakte(int id, PaylasimYaratDto dto)
    {
        var netice = await _paylasim.YenileAsync(id, dto);
        TempData[netice.Success ? "Success" : "Error"] = netice.Message;
        if (!netice.Success)
        {
            ViewBag.Id = id;
            return View(dto);
        }
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Admin/Paylasim/AktivlikDeyis/5 ────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AktivlikDeyis(int id)
    {
        var netice = await _paylasim.AktivlikDeyisAsync(id);
        TempData[netice.Success ? "Success" : "Error"] = netice.Message;
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Admin/Paylasim/Sil/5 ───────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var netice = await _paylasim.SilAsync(id);
        TempData[netice.Success ? "Success" : "Error"] = netice.Message;
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Admin/Paylasim/SekilYukle (AJAX, Quill image handler) ──
    [HttpPost]
    public async Task<IActionResult> SekilYukle(IFormFile? fayl)
    {
        if (fayl == null || fayl.Length == 0)
            return Json(new { ok = false, xeta = "Şəkil seçilməyib." });

        var ext = Path.GetExtension(fayl.FileName).ToLowerInvariant();
        if (!SekilTipler.Contains(ext))
            return Json(new { ok = false, xeta = "Yalnız JPG, PNG, GIF, WEBP şəkillər qəbul olunur." });

        if (fayl.Length > MaxSekilOlcusu)
            return Json(new { ok = false, xeta = "Şəkil ölçüsü 10 MB-dan çox ola bilməz." });

        var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
        var dir = Path.Combine(dmsRoot, "paylasim-sekiller");
        Directory.CreateDirectory(dir);

        var uniqueName = $"{Guid.NewGuid()}{ext}";
        await using (var fs = System.IO.File.Create(Path.Combine(dir, uniqueName)))
            await fayl.CopyToAsync(fs);

        return Json(new { ok = true, url = $"/dms/paylasim-sekiller/{uniqueName}" });
    }

    private async Task<int?> GetCurrentIsciIdAsync()
    {
        var appUser = await _userManager.GetUserAsync(User);
        return appUser?.IsciId;
    }
}
