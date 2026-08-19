using ClosedXML.Excel;
using FinNex.Application.DTOs.Aml;
using FinNex.Application.Interfaces.Aml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FinNex.UI.Areas.Risk.Controllers;

/// <summary>
/// AML hesabatları — «Hesab üzrə sorğu» (BMI: AML → Hesab üzrə sorğu).
///
/// Modul Risk sahəsindədir: top-nav-dakı «Departamentlər → Risk» maddəsinin
/// alt yazısı onsuz da «Risk / AML hesabatları»dır. Ayrıca «Hesabatlar» tabı
/// artıq HR hesabatı üçün işlənir — ora qoysaq iki fərqli şey bir ad altında
/// qalardı.
/// </summary>
[Area("Risk")]
[Authorize]
public class AmlController : Controller
{
    private readonly IAmlHesabatService _service;
    private readonly IConfiguration _config;

    public AmlController(IAmlHesabatService service, IConfiguration config)
    {
        _service = service;
        _config = config;
    }

    // Bankın öz rekvizitləri — şablonun şapkasına yazılır.
    // Köhnə şablonda bunlar SABİT yazılmışdı; yenisində boşdur, kod doldurur.
    private const string BankAdi = "Bank Melli Iran Bakı filialı";
    private const string BankVoen = "1300036291";

    private const string VerqSheet = "Hesab çıxarışı";

    // ── Forma ────────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Index(string? hesab, string? bt, string? st, bool huquqi = false)
    {
        var vm = new AmlHesabSorguDto
        {
            Hesab = hesab,
            BasTarix = ParseTarix(bt),
            SonTarix = ParseTarix(st),
            Huquqi = huquqi
        };
        ViewBag.Netice = null;
        return View(vm);
    }

