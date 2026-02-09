using Microsoft.AspNetCore.Mvc;
using FinNex.Domain.Interfaces;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using Microsoft.AspNetCore.Authorization;

namespace FinNex.UI.Controllers;

[Authorize]
public class MusteriController : Controller
{
    private readonly IUnitOfWork _uow;

    public MusteriController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IActionResult> Index()
    {
        var musteriler = await _uow.Repository<Musteri>().HamisiniGetirAsync();
        return View(musteriler);
    }

    public IActionResult Yarat() => View();

    [HttpPost]
    public async Task<IActionResult> Yarat(Musteri musteri)
    {
        if (ModelState.IsValid)
        {
            await _uow.Repository<Musteri>().YaratAsync(musteri);
            await _uow.YaddaSaxlaAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(musteri);
    }
}
