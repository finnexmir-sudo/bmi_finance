using System.Security.Claims;
using FinNex.Application.DTOs.Emeliyyat;
using FinNex.Application.Interfaces.Emeliyyat;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Emeliyyat.Controllers;

// Pul köçürməsi və Tələbə köçürməsi üçün ortaq baza — Novu ilə ayrılır.
[Area("Emeliyyat")]
[Authorize]
public abstract class KocurmeControllerBase : Controller
{
    protected readonly IKocurmeService _service;
    protected KocurmeControllerBase(IKocurmeService service) => _service = service;

    protected abstract string Novu { get; }     // "Pul" / "Telebe"
    protected abstract string Baslik { get; }    // səhifə başlığı

    private const string V = "~/Areas/Emeliyyat/Views/Kocurme/";

    protected int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected bool IsAdmin() => User.IsInRole(RoleNames.Admin);

    private void Baza()
    {
        ViewBag.Baslik = Baslik;
        ViewBag.Novu = Novu;
        ViewBag.UserId = GetUserId();
        ViewBag.IsAdmin = IsAdmin();
    }

    public async Task<IActionResult> Index(int? il)
    {
        var model = await _service.HamisiniGetirAsync(Novu, il);
        ViewBag.Il = il;
        Baza();
        return View($"{V}Index.cshtml", model);
    }

    [HttpGet]
    public IActionResult Yarat()
    {
        Baza();
        return View($"{V}Yarat.cshtml", new KocurmeCreateDto { Tarix = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(KocurmeCreateDto dto)
    {
        var res = await _service.YaratAsync(Novu, dto, GetUserId());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        if (!res.Success)
        {
            Baza();
            return View($"{V}Yarat.cshtml", dto);
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detal(int id)
    {
        var model = await _service.DetalAsync(id, Novu);
        if (model == null)
        {
            TempData["Error"] = "Köçürmə tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        Baza();
        return View($"{V}Detal.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> Redakte(int id)
    {
        var dto = await _service.RedakteMelumatiAsync(id, Novu);
        if (dto == null)
        {
            TempData["Error"] = "Köçürmə tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        if (!IsAdmin() && dto.YaradanId != GetUserId())
        {
            TempData["Error"] = "Bu köçürməni yalnız yaradan və ya Admin dəyişə bilər.";
            return RedirectToAction(nameof(Index));
        }
        Baza();
        return View($"{V}Redakte.cshtml", dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Redakte(KocurmeEditDto dto)
    {
        var res = await _service.YenileAsync(Novu, dto, GetUserId(), IsAdmin());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        if (!res.Success)
        {
            Baza();
            return View($"{V}Redakte.cshtml", dto);
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