    // ── Sorğu ────────────────────────────────────────────────────────────
    //
    // Tarixlər QƏSDƏN `string` kimi alınır və `ParseTarix` ilə oxunur.
    // Default model binder CARİ mədəniyyətlə (az-AZ) parse edir; `<input
    // type="date">` isə həmişə «yyyy-MM-dd» göndərir — ikisi uyuşmaya bilir
    // və sahə səssizcə `null` qalır. Eyni səbəbdən GET (Excel) və POST eyni
    // parametr adlarını işlədir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string? hesab, string? bt, string? st,
                                           bool huquqi, CancellationToken ct)
    {
        var model = new AmlHesabSorguDto
        {
            Hesab = hesab,
            BasTarix = ParseTarix(bt),
            SonTarix = ParseTarix(st),
            Huquqi = huquqi
        };
        ViewBag.Netice = await _service.IcraEtAsync(model, ct);
        return View(model);
    }

    // ── Excel ────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Excel(string? hesab, string? bt, string? st, bool huquqi, CancellationToken ct)
    {
        var sorgu = new AmlHesabSorguDto
        {
            Hesab = hesab,
            BasTarix = ParseTarix(bt),
            SonTarix = ParseTarix(st),
            Huquqi = huquqi
        };
        var m = await _service.IcraEtAsync(sorgu, ct);

        if (!m.Ugurlu || m.Setirler.Count == 0)
        {
            TempData["Error"] = m.Xeta ?? "Nəticə yoxdur — Excel yaradılmadı.";
            return RedirectToAction(nameof(Index), new { hesab, bt, st, huquqi });
        }

        var bayt = ExcelYarat(m);
        var ad = $"AML_cixaris_{m.Hesab}_{m.BasTarix:yyyyMMdd}_{m.SonTarix:yyyyMMdd}.xlsx";
        return File(bayt, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ad);
    }

    // ═════════════════════════════════════════════════════════════════════

    private static DateTime? ParseTarix(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        string[] formatlar = { "yyyy-MM-dd", "dd.MM.yyyy", "dd/MM/yyyy", "dd-MM-yyyy" };
        foreach (var f in formatlar)
            if (DateTime.TryParseExact(s.Trim(), f, System.Globalization.CultureInfo.InvariantCulture,
                                       System.Globalization.DateTimeStyles.None, out var d))
                return d;
        return DateTime.TryParse(s, out var d2) ? d2 : null;
    }

    /// <summary>
    /// Şablonu doldurur. Şablon tapılmasa sadə cədvəl generasiya edilir —
    /// hesabat heç vaxt «şablon yoxdur» deyə tamamilə dayanmır.
    /// </summary>
    private byte[] ExcelYarat(AmlHesabNeticeDto m)
    {
        var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
        var sablon = Path.Combine(dmsRoot, "hesabat-sablonlari", "aml", "AML_Hesab.xlsx");

        using var wb = System.IO.File.Exists(sablon)
            ? new XLWorkbook(sablon)
            : YeniKitab();

        var ws = wb.Worksheets.First();
        ws.Name = VerqSheet;

        // ── Şapka ────────────────────────────────────────────────────────
        // Xana ünvanları BMI-dəki `exceleat2()` ilə eynidir; yeni şablonda
        // əlavə olaraq D2/G2 (bank rekvizitləri) və D8 (hesabın valyutası) var.
        Yaz(ws, 2, 4, BankAdi);
        Yaz(ws, 2, 7, BankVoen);
        Yaz(ws, 5, 4, m.BasTarix.ToString("dd.MM.yyyy"));
        Yaz(ws, 5, 5, m.SonTarix.ToString("dd.MM.yyyy"));
        YazReqem(ws, 6, 4, m.GirisQaliq);
        YazReqem(ws, 6, 5, m.SonQaliq);
        Yaz(ws, 7, 4, m.Hesab);
        Yaz(ws, 8, 4, m.Valyuta);

        // ── Sətirlər — 12-ci sətirdən ────────────────────────────────────
        // Yeni şablonun 12-ci sətrindəki analitik qeydləri TESTDİR və üstündən
        // yazılır (istifadəçi qərarı, 19.08.2026). `startRow` dəyişmir.
        const int basSetir = 12;
        for (int i = 0; i < m.Setirler.Count; i++)
        {
            var setir = m.Setirler[i];
            for (int c = 0; c < setir.Length; c++)
            {
                var cell = ws.Cell(basSetir + i, c + 1);
                var v = setir[c];
                switch (v)
                {
                    case null:
                        cell.Value = "";
                        break;
                    // A/B — tarix. BMI Excel-ə «yyyy-MM-dd» MƏTNİ yazır;
                    // AML formatı belə gözlənilir, dəyişdirmirik.
                    case DateTime dt:
                        cell.Value = dt.ToString("yyyy-MM-dd");
                        break;
                    case decimal dec:
                        cell.Value = (double)dec;
                        break;
                    case double db:
                        cell.Value = db;
                        break;
                    case long l:
                        cell.Value = (double)l;
                        break;
                    case int ii:
                        cell.Value = (double)ii;
                        break;
                    default:
                        var s = v.ToString() ?? "";
                        // Hesab nömrəsi 20 rəqəmdir — rəqəm kimi yazılsa Excel onu
                        // elmi formata salır və son rəqəmləri itirir. Mətn qalsın.
                        cell.Value = s;
                        break;
                }
            }
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static XLWorkbook YeniKitab()
    {
        var wb = new XLWorkbook();
        wb.Worksheets.Add(VerqSheet);
        return wb;
    }

    private static void Yaz(IXLWorksheet ws, int satir, int sutun, string? deyer)
        => ws.Cell(satir, sutun).Value = deyer ?? "";

    private static void YazReqem(IXLWorksheet ws, int satir, int sutun, decimal? deyer)
    {
        if (deyer.HasValue) ws.Cell(satir, sutun).Value = (double)deyer.Value;
        else ws.Cell(satir, sutun).Value = "";
    }
}
