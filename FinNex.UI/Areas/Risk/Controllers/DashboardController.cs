using System.Text.Json;
using ClosedXML.Excel;
using FinNex.Application.DTOs.Aml;
using FinNex.Application.DTOs.Risk;
using FinNex.Application.Interfaces.Aml;
using FinNex.Application.Interfaces.Risk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;

namespace FinNex.UI.Areas.Risk.Controllers;

[Area("Risk")]
[Authorize]
public class DashboardController : Controller
{
    private readonly IRiskService _service;
    private readonly IMelumatBazasiService _mb;

    /// <summary>«Məlumat Bazası» Excel şablonu üçün — `App_Data/Templates/`.</summary>
    private readonly IWebHostEnvironment _env;

    public DashboardController(IRiskService service, IMelumatBazasiService mb, IWebHostEnvironment env)
    {
        _service = service;
        _mb = mb;
        _env = env;
    }

    // Risk dashboard: KPI kartları + qrafiklər + hesabat kartları
    public async Task<IActionResult> Index()
    {
        var model = await _service.PanelAsync();
        return View(model);
    }

    // Bir hesabatı icra edib dinamik cədvəl göstərir (bt/st/t=tarixlər, h=hədd, il=il)
    public async Task<IActionResult> Hesabat(int id, string? bt, string? st, string? t, string? h, string? il)
    {
        var deyer = new RiskParametrDeyer { BasTarix = bt, SonTarix = st, Tarix = t, Hedd = h, Il = il };
        var model = await _service.IcraEtAsync(id, deyer);
        if (model == null)
        {
            TempData["Error"] = "Hesabat tapılmadı və ya aktiv deyil.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Hesabatlar = await _service.HesabatlarAsync();
        return View(model);
    }

    // Hesabatın nəticəsini Excel-ə ixrac edir
    public async Task<IActionResult> Excel(int id, string? bt, string? st, string? t, string? h, string? il)
    {
        var deyer = new RiskParametrDeyer { BasTarix = bt, SonTarix = st, Tarix = t, Hedd = h, Il = il };
        var m = await _service.IcraEtAsync(id, deyer);
        if (m == null || !m.IcraOlundu)
        {
            TempData["Error"] = "Hesabat tapılmadı.";
            return RedirectToAction(nameof(Index));
        }

        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Risk");
        var hdr = sh.CreateRow(0);
        for (int c = 0; c < m.Netice.Sutunlar.Count; c++)
            hdr.CreateCell(c).SetCellValue(m.Netice.Sutunlar[c]);

        for (int i = 0; i < m.Netice.Setirler.Count; i++)
        {
            var r = sh.CreateRow(i + 1);
            var row = m.Netice.Setirler[i];
            for (int c = 0; c < row.Length; c++)
            {
                var v = row[c];
                var cell = r.CreateCell(c);
                if (v is null) cell.SetCellValue("");
                else if (v is decimal dec) cell.SetCellValue((double)dec);
                else if (v is double dbl) cell.SetCellValue(dbl);
                else if (v is float fl) cell.SetCellValue(fl);
                else if (v is int i32) cell.SetCellValue(i32);
                else if (v is long i64) cell.SetCellValue(i64);
                else if (v is DateTime dt) cell.SetCellValue(dt.ToString("dd.MM.yyyy"));
                else cell.SetCellValue(v.ToString());
            }
        }

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        var ad = $"Risk_{m.Ad.Replace(" ", "_").Replace("/", "-")}.xls";
        return File(ms.ToArray(), "application/vnd.ms-excel", ad);
    }

    // ── Məlumat Bazası — dövr üzrə AML paketi ────────────────────────────────
    //    BMI mənbəyi: `BMI/AML/Sorgular/MelumatBazasi.cs` → `excelDoldur()`.
    //    İki tarix + «Ümumi sorğu» → 11 Oracle SELECT → 11 vərəqli Excel.

    // GET: 1-ci addım — Excel siyahısı yüklənir. Tarix seçimi yüklədikdən SONRA
    // açılır, çünki siyahı olmadan sorğu icra edilmir (BMI-də siyahı
    // `odb.aml_yoxlama` cədvəlində saxlanılırdı; biz Oracle-a yazmırıq).
    public IActionResult MelumatBazasi()
    {
        ViewBag.Vereqler = _mb.Vereqler;
        return View(VarsayilanNetice());
    }

    private static FinNex.Application.DTOs.Aml.MelumatBazasiNeticeDto VarsayilanNetice()
    {
        // Defolt dövr — hər ikisi BU GÜN (istifadəçi qərarı, 23.09.2026).
        // (BMI-də dəyərlər Designer-də SABİT idi: 31-01-2025 / 28-02-2025,
        //  yəni heç bir «iş günü» hesablaması yox idi — operator əl ilə seçirdi.)
        //
        // ⚠️ Əvvəl BasTarix keçən ayın son gününə, SonTarix bu günə sabitlənirdi
        // (əsas fərqi: SonTarix bir zamanlar CARİ AYIN SON GÜNÜNƏ, yəni GƏLƏCƏYƏ
        // düşürdü — 23.09.2026-da düzəldilib). İstifadəçi tələbi: operator adətən
        // TƏK GÜNLÜK aralıq seçir, ona görə hər iki xana açılışda EYNİ günə
        // (bu günə) sabitlənir — operator lazım gəldikcə əl ilə genişləndirir.
        return new FinNex.Application.DTOs.Aml.MelumatBazasiNeticeDto
        {
            BasTarix = DateTime.Today,
            SonTarix = DateTime.Today
        };
    }

    // POST 1-ci ADDIM: .xlsx oxunur və cədvəldə GÖSTƏRİLİR. BMI-yə sorğu GETMİR.
    // Oxuyucu `Axtarilanlar` axını ilə ORTAQDIR (`ExceldenOxu`) — iki nüsxə
    // saxlansa biri gec-tez köhnə qalar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MelumatBazasiYukle(IFormFile fayl)
    {
        var uzanti = Path.GetExtension(fayl?.FileName ?? "");
        if (fayl == null || fayl.Length == 0)
        {
            TempData["Error"] = "Excel faylı seçilməyib.";
            return RedirectToAction(nameof(MelumatBazasi));
        }
        if (!string.Equals(uzanti, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = $"Yalnız .xlsx faylı oxunur (seçilən: «{fayl.FileName}»). " +
                                "Köhnə .xls faylını Excel-də açıb «Farklı kaydet → Excel Workbook (.xlsx)» edin.";
            return RedirectToAction(nameof(MelumatBazasi));
        }

        ExcelOxunus oxunus;
        try
        {
            using var stream = fayl.OpenReadStream();
            oxunus = ExceldenOxu(stream);
        }
        catch (Exception ex)
        {
            var kok = ex; while (kok.InnerException != null) kok = kok.InnerException;
            TempData["Error"] = "Excel faylı oxuna bilmədi: " +
                (ReferenceEquals(kok, ex) ? ex.Message : $"{ex.Message} → {kok.Message}");
            return RedirectToAction(nameof(MelumatBazasi));
        }

        if (oxunus.Xeta != null)
        {
            TempData["Error"] = oxunus.Xeta;
            return RedirectToAction(nameof(MelumatBazasi));
        }

        var netice = VarsayilanNetice();
        netice.AxtarilanSay = oxunus.Setirler.Count;

        // Kəsilmə xəbərdarlığı MƏHZ BURADA verilir: «Ümumi sorğu» addımı fayl
        // qaytarır, səhifə render olunmur — orada yazılan mesaj heç yerə düşməzdi.
        // ⚠️ `_mb.MaxUmumiSetir` (ÜMUMİ hədd, batch-lərə bölünərək) — `BmiLatin.MaxSetir`
        // DEYİL, o, tək batch-in ölçüsüdür (23.09.2026-dan HazirlaAsync avtomatik bölür).
        if (oxunus.Setirler.Count > _mb.MaxUmumiSetir)
            TempData["Error"] = $"Siyahıda {oxunus.Setirler.Count} sətir var — " +
                                $"yalnız ilk {_mb.MaxUmumiSetir} sətir axtarılacaq.";

        ViewBag.Vereqler   = _mb.Vereqler;
        ViewBag.Siyahi     = oxunus.Setirler;
        ViewBag.SiyahiJson = JsonSerializer.Serialize(oxunus.Setirler);
        ViewBag.Menbe      = $"{fayl.FileName} · {oxunus.Diaqnostika}";
        return View("MelumatBazasi", netice);
    }

    // POST 2-ci ADDIM: «Ümumi sorğu» — paketi hazırlayıb .xlsx kimi verir.
    // Siyahı gizli sahədə JSON kimi gəlir (fayl input-u yenidən doldurula bilmir,
    // `TempData` isə bu həcmi saxlamır) — `Axtarilanlar` axınında olduğu kimi.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MelumatBazasiPaket(
        string setirlerJson, DateTime basTarix, DateTime sonTarix,
        AxtarisRejimi rejim, CancellationToken ct)
    {
        List<AxtarisSetriDto>? setirler;
        try { setirler = JsonSerializer.Deserialize<List<AxtarisSetriDto>>(setirlerJson); }
        catch { setirler = null; }

        if (setirler == null || setirler.Count == 0)
        {
            TempData["Error"] = "Axtarılacaq siyahı itdi — Excel faylını yenidən yükləyin.";
            return RedirectToAction(nameof(MelumatBazasi));
        }

        var netice = await _mb.HazirlaAsync(basTarix, sonTarix, setirler, rejim, ct);

        if (netice.Xeta != null)
        {
            TempData["Error"] = netice.Xeta;
            return RedirectToAction(nameof(MelumatBazasi));
        }

        // Heç bir vərəq alınmadısa fayl vermək mənasızdır — səbəbi ekranda göstər.
        if (netice.Vereqler.All(v => v.Xeta != null))
        {
            TempData["Error"] = "Heç bir vərəq hazırlanmadı. " +
                                string.Join(" | ", netice.XetaliVereqler.Take(3).Select(v => $"{v.Ad}: {v.Xeta}"));
            return RedirectToAction(nameof(MelumatBazasi));
        }

        byte[] fayl;
        try { fayl = PaketiQur(netice); }
        catch (FileNotFoundException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(MelumatBazasi));
        }

        var ad = $"Melumat bazasi {netice.SonTarix:MMyyyy}.xlsx";   // BMI ilə eyni ad qaydası

        // Adi <form method="post"> ilə fayl endirmədə brauzer heç bir JS hadisəsi
        // vermir — "sorğu bitdi" anını bilmək üçün klassik üsul: faylı göndərməzdən
        // ƏVVƏL kiçik bir cookie qoy, JS onu poll edib tapanda düyməni aç. Beləliklə
        // düymə HƏMİŞƏ real bitmə vaxtına bağlıdır (3 saniyə də, 10 dəqiqə də olsa).
        Response.Cookies.Append("mbHazir", "1", new CookieOptions { Path = "/" });
        return File(fayl, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ad);
    }

