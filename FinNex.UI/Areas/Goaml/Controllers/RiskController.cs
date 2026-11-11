using FinNex.Application.DTOs.Goaml;
using FinNex.Application.Interfaces.Goaml;
using FinNex.Domain;
using FinNex.UI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Goaml.Controllers;

/// <summary>
/// GOAML — "Risk" tabı. Namizəd `ODB.DOC_DNA_ALL_RISK`-dən (ölkə/qara siyahı/
/// hesab-sahibi vətəndaşlığı əsaslı kriteriya ilə) OXUNUR — Oracle-a HEÇ NƏ
/// yazılmır. "Təsdiqlə"/"Təsdiqlənmiş siyahı"/"Detal"/"Sil" Əməliyyatlar
/// tabı ilə EYNİ, ORTAQ axındır (hər iki tabın namizədi eyni
/// <see cref="FinNex.Application.DTOs.Goaml.GoamlNamizedSetirDto"/> formasındadır,
/// eyni <see cref="FinNex.Domain.Entities.Goaml.GoamlEmeliyyat"/> cədvəlinə
/// yazılır) — bu controller yalnız ÖZ namizəd-axtarışını daşıyır, Tesdiq/Siyahi/
/// Detal/Sil-i TƏKRARLAMIR, `EmeliyyatlarController`-ə yönləndirir.
/// </summary>
[Area("Goaml")]
[Authorize]
[Icaze(IcazeKodlari.GoamlBax)]
public class RiskController : Controller
{
    private readonly IGoamlService _goaml;

    public RiskController(IGoamlService goaml)
    {
        _goaml = goaml;
    }

    // ── GET /Goaml/Risk — axtarış ekranı ────────────────────────────────────
    public IActionResult Index()
    {
        ViewData["Title"] = "GOAML — Risk";
        return View(new GoamlAxtarisDto
        {
            BasTarix = DateTime.Today,
            SonTarix = DateTime.Today,
        });
    }

    // ── POST /Goaml/Risk/Axtar — AJAX, namizəd siyahısı ─────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Axtar([FromForm] GoamlAxtarisDto dto)
    {
        if (dto.SonTarix < dto.BasTarix)
            return Json(new { success = false, message = "Bitmə tarixi başlanğıcdan əvvəl ola bilməz." });

        var netice = await _goaml.RiskNamizedAxtarAsync(dto);
        if (!netice.Success)
            return Json(new { success = false, message = netice.Message });

        return Json(new { success = true, setirler = netice.Data });
    }
}
