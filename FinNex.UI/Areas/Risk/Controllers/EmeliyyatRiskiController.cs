using System.Security.Claims;
using FinNex.Application.DTOs.Risk;
using FinNex.Application.Helpers.Risk;
using FinNex.Application.Interfaces.Risk;
using FinNex.Domain.Entities.Risk;
using FinNex.UI.Areas.Risk.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.Risk.Controllers;

/// <summary>
/// MB Qərar 04/1, Əlavə 4 — əməliyyat riski hadisələri jurnalı
/// (30.09.2026, istifadəçi tələbi). Təsdiq zənciri yoxdur: Risk/AML
/// şöbəsindəki işçi hadisəni müşahidə edən kimi birbaşa yaradır.
/// </summary>
[Area("Risk")]
[Authorize]
public class EmeliyyatRiskiController : Controller
{
    private readonly IEmeliyyatRiskiHadisesiService _service;
    private readonly IEmeliyyatRiskiParametrleriService _parametrleriService;
    private readonly IIsciService _isciService;

    public EmeliyyatRiskiController(
        IEmeliyyatRiskiHadisesiService service,
        IEmeliyyatRiskiParametrleriService parametrleriService,
        IIsciService isciService)
    {
        _service = service;
        _parametrleriService = parametrleriService;
        _isciService = isciService;
    }