    // ── Excel paketi — HAZIR ŞABLON DOLDURULUR ───────────────────────────────
    //
    // Şablon: `App_Data/Templates/Melumat_bazasi.xlsx` (BMI-nin öz faylı, data
    // sətirləri təmizlənmiş). Sıfırdan qurmaq ƏVƏZİNƏ doldururuq, çünki:
    //   * `Esas_Sehife` vərəqi hazırdır — «Nəticə sayı» `COUNT(...)` formulu və
    //     `=HYPERLINK("#'"&C5&"'!A1","Bax")` keçidi ilə;
    //   * başlıqlar, sütun enləri, «Əsas səhifə» geri keçidləri saxlanılır;
    //   * AML işçisi öz köhnə faylı ilə tutuşdura bilir.
    //
    // ⚠️ HƏR VƏRƏQİN DATA SƏTRİ FƏRQLİDİR — `Esas_Sehife`-dəki COUNT aralıqları
    // ilə tutuşdurulub (Emit_benef A4, Kred_zamin A5, qalanları A6). Səhv sətirdən
    // yazsaq başlıq üstələnər VƏ «Nəticə sayı» yanlış çıxar; heç bir xəta olmaz.
    private static readonly Dictionary<string, int> DataSetri = new()   // Excel sətri (1-dən)
    {
        ["Aktiv_hesablar"] = 6, ["Open_Accounts"]        = 6, ["Kochurme_Daxili_hes"] = 6,
        ["Exchange"]       = 6, ["Kochurme_Mushteri_hes"] = 6, ["Owner"]              = 6,
        ["Emit_benef"]     = 4, ["3-cu shexs"]           = 6, ["Transfer"]            = 6,
        ["A_M_L"]          = 6, ["Kred_zamin"]           = 5,
    };

