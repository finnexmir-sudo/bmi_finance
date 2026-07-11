using FinNex.Application.Interfaces.Emeliyyat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Emeliyyat.Controllers;

[Area("Emeliyyat")]
[Authorize]
public class DashboardController : Controller
{
    private readonly IKocurmeService _pul;
    private readonly ITelebeKocurmeService _telebe;

    public DashboardController(IKocurmeService pul, ITelebeKocurmeService telebe)
    {
        _pul = pul;
        _telebe = telebe;
    }

    public async Task<IActionResult> Index()
    {
        var il = DateTime.Now.Year;
        var pul = await _pul.HamisiniGetirAsync("Pul", il);
        var telebe = await _telebe.HamisiniGetirAsync(il);

        ViewBag.Il = il;
        ViewBag.PulSay = pul.Count;
        ViewBag.TelebeSay = telebe.Count;
        ViewBag.PulMebleg = pul.Where(x => x.Mebleg.HasValue).Sum(x => x.Mebleg!.Value);
        ViewBag.TelebeMebleg = telebe.Where(x => x.Mebleg.HasValue).Sum(x => x.Mebleg!.Value);
        return View();
    }
}
