using Microsoft.AspNetCore.Mvc;
using FinNex.Domain.Interfaces;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using Microsoft.AspNetCore.Authorization;
using FinNex.UI.Areas.PR_Odenis_Tapsirigi.ViewModels;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Musteri;
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.DataAccess.UnitOfWorks;
using FinNex.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.PR_Odenis_Tapsirigi.Controllers;

[Authorize]
[Area("PR_Odenis_Tapsirigi")]
public class MusteriController : Controller
{
    private readonly IMusteriService _uow;
    private readonly IValyutaService _valyutaService;

    public MusteriController(IMusteriService uow, IValyutaService valyutaService)
    {
        _uow = uow;
        _valyutaService = valyutaService;
    }

    public async Task<IActionResult> Index()
    {
        var musteriler = await _uow.HamisiniGetirAsync();
        return View(musteriler);
    }

    [HttpPost]
    [HttpPost]
    public async Task<IActionResult> Yarat(MusteriCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            var valyutalar = await _valyutaService.GetAktivAsync();

            ViewBag.Valyutalar = valyutalar
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = v.Kod
                }).ToList();

            return View(dto);
        }

        await _uow.YaratAsync(dto);

        return RedirectToAction(nameof(Index));
    }


    public async Task<IActionResult> Yarat()
    {
        var valyutalar = await _valyutaService.GetAktivAsync();

        ViewBag.Valyutalar = valyutalar
            .Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = v.Kod
            }).ToList();

        return View(new MusteriCreateDto());
    }

}
