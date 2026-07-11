using FinNex.Application.Interfaces.Emeliyyat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Emeliyyat.Controllers;

[Area("Emeliyyat")]
[Authorize]
public class DashboardController : Controller
{
    private readonly IKocurmeService _service;

    public DashboardController(IKocurmeService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index()
    {
        var il = DateTime.Now.Year;
        var pul = await _service.HamisiniGetirAsync("Pul", il);
        var telebe = await _service.HamisiniGetirAsync("Telebe", il);

        ViewBag.Il = il;
        ViewBag.PulSay = pul.Count;
        ViewBag.TelebeSay = telebe.Count;
        ViewBag.PulMebleg = pul.Where(x => x.Mebleg.HasValue).Sum(x => x.Mebleg!.Value);
        ViewBag.TelebeMebleg = telebe.Where(x => x.Mebleg.HasValue).Sum(x => x.Mebleg!.Value);
        return View();
    }
}
