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
        if (musteriler ==null)
        {
            return NotFound();

        }
        return View(musteriler.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Yarat(string? returnUrl = null,
                                        string? ad = null,
                                        string? voen = null,
                                        string? hesab = null)
    {
        var valyutalar = await _valyutaService.GetAktivAsync();
        ViewBag.Valyutalar = valyutalar.Select(v => new SelectListItem
        {
            Value = v.Id.ToString(),
            Text = v.Kod
        }).ToList();

        ViewBag.ReturnUrl = returnUrl;

        // Gələn məlumatları forma doldur
        return View(new MusteriCreateDto
        {
            Ad = ad,
            Voen = voen,
            Hesab = hesab
        });
    }

    [HttpPost]
    public async Task<IActionResult> Yarat(MusteriCreateDto dto, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            var valyutalar = await _valyutaService.GetAktivAsync();
            ViewBag.Valyutalar = valyutalar.Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = v.Kod
            }).ToList();
            ViewBag.ReturnUrl = returnUrl;
            return View(dto);
        }

        await _uow.YaratAsync(dto);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            // Məlumatları TempData-ya yaz
            TempData["AlanMusteri_Ad"] = dto.Ad;
            TempData["AlanMusteri_Voen"] = dto.Voen;
            TempData["AlanMusteri_Hesab"] = dto.Hesab;
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

}
