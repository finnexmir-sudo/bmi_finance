using FinNex.Application.DTOs.Sorgular;
using FinNex.Application.Interfaces.Sorgular;
using FinNex.Application.Interfaces.Structur;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OracleSorguController : Controller
{
    private readonly IOracleSorguService _service;
    private readonly IDepartmentService _deptService;

    public OracleSorguController(IOracleSorguService service, IDepartmentService deptService)
    {
        _service = service;
        _deptService = deptService;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _service.HamisiniGetirAsync();
        return View(result.Data ?? new List<OracleSorguDto>());
    }

    public async Task<IActionResult> Create()
    {
        await YukleSobeler();
        return View(new OracleSorguCreateDto { Aktiv = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OracleSorguCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await YukleSobeler();
            return View(dto);
        }

        var result = await _service.YaratAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message ?? "Xəta baş verdi.");
            await YukleSobeler();
            return View(dto);
        }

        TempData["Success"] = $"«{dto.SorguAdi}» sorğusu əlavə edildi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var result = await _service.IdIleGetirAsync(id);
        if (!result.Success || result.Data == null) return NotFound();

        var dto = new OracleSorguUpdateDto
        {
            Id = result.Data.Id,
            SorguAdi = result.Data.SorguAdi,
            Mahiyyet = result.Data.Mahiyyet,
            SorguMetni = result.Data.SorguMetni,
            Aktiv = result.Data.Aktiv,
            DepartamentId = result.Data.DepartamentId
        };

        await YukleSobeler(dto.DepartamentId);
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OracleSorguUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await YukleSobeler(dto.DepartamentId);
            return View(dto);
        }

        var result = await _service.YenileAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message ?? "Xəta baş verdi.");
            await YukleSobeler(dto.DepartamentId);
            return View(dto);
        }

        TempData["Success"] = $"«{dto.SorguAdi}» sorğusu yeniləndi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var result = await _service.SilAsync(id);
        TempData[result.Success ? "Success" : "Error"] =
            result.Success ? "Sorğu silindi." : result.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task YukleSobeler(int? seciliId = null)
    {
        var sobeler = await _deptService.HamisiniGetirAsync();
        ViewBag.Sobeler = new SelectList(
            sobeler.Data ?? [],
            "Id", "Ad",
            seciliId);
    }
}
