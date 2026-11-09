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
    public async Task<IActionResult> Tesdiq(int filialNumber, DateTime dateOper, long recNum,
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

        // ── "Sistem gətirə bilmirsə, işçi doldurur" (09.11.2026, istifadəçi
        //    qərarı) — bizim bankın hesabı olanın sahibi fiziki şəxsdirsə
        //    CON+SIG-in hər ikisinə, hüquqi şəxsdirsə YALNIZ SIG-ə ("Rəhbər")
        //    avtomatik yazılır. CON-u hüquqi şəxsdə operator özü seçir —
        //    ViewBag.ElaqaliSexsler ilə görüntüyə ötürülür. ────────────────
        var kyc = await _goaml.HesabSahibiKycTapAsync(filialNumber, debet, kredit);
        if (kyc?.TekSexs != null)
        {
            if (kyc.Fiziki)
            {
                dto.ConAdi = kyc.TekSexs.Adi;
                dto.ConSoyadi = kyc.TekSexs.Soyadi;
                dto.ConAtasininAdi = kyc.TekSexs.AtaAdi;
                dto.ConDogulduguTarix = kyc.TekSexs.DogulduguTarix;
                dto.ConDogulduguYer = kyc.TekSexs.DogulduguYer;
                dto.ConCinsi = kyc.TekSexs.Cinsi;
                dto.ConUnvan = kyc.TekSexs.Unvan;
                dto.ConVetendasligi = kyc.TekSexs.Vetendasligi;
                dto.ConPin = kyc.TekSexs.Pin;
                dto.ConSenedinSeriyaNomresi = kyc.TekSexs.SenedinSeriyaNomresi;
                dto.ConSenedinVerilmeTarixi = kyc.TekSexs.SenedinVerilmeTarixi;
                dto.ConSenedinEtibarliliqMuddeti = kyc.TekSexs.SenedinEtibarliliqMuddeti;
                dto.ConVerenOrqan = kyc.TekSexs.VerenOrqan;
            }

            dto.SigAdi = kyc.TekSexs.Adi;
            dto.SigSoyadi = kyc.TekSexs.Soyadi;
            dto.SigAtasininAdi = kyc.TekSexs.AtaAdi;
            dto.SigDogulduguTarix = kyc.TekSexs.DogulduguTarix;
            dto.SigDogulduguYer = kyc.TekSexs.DogulduguYer;
            dto.SigVetendasligi = kyc.TekSexs.Vetendasligi;
            dto.SigUnvan = kyc.TekSexs.Unvan;
            dto.SigSenedinNovu = kyc.TekSexs.SenedinNovu;
            dto.SigSenedinSeriyaNomresi = kyc.TekSexs.SenedinSeriyaNomresi;
            dto.SigSenedinVerilmeTarixi = kyc.TekSexs.SenedinVerilmeTarixi;
            dto.SigSenedinEtibarliliqMuddeti = kyc.TekSexs.SenedinEtibarliliqMuddeti;
            dto.SigVerenOrqan = kyc.TekSexs.VerenOrqan;
            dto.SigVezife = kyc.TekSexs.Vezife;
            dto.SigIsYeriAdi = kyc.TekSexs.IsYeriAdi;
        }

        ViewBag.ElaqaliSexsler = kyc != null && !kyc.Fiziki ? kyc.ElaqaliSexsler : new List<GoamlElaqaliSexsDto>();
        ViewBag.AvtoDolduruldu = kyc?.TekSexs != null;

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

    // ── GET /Goaml/Emeliyyatlar/Detal/{id} — TƏK qeydin tam (sırf oxunuş) detalı ──
    public async Task<IActionResult> Detal(int id)
    {
        ViewData["Title"] = "GOAML — Əməliyyat detalı";
        var netice = await _goaml.DetalGetirAsync(id);
        if (!netice.Success || netice.Data == null)
        {
            TempData["Error"] = netice.Message ?? "Qeyd tapılmadı.";
            return RedirectToAction(nameof(Siyahi));
        }

        return View(netice.Data);
    }
}
