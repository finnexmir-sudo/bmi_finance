using FinNex.Application.DTOs.Pid;
using FinNex.Application.Interfaces.Pid;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize(Roles = "Admin,PID")]
public class MehkemeIsiController : Controller
{
    private readonly IMehkemeIsiService _service;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _config;

    public MehkemeIsiController(
        IMehkemeIsiService service,
        UserManager<AppUser> userManager,
        IConfiguration config)
    {
        _service = service;
        _userManager = userManager;
        _config = config;
    }

    private async Task<int?> CurrentIsciIdAsync()
    {
        var u = await _userManager.GetUserAsync(User);
        return u?.IsciId;
    }

    private string DmsRoot =>
        _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";

    // ── Siyahı ────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var list = await _service.HamisiniGetirAsync();
        return View(list);
    }

    // ── Yarat formu ───────────────────────────────────────
    public IActionResult Yarat() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(MehkemeIsiCreateDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        if (string.IsNullOrWhiteSpace(dto.QeydiyyatNomresi) || string.IsNullOrWhiteSpace(dto.BorcluAd))
        {
            ModelState.AddModelError("", "Qeydiyyat nömrəsi və borclu adı məcburidir.");
            return View(dto);
        }

        var isciId = await CurrentIsciIdAsync() ?? 0;
        var entity = await _service.YaratAsync(dto, isciId);
        TempData["Success"] = "Məhkəmə işi yaradıldı.";
        return RedirectToAction("Detal", new { id = entity.Id });
    }

    // ── Detal ─────────────────────────────────────────────
    public async Task<IActionResult> Detal(int id)
    {
        var model = await _service.DetailGetirAsync(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // ── Yenilə (inline, AJAX) ─────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yenile(int id, MehkemeIsiUpdateDto dto)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        var ok = await _service.YenileAsync(id, dto, isciId);
        if (!ok) return Json(new { success = false, message = "Tapılmadı." });
        return Json(new { success = true, message = "Yeniləndi." });
    }

    // ── Sil ───────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        var ok = await _service.SilAsync(id, isciId);
        TempData[ok ? "Success" : "Error"] = ok ? "Silindi." : "Tapılmadı.";
        return RedirectToAction("Index");
    }

    // ── Oracle axtarış ────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> OracleAxtar(string nomre)
    {
        if (string.IsNullOrWhiteSpace(nomre))
            return Json(new { success = false, message = "Nömrə daxil edin." });

        var r = await _service.OracleIleAxtarAsync(nomre);
        if (!r.Tapildi)
            return Json(new { success = false, message = r.Xeta ?? "Tapılmadı." });

        return Json(new { success = true, borcluAd = r.BorcluAd, esasBorc = r.EsasBorc });
    }

    // ── Mərhələ əlavə et ─────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MerheleElave(MehkemeMerheleCreateDto dto, IFormFile? fayl)
    {
        if (dto.MehkemeIsiId <= 0)
            return Json(new { success = false, message = "İş ID tapılmadı." });

        var isciId = await CurrentIsciIdAsync() ?? 0;
        await _service.MerheleElavEtAsync(dto, fayl, DmsRoot, isciId);
        TempData["Success"] = "Mərhələ əlavə edildi.";
        return RedirectToAction("Detal", new { id = dto.MehkemeIsiId });
    }

    // ── Mərhələ sil ───────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MerheleSil(int merheleId, int ishId)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        await _service.MerheleSilAsync(merheleId, isciId);
        return RedirectToAction("Detal", new { id = ishId });
    }
}
