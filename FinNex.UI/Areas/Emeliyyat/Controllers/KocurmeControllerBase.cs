using System.Globalization;
using System.Security.Claims;
using FinNex.Application.DTOs.Emeliyyat;
using FinNex.Application.Interfaces.Emeliyyat;
using FinNex.Domain;
using FinNex.UI.Services.Kredit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;

namespace FinNex.UI.Areas.Emeliyyat.Controllers;

// Pul köçürməsi və Tələbə köçürməsi üçün ortaq baza — Novu ilə ayrılır.
[Area("Emeliyyat")]
[Authorize]
public abstract class KocurmeControllerBase : Controller
{
    protected readonly IKocurmeService _service;
    private readonly IWebHostEnvironment _env;
    protected KocurmeControllerBase(IKocurmeService service, IWebHostEnvironment env)
    {
        _service = service;
        _env = env;
    }

    protected abstract string Novu { get; }     // "Pul" / "Telebe"
    protected abstract string Baslik { get; }    // səhifə başlığı

    private const string V = "~/Areas/Emeliyyat/Views/Kocurme/";

    protected int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected bool IsAdmin() => User.IsInRole(RoleNames.Admin);

    private void Baza()
    {
        ViewBag.Baslik = Baslik;
        ViewBag.Novu = Novu;
        ViewBag.UserId = GetUserId();
        ViewBag.IsAdmin = IsAdmin();
    }

    public async Task<IActionResult> Index(int? il)
    {
        var model = await _service.HamisiniGetirAsync(Novu, il);
        ViewBag.Il = il;
        Baza();
        return View($"{V}Index.cshtml", model);
    }

    [HttpGet]
    public IActionResult Yarat()
    {
        Baza();
        return View($"{V}Yarat.cshtml", new KocurmeCreateDto { Tarix = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(KocurmeCreateDto dto)
    {
        var res = await _service.YaratAsync(Novu, dto, GetUserId());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        if (!res.Success)
        {
            Baza();
            return View($"{V}Yarat.cshtml", dto);
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detal(int id)
    {
        var model = await _service.DetalAsync(id, Novu);
        if (model == null)
        {
            TempData["Error"] = "Köçürmə tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        Baza();
        return View($"{V}Detal.cshtml", model);
    }

    // Canlı voucher preview (yadda saxlanmadan) — JSON
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VoucherPreview(KocurmeCreateDto dto)
    {
        var setirler = _service.VoucherHesabla(dto, "(yeni)");
        return Json(setirler.Select(s => new { s.Debet, s.Kredit, Mebleg = s.Mebleg, s.Teyinat }));
    }

    // Excel ixracı — başqa proqrama import üçün: sheet "PK_Iran", D=debet, F=kredit, G=məbləğ, J=təyinat
    public async Task<IActionResult> ExcelIxrac(int id)
    {
        var m = await _service.DetalAsync(id, Novu);
        if (m == null) { TempData["Error"] = "Köçürmə tapılmadı."; return RedirectToAction(nameof(Index)); }

        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("PK_Iran");
        for (int i = 0; i < m.Setirler.Count; i++)
        {
            var r = sh.CreateRow(i + 1);                 // 2-ci sətirdən (index 1)
            var s = m.Setirler[i];
            r.CreateCell(3).SetCellValue(s.Debet ?? "");   // D
            r.CreateCell(5).SetCellValue(s.Kredit ?? "");  // F
            r.CreateCell(6).SetCellValue((double)s.Mebleg);// G
            r.CreateCell(9).SetCellValue(s.Teyinat ?? ""); // J
        }
        using var ms = new MemoryStream();
        wb.Write(ms, true);
        var ad = $"PK_Kocur_{(m.HevaleNo ?? id.ToString()).Replace("/", "-")}.xls";
        return File(ms.ToArray(), "application/vnd.ms-excel", ad);
    }

    // Word ərizə ixracı — Erize1.docx şablonu token doldurma
    public async Task<IActionResult> WordIxrac(int id)
    {
        var m = await _service.DetalAsync(id, Novu);
        if (m == null) { TempData["Error"] = "Köçürmə tapılmadı."; return RedirectToAction(nameof(Index)); }

        var sablon = Path.Combine(_env.WebRootPath, "Files", "Word", "Emeliyyat", "Erize1.docx");
        if (!System.IO.File.Exists(sablon)) { TempData["Error"] = "Ərizə şablonu tapılmadı."; return RedirectToAction(nameof(Detal), new { id }); }

        string F(decimal? v) => v.HasValue ? v.Value.ToString("#,0.00", CultureInfo.InvariantCulture) : "";
        var tokenler = new Dictionary<string, string?>
        {
            ["{T}"] = m.HevaleNo,
            ["{g_adi}"] = m.GonderenAd, ["{g_soyadi}"] = m.GonderenSoyad, ["{g_ataadi}"] = m.GonderenAtaAd,
            ["{G_passport}"] = m.GonderenPassport, ["{g_telefon}"] = m.GonderenTelefon,
            ["{bank_adi}"] = m.BankAd, ["{filial}"] = m.Filial,
            ["{a_adi}"] = m.AlanAd, ["{a_soyadi}"] = m.AlanSoyad, ["{a_ataadi}"] = m.AlanAtaAd,
            ["{a_hesab}"] = m.AlanHesab, ["{a_passport}"] = m.AlanPassport,
            ["{mebleg}"] = F(m.Mebleg), ["{m_yaziile}"] = "",
            ["{valyuta_novu}"] = m.KocurulenValyuta,
            ["{meqsed}"] = m.Meqsed, ["{elave}"] = m.Elave, ["{qeyd}"] = m.Qeyd,
            ["{alinan_valyuta}"] = $"{F(m.Mebleg)} {m.MedaxilValyuta}".Trim(),
            ["{satilan_valyuta}"] = (m.KocurulenValyuta is "Rial" or "Rubl") ? $"{F(m.Mebleg * (m.IranRial ?? 0m))} {m.KocurulenValyuta}".Trim() : "",
            ["{mezenne}"] = (m.KocurulenValyuta is "Rial" or "Rubl") ? F(m.IranRial) : "",
            ["{tarix}"] = m.Tarix?.ToString("dd.MM.yyyy")
        };
        var bytes = KreditWordService.Doldur(sablon, tokenler);
        var ad = $"Erize_{(m.HevaleNo ?? id.ToString()).Replace("/", "-")}.docx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ad);
    }

    [HttpGet]
    public async Task<IActionResult> Redakte(int id)
    {
        var dto = await _service.RedakteMelumatiAsync(id, Novu);
        if (dto == null)
        {
            TempData["Error"] = "Köçürmə tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        if (!IsAdmin() && dto.YaradanId != GetUserId())
        {
            TempData["Error"] = "Bu köçürməni yalnız yaradan və ya Admin dəyişə bilər.";
            return RedirectToAction(nameof(Index));
        }
        Baza();
        return View($"{V}Redakte.cshtml", dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Redakte(KocurmeEditDto dto)
    {
        var res = await _service.YenileAsync(Novu, dto, GetUserId(), IsAdmin());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        if (!res.Success)
        {
            Baza();
            return View($"{V}Redakte.cshtml", dto);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var res = await _service.SilAsync(id, GetUserId(), IsAdmin());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }
}
