using FinNex.Application.DTOs.HR.IcraciNo;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
public class IcraciNoController : Controller
{
    private readonly IIcraciNoService _service;

    public IcraciNoController(IIcraciNoService service)
    {
        _service = service;
    }

    // GET: /HR/IcraciNo
    public async Task<IActionResult> Index()
    {
        var model = await _service.HamisiniGetirAsync();
        return View(model);
    }

    // POST: /HR/IcraciNo/Saxla — toplu təyinat
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Saxla(List<IcraciNoTeyinDto> teyinler)
    {
        var res = await _service.TopluTeyinEtAsync(teyinler);
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }
}
