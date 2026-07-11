using System.Security.Claims;
using FinNex.Application.DTOs.Emeliyyat;
using FinNex.Application.Interfaces.Emeliyyat;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Emeliyyat.Controllers;

[Area("Emeliyyat")]
[Authorize]
public class TelebeKocurmeController : Controller
{
    private readonly ITelebeKocurmeService _service;

    public TelebeKocurmeController(ITelebeKocurmeService service)
    {
        _service = service;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin() => User.IsInRole(RoleNames.Admin);

    public async Task<IActionResult> Index(int? il)
    {
        var model = await _service.HamisiniGetirAsync(il);
        ViewBag.Il = il;
        ViewBag.UserId = GetUserId();
        ViewBag.IsAdmin = IsAdmin();
        return View(model);
    }

    [HttpGet]
    public IActionResult Yarat()
    {
        var (h35025, h45023, h45011, h67013) = _service.StandartHesablar();
        return View(new TelebeKocurmeCreateDto
        {
            Tarix = DateTime.Today,
            AlanBank = "Kapital",
            XH = 0.1m,
            Kurs = 1.68m,
            Hes35025 = h35025,
            Hes45023 = h45023,
            Hes45011 = h45011,
            Hes67013 = h67013
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(TelebeKocurmeCreateDto dto)
    {
        var res = await _service.YaratAsync(dto, GetUserId());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        if (!res.Success)
            return View(dto);
        return RedirectToAction(nameof(Detal), new { id = res.Data });
    }

    public async Task<IActionResult> Detal(int id)
    {
        var model = await _service.DetalAsync(id);
        if (model == null)
        {
            TempData["Error"] = "Qeyd tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.UserId = GetUserId();
        ViewBag.IsAdmin = IsAdmin();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Redakte(int id)
    {
        var dto = await _service.RedakteMelumatiAsync(id);
        if (dto == null)
        {
            TempData["Error"] = "Qeyd tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        if (!IsAdmin() && dto.YaradanId != GetUserId())
        {
            TempData["Error"] = "Bu qeydi yalnız yaradan və ya Admin dəyişə bilər.";
            return RedirectToAction(nameof(Index));
        }
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Redakte(TelebeKocurmeEditDto dto)
    {
        var res = await _service.YenileAsync(dto, GetUserId(), IsAdmin());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        if (!res.Success)
            return View(dto);
        return RedirectToAction(nameof(Detal), new { id = dto.Id });
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
