using FinNex.Application.DTOs.Kredit.Muqavile;
using FinNex.Application.Interfaces.Kredit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class KreditMuqavileController : Controller
{
    private readonly IKreditMuqavileService _muqavileService;

    // Müqavilə tipləri — BMI-dəki "Müqavilə tipi" dropdown ilə eyni.
    public static readonly string[] MuqavileTipleri =
    {
        "Avtomobil", "Zaminlik", "Daşınmaz Əmlak", "Qızıl girovu", "Əlavə"
    };

    public KreditMuqavileController(IKreditMuqavileService muqavileService)
    {
        _muqavileService = muqavileService;
    }

    // Səviyyə 1 — Seçim səhifəsi: tarixə görə verilmiş kreditlərin siyahısı.
    public async Task<IActionResult> Index(DateTime? tarix, string? tip)
    {
        var seciliTarix = tarix ?? DateTime.Today;

        var kreditler = new List<KreditMuqavileSatirDto>();
        try
        {
            kreditler = await _muqavileService.KreditleriGetirAsync(seciliTarix);
        }
        catch (Exception ex)
        {
            ViewBag.Xeta = "Oracle-dan məlumat alınmadı: " + ex.Message;
        }

        ViewBag.SeciliTarix = seciliTarix;
        ViewBag.SeciliTip = MuqavileTipleri.Contains(tip) ? tip : "Daşınmaz Əmlak";
        ViewBag.Tipler = MuqavileTipleri;

        return View(kreditler);
    }
}
