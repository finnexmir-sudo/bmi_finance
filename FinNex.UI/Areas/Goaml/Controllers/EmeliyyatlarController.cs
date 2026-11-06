using FinNex.Application.DTOs.Goaml;
using FinNex.Application.Interfaces.Goaml;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Goaml.Controllers;

/// <summary>
/// GOAML — "Əməliyyatlar" tabı. Namizəd Oracle-dan OXUNUR (yalnız SELECT),
/// operator seçib "Təsdiqlə" edəndə bizim ÖZ SQL Server-imizə yazılır.
/// </summary>
[Area("Goaml")]
[Authorize]
[Icaze(IcazeKodlari.GoamlBax)]
public class EmeliyyatlarController : Controller
{
    private readonly IGoamlService _goaml;
    private readonly UserManager<AppUser> _userManager;

    public EmeliyyatlarController(IGoamlService goaml, UserManager<AppUser> userManager)
    {
        _goaml = goaml;
        _userManager = userManager;
    }

    private async Task<int?> GetCurrentIsciIdAsync()
    {
        var appUser = await _userManager.GetUserAsync(User);
        return appUser?.IsciId;
    }

    // ── GET /Goaml/Emeliyyatlar — axtarış ekranı ───────────────────────────
    public IActionResult Index()
    {
        ViewData["Title"] = "Əməliyyatlar";
        return View(new GoamlAxtarisDto
        {
            BasTarix = DateTime.Today,
            SonTarix = DateTime.Today,
        });
    }

    // ── POST /Goaml/Emeliyyatlar/Axtar — AJAX, namizəd siyahısı ────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Axtar([FromForm] GoamlAxtarisDto dto)
    {
        if (dto.SonTarix < dto.BasTarix)
            return Json(new { success = false, message = "Bitmə tarixi başlanğıcdan əvvəl ola bilməz." });

        var netice = await _goaml.NamizedAxtarAsync(dto);
        if (!netice.Success)
            return Json(new { success = false, message = netice.Message });

        return Json(new { success = true, setirler = netice.Data });
    }

    // ── GET /Goaml/Emeliyyatlar/Tesdiq — seçilmiş namizəd üçün KYC forması ──
    public IActionResult Tesdiq(int filialNumber, DateTime dateOper, long recNum,
        string? debet, string? kredit, decimal? summaVNacval, int? kodValuti,
        string? innDebet, string? innKredit, string? primechanie,
        string? nameLicschDe, string? nameLicschKre)
    {
        ViewData["Title"] = "GOAML — Təsdiqlə";

        var dto = new GoamlTesdiqDto
        {
            CandidateFilialNumber = filialNumber,
            CandidateDateOper = dateOper,
            CandidateRecNum = recNum,
            Debet = debet,
            Kredit = kredit,
            SummaVNacval = summaVNacval,
            KodValuti = kodValuti,
            Primechanie = primechanie,
            DateOper = dateOper,
            NameUr = nameLicschDe,
            ToAdi = nameLicschKre,
        };

        return View(dto);
    }

    // ── POST /Goaml/Emeliyyatlar/Tesdiq — KYC formunu yaddaşa yazır ─────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tesdiq(GoamlTesdiqDto dto)
    {
        var isciId = await GetCurrentIsciIdAsync();
        if (isciId == null)
        {
            TempData["Error"] = "İşçi profiliniz tapılmadı.";
            return View(dto);
        }

        var netice = await _goaml.TesdiqleAsync(dto, isciId.Value);
        if (!netice.Success)
        {
            TempData["Error"] = netice.Message;
            return View(dto);
        }

        TempData["Success"] = netice.Message;
        return RedirectToAction(nameof(Siyahi));
    }

    // ── GET /Goaml/Emeliyyatlar/Siyahi — təsdiqlənmiş qeydlər ───────────────
    public async Task<IActionResult> Siyahi()
    {
        ViewData["Title"] = "GOAML — Təsdiqlənmiş əməliyyatlar";
        var netice = await _goaml.HamisiniGetirAsync();
        return View(netice.Data ?? new List<GoamlEmeliyyatListDto>());
    }
}
