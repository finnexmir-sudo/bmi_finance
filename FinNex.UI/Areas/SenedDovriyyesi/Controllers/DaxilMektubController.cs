using System.Security.Claims;
using FinNex.Application.DTOs.Mektub;
using FinNex.Application.Interfaces.Mektub;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.SenedDovriyyesi.Controllers;

[Area("SenedDovriyyesi")]
[Authorize]
public class DaxilMektubController : Controller
{
    private readonly IDaxilMektubService _service;

    public DaxilMektubController(IDaxilMektubService service)
    {
        _service = service;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin() => User.IsInRole(RoleNames.Admin);

    // GET: /SenedDovriyyesi/DaxilMektub
    public async Task<IActionResult> Index(int? il)
    {
        var model = await _service.HamisiniGetirAsync(il);
        ViewBag.Il = il;
        ViewBag.UserId = GetUserId();
        ViewBag.IsAdmin = IsAdmin();
        return View(model);
    }

    // GET: /SenedDovriyyesi/DaxilMektub/Yarat
    [HttpGet]
    public IActionResult Yarat() =>
        View(new DaxilMektubCreateDto { DaxTarix = DateTime.Today });

    // POST: /SenedDovriyyesi/DaxilMektub/Yarat
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(DaxilMektubCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.IdareAdi))
        {
            TempData["Error"] = "Göndərən idarə/təşkilat adı boş ola bilməz.";
            return View(dto);
        }

        var res = await _service.YaratAsync(dto, GetUserId());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }
}