    // ── GET: /Risk/EmeliyyatRiski ────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index(
        BiznesSahesi? biznesSahesi, RiskKateqoriyasi1? riskKateqoriyasi1,
        DateTime? basTarix, DateTime? sonTarix)
    {
        var siyahi = await _service.SiyahiAsync(biznesSahesi, riskKateqoriyasi1, basTarix, sonTarix);

        ViewBag.BiznesSaheleri = BiznesSaheleriSelect(biznesSahesi);
        ViewBag.RiskKateqoriyalari1 = RiskKateqoriyalari1Select(riskKateqoriyasi1);
        ViewBag.BasTarix = basTarix?.ToString("yyyy-MM-dd");
        ViewBag.SonTarix = sonTarix?.ToString("yyyy-MM-dd");

        ViewData["Title"] = "Əməliyyat Riski Hadisələri";
        return View(siyahi);
    }

    // ── GET: /Risk/EmeliyyatRiski/Detail/5 ───────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var dto = await _service.DetalAsync(id);
        if (dto == null) return NotFound("Hadisə tapılmadı.");

        ViewBag.Tarixce = await _service.TarixceAsync(id);
        ViewData["Title"] = $"Hadisə — {dto.QeydiyyatKodu}";
        return View(dto);
    }

    // ── GET: /Risk/EmeliyyatRiski/Create ─────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "Yeni Əməliyyat Riski Hadisəsi";
        return View(await BuildFormVMAsync(new EmeliyyatRiskiHadisesiUpdateDto
        {
            HadiseninBasVerdiyiTarix = DateTime.Today,
            HadiseninMueyyenlesdirilmeTarixi = DateTime.Today
        }));
    }

    // ── POST: /Risk/EmeliyyatRiski/Create ────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmeliyyatRiskiHadisesiUpdateDto dto)
    {
        var icraciId = CariIcraciId();
        var result = await _service.YaratAsync(dto, icraciId);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message ?? "Xəta baş verdi.");
            return View(await BuildFormVMAsync(dto));
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id = result.Data });
    }

    // ── GET: /Risk/EmeliyyatRiski/Edit/5 ─────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _service.DetalAsync(id);
        if (dto == null) return NotFound("Hadisə tapılmadı.");

        var updateDto = new EmeliyyatRiskiHadisesiUpdateDto
        {
            Id = dto.Id,
            StrukturBolme = dto.StrukturBolme,
            MelumatiVerenSexs = dto.MelumatiVerenSexs,
            HadiseninBasVerdiyiTarix = dto.HadiseninBasVerdiyiTarix,
            HadiseninMueyyenlesdirilmeTarixi = dto.HadiseninMueyyenlesdirilmeTarixi,
            MueyyenlesdirenIsciId = dto.MueyyenlesdirenIsciId,
            Tesvir = dto.Tesvir,
            Sebeb = dto.Sebeb,
            TezlikDerecesi = dto.TezlikDerecesi,
            TesirDerecesi = dto.TesirDerecesi,
            BiznesSahesi = dto.BiznesSahesi,
            BankMehsulu = dto.BankMehsulu,
            RiskKateqoriyasi1 = dto.RiskKateqoriyasi1,
            RiskKateqoriyasi2 = dto.RiskKateqoriyasi2,
            RiskHadisesiNumune = dto.RiskHadisesiNumune,
            ZererTesirKateqoriyasi = dto.ZererTesirKateqoriyasi,
            UmumiZererMebleg = dto.UmumiZererMebleg,
            PotensialZererMebleg = dto.PotensialZererMebleg,
            BerpaTarixi = dto.BerpaTarixi,
            BerpaOlunanMebleg = dto.BerpaOlunanMebleg,
            SigortaIleBerpaOlunanHisse = dto.SigortaIleBerpaOlunanHisse,
            TedbirlerinTarixi = dto.TedbirlerinTarixi,
            TedbirlerinTesviri = dto.TedbirlerinTesviri,
            TedbirlereMesulBolme = dto.TedbirlereMesulBolme,
            TedbirlerinIcraStatusu = dto.TedbirlerinIcraStatusu
        };

        ViewData["Title"] = $"Redaktə — {dto.QeydiyyatKodu}";
        var vm = await BuildFormVMAsync(updateDto);
        vm.RedaktedirMi = true;
        return View(vm);
    }

    // ── POST: /Risk/EmeliyyatRiski/Edit/5 ────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmeliyyatRiskiHadisesiUpdateDto dto)
    {
        dto.Id = id;
        var icraciId = CariIcraciId();
        var result = await _service.YenileAsync(dto, icraciId);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message ?? "Xəta baş verdi.");
            var vm = await BuildFormVMAsync(dto);
            vm.RedaktedirMi = true;
            return View(vm);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Köməkçi metodlar ─────────────────────────────────────────────────

    private int? CariIcraciId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private async Task<EmeliyyatRiskiFormVM> BuildFormVMAsync(EmeliyyatRiskiHadisesiUpdateDto dto)
    {
        var isciSonuc = await _isciService.HamisiniGetirAsync();
        var parametr = await _parametrleriService.AlAsync();

        return new EmeliyyatRiskiFormVM
        {
            Dto = dto,
            BiznesSaheleri = BiznesSaheleriSelect(dto.BiznesSahesi),
            RiskKateqoriyalari1 = RiskKateqoriyalari1Select(dto.RiskKateqoriyasi1),
            RiskKateqoriyalari2 = Enum.GetValues<RiskKateqoriyasi2>()
                .Select(k => (Deyer: (int)k, Ad: EmeliyyatRiskiAdlari.RiskKateqoriyasi2AdiKodlu(k), Ust1: (int)k / 10))
                .ToList(),
            TezlikDereceleri = EnumSelect<RiskDerecesi>(EmeliyyatRiskiAdlari.RiskDerecesiAdi, dto.TezlikDerecesi),
            TesirDereceleri = EnumSelect<RiskDerecesi>(EmeliyyatRiskiAdlari.RiskDerecesiAdi, dto.TesirDerecesi),
            ZererTesirKateqoriyalari = EnumSelect<ZererTesirKateqoriyasi>(
                EmeliyyatRiskiAdlari.ZererTesirKateqoriyasiAdi, dto.ZererTesirKateqoriyasi),
            TedbirIcraStatuslari = EnumSelect<TedbirIcraStatusu>(
                EmeliyyatRiskiAdlari.TedbirIcraStatusuAdi, dto.TedbirlerinIcraStatusu),
            Isciler = isciSonuc.Success && isciSonuc.Data != null
                ? isciSonuc.Data
                    .Select(i => new SelectListItem(i.TamAd, i.Id.ToString(), i.Id == dto.MueyyenlesdirenIsciId))
                    .ToList()
                : new List<SelectListItem>(),
            TedbirZererHeddi = parametr.TedbirZererHeddi
        };
    }

    private static List<SelectListItem> BiznesSaheleriSelect(BiznesSahesi? secili) =>
        Enum.GetValues<BiznesSahesi>()
            .Select(s => new SelectListItem(EmeliyyatRiskiAdlari.BiznesSahesiAdiKodlu(s), ((int)s).ToString(), s == secili))
            .ToList();

    private static List<SelectListItem> RiskKateqoriyalari1Select(RiskKateqoriyasi1? secili) =>
        Enum.GetValues<RiskKateqoriyasi1>()
            .Select(k => new SelectListItem(EmeliyyatRiskiAdlari.RiskKateqoriyasi1AdiKodlu(k), ((int)k).ToString(), k == secili))
            .ToList();

    private static List<SelectListItem> EnumSelect<TEnum>(Func<TEnum, string> adFunc, TEnum? secili)
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>()
            .Select(v => new SelectListItem(adFunc(v), Convert.ToInt32(v).ToString(), v.Equals(secili)))
            .ToList();
}
