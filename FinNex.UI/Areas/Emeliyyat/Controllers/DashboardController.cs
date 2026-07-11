using FinNex.Application.Interfaces.Hevale;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Emeliyyat.Controllers;

[Area("Emeliyyat")]
[Authorize]
public class DashboardController : Controller
{
    private readonly IGedenHevaleService _geden;
    private readonly IGelenHevaleService _gelen;

    public DashboardController(IGedenHevaleService geden, IGelenHevaleService gelen)
    {
        _geden = geden;
        _gelen = gelen;
    }

    public async Task<IActionResult> Index()
    {
        var il = DateTime.Now.Year;
        var geden = await _geden.HamisiniGetirAsync(il);
        var gelen = await _gelen.HamisiniGetirAsync(il);

        ViewBag.Il = il;
        ViewBag.GedenSay = geden.Count;
        ViewBag.GelenSay = gelen.Count;
        ViewBag.GedenMebleg = geden.Where(x => x.Mebleg.HasValue).Sum(x => x.Mebleg!.Value);
        ViewBag.GelenMebleg = gelen.Where(x => x.Mebleg.HasValue).Sum(x => x.Mebleg!.Value);
        return View();
    }
}