    /// <summary>Sorğu alias-ı → Excel başlığı (şablonda olmayan YENİ sütunlar üçün).</summary>
    private static readonly Dictionary<string, string> YeniSutunBasliq = new(StringComparer.OrdinalIgnoreCase)
    {
        ["axtarilan"] = "Axtarılan şəxs",
        ["uygunluq"]  = "Uyğunluq",
        ["voen"]      = "VÖEN",
        ["fin"]       = "FİN",
    };

    private byte[] PaketiQur(FinNex.Application.DTOs.Aml.MelumatBazasiNeticeDto netice)
    {
        var yol = Path.Combine(_env.ContentRootPath, "App_Data", "Templates", "Melumat_bazasi.xlsx");
        if (!System.IO.File.Exists(yol))
            throw new FileNotFoundException("Şablon tapılmadı: App_Data/Templates/Melumat_bazasi.xlsx");

        XSSFWorkbook wb;
        using (var fs = new FileStream(yol, FileMode.Open, FileAccess.Read))
            wb = new XSSFWorkbook(fs);

        // ⚠️ Stilləri BİR DƏFƏ yarat. Hər xana üçün `CreateCellStyle()` çağırmaq
        // NPOI-nin 64 000 stil həddinə dəyir və fayl açılmaz olur.
        var serhed = wb.CreateCellStyle();
        serhed.BorderTop = serhed.BorderBottom = serhed.BorderLeft = serhed.BorderRight
             = NPOI.SS.UserModel.BorderStyle.Thin;

        var tarixStil = wb.CreateCellStyle();
        tarixStil.CloneStyleFrom(serhed);
        tarixStil.DataFormat = wb.CreateDataFormat().GetFormat("dd.mm.yyyy");

        foreach (var v in netice.Vereqler)
        {
            var sh = wb.GetSheet(v.Ad);
            if (sh == null) continue;               // şablonda olmayan vərəq — atla

            var dataSetri = DataSetri.TryGetValue(v.Ad, out var ds) ? ds : 6;
            var basliqIdx = dataSetri - 2;          // NPOI 0-dan sayır: Excel 6 → başlıq idx 4

            if (v.Xeta != null)
            {
                var xr = sh.GetRow(dataSetri - 1) ?? sh.CreateRow(dataSetri - 1);
                (xr.GetCell(0) ?? xr.CreateCell(0)).SetCellValue("XƏTA: " + v.Xeta);
                continue;
            }

            // Şablonda olmayan YENİ sütunların başlığını əlavə et (Axtarılan şəxs,
            // Uyğunluq, Transfer-də əlavə olaraq VÖEN/FİN). `№` 0-cı sütundadır,
            // ona görə sorğunun c-ci sütunu Excel-də c+1-dir.
            var bsh = sh.GetRow(basliqIdx);
            if (bsh != null)
            {
                var sonBasliq = bsh.LastCellNum;    // mövcud başlıqların sayı
                for (int c = 0; c < v.Sutunlar.Count; c++)
                {
                    if (c + 1 < sonBasliq) continue;
                    var ad = YeniSutunBasliq.TryGetValue(v.Sutunlar[c], out var b) ? b : v.Sutunlar[c];
                    var h = bsh.CreateCell(c + 1);
                    h.SetCellValue(ad);
                    if (bsh.GetCell(0)?.CellStyle is { } st) h.CellStyle = st;   // başlıq görünüşünü təkrarla
                }
            }

            for (int i = 0; i < v.Setirler.Count; i++)
            {
                var r = sh.CreateRow(dataSetri - 1 + i);
                var nCell = r.CreateCell(0);
                nCell.SetCellValue(i + 1);          // `Esas_Sehife` məhz A sütununu COUNT edir
                nCell.CellStyle = serhed;

                var sətir = v.Setirler[i];
                for (int c = 0; c < sətir.Length; c++)
                {
                    var cell = r.CreateCell(c + 1);
                    cell.CellStyle = serhed;

                    // ⚠️ Rəqəmi `ToString()` ilə YAZMA — az-AZ vergülü Excel-də
                    // mətnə çevirər və sütun toplanmaz (CLAUDE.md, `x:num` hadisəsi).
                    switch (sətir[c])
                    {
                        case null:        cell.SetCellValue(""); break;
                        case decimal d:   cell.SetCellValue((double)d); break;
                        case double db:   cell.SetCellValue(db); break;
                        case float f:     cell.SetCellValue(f); break;
                        case int i32:     cell.SetCellValue(i32); break;
                        case long i64:    cell.SetCellValue(i64); break;
                        // Tarix MƏTN kimi yazılsa Excel-də süzülmür və sıralanmır.
                        case DateTime dt: cell.SetCellValue(dt); cell.CellStyle = tarixStil; break;
                        default:          cell.SetCellValue(sətir[c]!.ToString()); break;
                    }
                }
            }
        }

        // Dövr və hazırlanma vaxtı — BMI-də olduğu kimi `Aktiv_hesablar` B4/D4-də.
        // Qalan vərəqlər onu FORMUL ilə oxuyur (`=Aktiv_hesablar!B4`).
        var ah = wb.GetSheet("Aktiv_hesablar");
        if (ah != null)
        {
            var r4 = ah.GetRow(3) ?? ah.CreateRow(3);
            (r4.GetCell(1) ?? r4.CreateCell(1))
                .SetCellValue($"{netice.BasTarix:dd.MM.yyyy}  -  {netice.SonTarix:dd.MM.yyyy}");
            (r4.GetCell(3) ?? r4.CreateCell(3))
                .SetCellValue($"{DateTime.Now:dd.MM.yyyy HH:mm}");
        }

        // ⚠️ MƏCBURİDİR. NPOI formulu HESABLAMIR, yalnız mətnini saxlayır.
        // Bunsuz `Esas_Sehife`-dəki «Nəticə sayı» və vərəqlərdəki
        // `=Aktiv_hesablar!B4` istinadları KEŞLƏNMİŞ (boş) dəyərlə açılar.
        wb.SetForceFormulaRecalculation(true);

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return ms.ToArray();
    }

    // ── Axtarılanlar — Excel siyahısının bank müştəriləri ilə yoxlanması ──────
    //    BMI mənbəyi: `BMI/AML/AMLexcel.cs`.
    //
    //    ⚠️ 22.09.2026-ya qədər bu səhifə səhvən «Məlumat Bazası» adlanırdı.
    //    BMI-də «Məlumat Bazası» TAMAMİLƏ BAŞQA formadır
    //    (`BMI/AML/Sorgular/MelumatBazasi.cs` — iki tarix + «Ümumi sorğu»,
    //    11 sorğuluq Excel paketi). O, indi aşağıda ayrıca qurulub.
    //    Ad düzəldildi, funksiya SİLİNMƏDİ — işlək modul idi (CLAUDE.md).

    // GET: boş forma (fayl yükləmə)
    public IActionResult Axtarilanlar() => View(new AxtarisNeticeDto());

    // POST 1-ci ADDIM: .xlsx oxunur və cədvəldə GÖSTƏRİLİR. BMI-yə sorğu GETMİR.
    // (22.09.2026, istifadəçi qərarı: «həmin exceli tabledə göstərsin və sonra
    //  bazada axtarmaq işlərinə getsin buton ilə».)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AxtarilanlarYukle(IFormFile fayl)
    {
        if (fayl == null || fayl.Length == 0)
        {
            TempData["Error"] = "Excel faylı seçilməyib.";
            return RedirectToAction(nameof(Axtarilanlar));
        }

        // ⚠️ KÖHNƏ .xls (OLE2) ClosedXML ilə AÇILMIR — OpenXML yalnız .xlsx oxuyur.
        // Adına görə əvvəlcədən deyirik, yoxsa aşağıdakı `catch` anlaşılmaz
        // kitabxana mətni göstərər.
        var uzanti = Path.GetExtension(fayl.FileName);
        if (!string.Equals(uzanti, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = $"Yalnız .xlsx faylı oxunur (seçilən: «{fayl.FileName}»). " +
                                "Köhnə .xls faylını Excel-də açıb «Farklı kaydet → Excel Workbook (.xlsx)» edin.";
            return RedirectToAction(nameof(Axtarilanlar));
        }

        ExcelOxunus oxunus;
        try
        {
            using var stream = fayl.OpenReadStream();
            oxunus = ExceldenOxu(stream);
        }
        catch (Exception ex)
        {
            // Kök səbəbi də göstər — kitabxana xətaları çox vaxt InnerException-dadır
            // və tək `ex.Message` diaqnozu yanlış istiqamətə aparır (CLAUDE.md).
            var kok = ex; while (kok.InnerException != null) kok = kok.InnerException;
            var metn = ReferenceEquals(kok, ex) ? ex.Message : $"{ex.Message} → {kok.Message}";
            TempData["Error"] = "Excel faylı oxuna bilmədi: " + metn;
            return RedirectToAction(nameof(Axtarilanlar));
        }

        if (oxunus.Xeta != null)
        {
            TempData["Error"] = oxunus.Xeta;
            return RedirectToAction(nameof(Axtarilanlar));
        }

        // Yalnız GÖSTƏRİŞ — `Uygunluqlar` boşdur, `AxtarisEdildi` false.
        return View("Axtarilanlar", new AxtarisNeticeDto
        {
            Setirler      = oxunus.Setirler.Select(s => new AxtarisNeticeSetriDto { Axtarilan = s }).ToList(),
            UmumiSay      = oxunus.Setirler.Count,
            Menbe         = $"{fayl.FileName} · {oxunus.Diaqnostika}",
            AxtarisEdildi = false
        });
    }

    // POST 2-ci ADDIM: «Bazada axtar» — artıq göstərilən siyahı BMI-də yoxlanılır.
    // Siyahı gizli sahədə JSON kimi gəlir; fayl yenidən oxunmur (input yenidən
    // doldurula bilmir, TempData isə belə həcmi saxlamır).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AxtarilanlarAxtar(string setirlerJson, string? menbe)
    {
        List<AxtarisSetriDto>? setirler;
        try { setirler = JsonSerializer.Deserialize<List<AxtarisSetriDto>>(setirlerJson); }
        catch { setirler = null; }

        if (setirler == null || setirler.Count == 0)
        {
            TempData["Error"] = "Axtarılacaq siyahı itdi — Excel faylını yenidən yükləyin.";
            return RedirectToAction(nameof(Axtarilanlar));
        }

        var netice = await _service.AxtarilanlariYoxlaAsync(setirler);
        netice.AxtarisEdildi = true;
        netice.Menbe = menbe;
        return View("Axtarilanlar", netice);
    }

    // ── Excel oxunuşu ────────────────────────────────────────────────────────
    //
    // ⚠️ ƏVVƏLKİ VERSİYA «heç nə gəlmir» verirdi (real hadisə 22.09.2026) və
    // səbəbi ekranda görünmürdü. Üç sərt fərziyyə vardı:
    //   1. data HƏMİŞƏ 1-ci vərəqdədir;
    //   2. 1-ci sətir HƏMİŞƏ başlıqdır (`Skip(1)`) — başlıqsız faylda
    //      YEGANƏ data sətri yeyilirdi;
    //   3. sütunlar HƏMİŞƏ A/B/C-dir.
    // Üçü də pozulanda nəticə eyni idi: boş siyahı, izahsız.
    //
    // İndi fayla UYĞUNLAŞIR: data olan ilk vərəq seçilir, başlıq sətri
    // axtarılır (tapılmasa HEÇ NƏ atılmır), sütunlar başlıq adına görə
    // xəritələnir. Nə etdiyini `Diaqnostika` ilə ekranda YAZIR.
    private sealed record ExcelOxunus(List<AxtarisSetriDto> Setirler, string Diaqnostika, string? Xeta);

    /// <summary>Azəri hərflərini sadələşdirir ki, başlıq müqayisəsi «VÖEN»/«VOEN» fərqinə ilişməsin.</summary>
    private static string Sadeles(string? s)
    {
        var t = (s ?? "").Trim().ToLowerInvariant().Replace("̇", "");  // İ→i qalıq nöqtəsi
        var sb = new System.Text.StringBuilder(t.Length);
        foreach (var c in t)
            sb.Append(c switch
            {
                'ə' => 'e', 'ö' => 'o', 'ü' => 'u', 'ı' => 'i',
                'ğ' => 'g', 'ş' => 's', 'ç' => 'c', _ => c
            });
        return sb.ToString();
    }

    /// <summary>
    /// Birləşmiş FİN/VÖEN sütunundakı bir xananı formatına görə ayırd edir.
    /// VÖEN — Azərbaycanda YALNIZ rəqəm, 9-10 xanə. FİN — 7 simvol (adətən hərf+rəqəm
    /// qarışığı). Uzunluq fərqi kəskin olduğu üçün format-əsaslı ayırma etibarlıdır;
    /// istifadəçi (operator) FİN/VÖEN fərqini özü bilməli deyil, sistem ayırır.
    /// </summary>
    private static (string fin, string voen) SinifleFinVoen(string deyer)
    {
        if (string.IsNullOrWhiteSpace(deyer)) return ("", "");
        var t = deyer.Trim();
        var reqemSay = t.Count(char.IsDigit);
        if ((t.Length == 9 || t.Length == 10) && reqemSay == t.Length) return ("", t);
        return (t, "");
    }

    /// <summary>Sütun nömrəsi → hərf (1→A). İstifadəçiyə «hansı sütunu oxudum» demək üçün.</summary>
    private static string SutunHerfi(int n)
    {
        if (n <= 0) return "—";
        var s = "";
        while (n > 0) { n--; s = (char)('A' + n % 26) + s; n /= 26; }
        return s;
    }

    private static ExcelOxunus ExceldenOxu(Stream stream)
    {
        using var wb = new XLWorkbook(stream);

        if (wb.Worksheets.Count == 0)
            return new(new(), "", "Excel faylında heç bir vərəq (sheet) yoxdur.");

        // Data olan İLK vərəq — 1-ci vərəq boş ola bilər.
        var ws = wb.Worksheets.FirstOrDefault(w => w.RowsUsed().Any());
        if (ws == null)
        {
            var adlar = string.Join(", ", wb.Worksheets.Select(w => $"«{w.Name}»"));
            return new(new(), "", $"Faylın heç bir vərəqində data yoxdur. Vərəqlər: {adlar}.");
        }

        var hamSetirler = ws.RowsUsed().ToList();
        var sonSutun    = ws.LastColumnUsed()?.ColumnNumber() ?? 3;
        var baxilacaq   = Math.Min(sonSutun, 30);

        // Başlıq sətrini ilk 10 sətirdə axtar (fayl başında boş/başlıq mətni ola bilər).
        // Əvvəlki şablon: A=«Adlar», B=«VOEN», C=«fin», D=«novu» (ayrı sütunlar).
        // 23.09.2026: istifadəçi FİN və VÖEN-i TƏK sütunda saxlamağı seçdi (məs.
        // «FİN/VOEN» başlığı) — operator adamın FİN, yoxsa VÖEN olduğunu bilməli
        // deyil, sistem formatına görə (uzunluq/rəqəm) özü ayırd edir (bax `SinifleFinVoen`).
        // Ayrı sütunlu KÖHNƏ fayllar da işləməyə davam edir — birləşmiş sütun
        // yalnız başlıqda HƏM «fin», HƏM «voen» sözü birlikdə tapılanda seçilir.
        int basliqIdx = -1, adC = 0, voenC = 0, finC = 0, finVoenC = 0, novC = 0;
        for (var i = 0; i < Math.Min(10, hamSetirler.Count); i++)
        {
            int a = 0, v = 0, f = 0, fv = 0, n = 0;
            for (var c = 1; c <= baxilacaq; c++)
            {
                var h = Sadeles(hamSetirler[i].Cell(c).GetString());
                if (h.Length == 0) continue;
                if (fv == 0 && h.Contains("fin") && h.Contains("voen")) fv = c;   // BİRLƏŞMİŞ sütun
                else if (f == 0 && h.Contains("fin")) f = c;
                else if (v == 0 && h.Contains("voen")) v = c;
                else if (a == 0 && (h.Contains("soyad") || h.Contains("saa")
                                 || h.Contains("sexs")  || h.StartsWith("ad"))) a = c;
                else if (n == 0 && h.StartsWith("nov")) n = c;   // «novu» / «Növü»
            }
            if (a > 0 || v > 0 || f > 0 || fv > 0)
            { basliqIdx = i; adC = a; voenC = v; finC = f; finVoenC = fv; novC = n; break; }
        }

        var diaq = new System.Text.StringBuilder($"vərəq «{ws.Name}»");
        IEnumerable<IXLRow> data;

        if (basliqIdx >= 0)
        {
            data = hamSetirler.Skip(basliqIdx + 1);
            diaq.Append($" · başlıq {hamSetirler[basliqIdx].RowNumber()}-ci sətir · sütunlar: ")
                .Append($"Ad={SutunHerfi(adC)}, ");
            diaq.Append(finVoenC > 0
                ? $"FİN/VÖEN={SutunHerfi(finVoenC)}"
                : $"VÖEN={SutunHerfi(voenC)}, FİN={SutunHerfi(finC)}");
            if (novC > 0) diaq.Append($", Növü={SutunHerfi(novC)}");
        }
        else
        {
            // ⚠️ Başlıq tapılmadısa HEÇ NƏ ATILMIR. Köhnə kod burada da `Skip(1)`
            // edirdi və başlıqsız faylda ilk (bəzən yeganə) sətri yeyirdi.
            adC = 1; voenC = 2; finC = 3;
            data = hamSetirler;
            diaq.Append(" · başlıq sətri tapılmadı — bütün sətirlər data sayıldı, sütunlar A/B/C");
        }

        var setirler = new List<AxtarisSetriDto>();
        var sira = 1;
        foreach (var row in data)
        {
            var ad = adC > 0 ? row.Cell(adC).GetString().Trim() : "";

            string voen, fin;
            if (finVoenC > 0)
                (fin, voen) = SinifleFinVoen(row.Cell(finVoenC).GetString().Trim());
            else
            {
                voen = voenC > 0 ? row.Cell(voenC).GetString().Trim() : "";
                fin  = finC  > 0 ? row.Cell(finC).GetString().Trim()  : "";

                // ⚠️ 25.09.2026, real hadisə: AYRI "VÖEN" sütunu tək başına
                // olanda (FİN sütunu heç tapılmayıb, `finC=0`) operator ora
                // FİN-formatlı dəyər yazmışdı ("65GPNXJ") — sistem onu olduğu
                // kimi VÖEN saydı, FİN boş qaldı, «Yalnız FİN/VÖEN» axtarışı
                // heç nə tapmadı. Format uyğunsuzluğunu SinifleFinVoen ilə
                // düzəlt — VÖEN yalnız 9-10 rəqəmdirsə doğrudur, əks halda
                // dəyər FİN-dir (və əksinə). Yalnız qarşı sahə BOŞ olanda
                // köçür ki, hər ikisi artıq düzgün doldurulmuş sətirlərə
                // toxunmasın.
                if (voen.Length > 0)
                {
                    var (v2f, _) = SinifleFinVoen(voen);
                    if (v2f.Length > 0 && fin.Length == 0) { fin = v2f; voen = ""; }
                }
                if (fin.Length > 0)
                {
                    var (_, f2v) = SinifleFinVoen(fin);
                    if (f2v.Length > 0 && voen.Length == 0) { voen = f2v; fin = ""; }
                }
            }

            var novu = novC > 0 ? row.Cell(novC).GetString().Trim() : "";

            // «Növü» TƏK BAŞINA sətri saxlatmır — axtarılacaq heç nə yoxdursa sətir boşdur.
            if (ad.Length == 0 && voen.Length == 0 && fin.Length == 0) continue;

            setirler.Add(new AxtarisSetriDto
            {
                Sira = sira++, AdSoyadAta = ad, Voen = voen, Fin = fin, Novu = novu
            });
        }

        if (setirler.Count == 0)
        {
            // Boş nəticədə faylda NƏ GÖRDÜYÜMÜZÜ yaz — «tapılmadı» tək başına
            // istifadəçiyə heç nə demir və kodda səbəb axtarmağa məcbur edir.
            var ornek = string.Join("  |  ", hamSetirler.Take(3).Select(r =>
                string.Join(" ; ", Enumerable.Range(1, Math.Min(baxilacaq, 5))
                    .Select(c => r.Cell(c).GetString().Trim()))));
            return new(new(), diaq.ToString(),
                $"Faylda doldurulmuş sətir tapılmadı ({diaq}). " +
                $"Faylın ilk sətirlərində gördüyüm: {ornek}. " +
                "Sütun başlıqlarında «Ad Soyad», «VÖEN» və ya «FİN» sözləri olsa avtomatik tanıyıram.");
        }

        diaq.Append($" · {setirler.Count} sətir oxundu");
        return new(setirler, diaq.ToString(), null);
    }

    // POST: göstərilən nəticəni Excel-ə ixrac edir (yenidən Oracle sorğusu icra etmir —
    // artıq göstərilən nəticəni JSON-dan bərpa edir).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AxtarilanlarExcel(string neticeJson)
    {
        AxtarisNeticeDto? netice;
        try { netice = JsonSerializer.Deserialize<AxtarisNeticeDto>(neticeJson); }
        catch { netice = null; }
        if (netice == null || netice.Setirler.Count == 0)
        {
            TempData["Error"] = "İxrac ediləcək nəticə tapılmadı.";
            return RedirectToAction(nameof(Axtarilanlar));
        }

        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("Axtarilanlar");
        var hdr = sh.CreateRow(0);
        // «Növü» — istifadəçinin şablonundakı 4-cü sütun; axtarışda iştirak etmir,
        // amma ixracda qalır ki, mühasib/risk işçisi öz faylı ilə tutuşdura bilsin.
        string[] basliqlar = { "№", "Ad Soyad Ata adı", "VÖEN", "FİN", "Növü",
                               "Tapıldı", "Mənbə", "Uyğun ad", "Uyğun regnom", "Uyğun sahə" };
        for (int c = 0; c < basliqlar.Length; c++) hdr.CreateCell(c).SetCellValue(basliqlar[c]);

        // Sətrin SOL hissəsi (Excel-dən gələn 5 sütun) hər iki qolda eynidir —
        // nüsxə saxlamırıq ki, sütun əlavə olunanda biri köhnə qalmasın.
        int r = 1;
        NPOI.SS.UserModel.IRow YeniSetir(AxtarisSetriDto a)
        {
            var row = sh.CreateRow(r++);
            row.CreateCell(0).SetCellValue(a.Sira);
            row.CreateCell(1).SetCellValue(a.AdSoyadAta ?? "");
            row.CreateCell(2).SetCellValue(a.Voen ?? "");
            row.CreateCell(3).SetCellValue(a.Fin ?? "");
            row.CreateCell(4).SetCellValue(a.Novu ?? "");
            return row;
        }

        foreach (var setir in netice.Setirler)
        {
            if (setir.Uygunluqlar.Count == 0)
            {
                YeniSetir(setir.Axtarilan).CreateCell(5).SetCellValue("Xeyr");
            }
            else
            {
                foreach (var u in setir.Uygunluqlar)
                {
                    var row = YeniSetir(setir.Axtarilan);
                    row.CreateCell(5).SetCellValue("Bəli");
                    row.CreateCell(6).SetCellValue(u.Menbe);
                    row.CreateCell(7).SetCellValue(u.AdSoyad ?? "");
                    row.CreateCell(8).SetCellValue(u.Regnom ?? "");
                    row.CreateCell(9).SetCellValue(u.UygunSahe ?? "");
                }
            }
        }

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        var ad = $"Melumat_bazasi_axtaris_{DateTime.Now:yyyyMMdd_HHmm}.xls";
        return File(ms.ToArray(), "application/vnd.ms-excel", ad);
    }
}
