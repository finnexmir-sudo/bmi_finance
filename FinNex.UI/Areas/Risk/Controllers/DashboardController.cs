using System.Text.Json;
using ClosedXML.Excel;
using FinNex.Application.DTOs.Risk;
using FinNex.Application.Interfaces.Risk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;

namespace FinNex.UI.Areas.Risk.Controllers;

[Area("Risk")]
[Authorize]
public class DashboardController : Controller
{
    private readonly IRiskService _service;

    public DashboardController(IRiskService service)
    {
        _service = service;
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

    // ── Məlumat Bazası — "Axtarılanlar" siyahısının bank müştəriləri ilə yoxlanması ──

    // GET: boş forma (fayl yükləmə)
    public IActionResult MelumatBazasi() => View(new AxtarisNeticeDto());

    // POST 1-ci ADDIM: .xlsx oxunur və cədvəldə GÖSTƏRİLİR. BMI-yə sorğu GETMİR.
    // (22.09.2026, istifadəçi qərarı: «həmin exceli tabledə göstərsin və sonra
    //  bazada axtarmaq işlərinə getsin buton ilə».)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MelumatBazasiYukle(IFormFile fayl)
    {
        if (fayl == null || fayl.Length == 0)
        {
            TempData["Error"] = "Excel faylı seçilməyib.";
            return RedirectToAction(nameof(MelumatBazasi));
        }

        // ⚠️ KÖHNƏ .xls (OLE2) ClosedXML ilə AÇILMIR — OpenXML yalnız .xlsx oxuyur.
        // Adına görə əvvəlcədən deyirik, yoxsa aşağıdakı `catch` anlaşılmaz
        // kitabxana mətni göstərər.
        var uzanti = Path.GetExtension(fayl.FileName);
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
            // Kök səbəbi də göstər — kitabxana xətaları çox vaxt InnerException-dadır
            // və tək `ex.Message` diaqnozu yanlış istiqamətə aparır (CLAUDE.md).
            var kok = ex; while (kok.InnerException != null) kok = kok.InnerException;
            var metn = ReferenceEquals(kok, ex) ? ex.Message : $"{ex.Message} → {kok.Message}";
            TempData["Error"] = "Excel faylı oxuna bilmədi: " + metn;
            return RedirectToAction(nameof(MelumatBazasi));
        }

        if (oxunus.Xeta != null)
        {
            TempData["Error"] = oxunus.Xeta;
            return RedirectToAction(nameof(MelumatBazasi));
        }

        // Yalnız GÖSTƏRİŞ — `Uygunluqlar` boşdur, `AxtarisEdildi` false.
        return View("MelumatBazasi", new AxtarisNeticeDto
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
    public async Task<IActionResult> MelumatBazasiAxtar(string setirlerJson, string? menbe)
    {
        List<AxtarisSetriDto>? setirler;
        try { setirler = JsonSerializer.Deserialize<List<AxtarisSetriDto>>(setirlerJson); }
        catch { setirler = null; }

        if (setirler == null || setirler.Count == 0)
        {
            TempData["Error"] = "Axtarılacaq siyahı itdi — Excel faylını yenidən yükləyin.";
            return RedirectToAction(nameof(MelumatBazasi));
        }

        var netice = await _service.AxtarilanlariYoxlaAsync(setirler);
        netice.AxtarisEdildi = true;
        netice.Menbe = menbe;
        return View("MelumatBazasi", netice);
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
        // Real şablon (22.09.2026): A=«Adlar», B=«VOEN», C=«fin», D=«novu».
        int basliqIdx = -1, adC = 0, voenC = 0, finC = 0, novC = 0;
        for (var i = 0; i < Math.Min(10, hamSetirler.Count); i++)
        {
            int a = 0, v = 0, f = 0, n = 0;
            for (var c = 1; c <= baxilacaq; c++)
            {
                var h = Sadeles(hamSetirler[i].Cell(c).GetString());
                if (h.Length == 0) continue;
                if (f == 0 && h.Contains("fin")) f = c;
                else if (v == 0 && h.Contains("voen")) v = c;
                else if (a == 0 && (h.Contains("soyad") || h.Contains("saa")
                                 || h.Contains("sexs")  || h.StartsWith("ad"))) a = c;
                else if (n == 0 && h.StartsWith("nov")) n = c;   // «novu» / «Növü»
            }
            if (a > 0 || v > 0 || f > 0) { basliqIdx = i; adC = a; voenC = v; finC = f; novC = n; break; }
        }

        var diaq = new System.Text.StringBuilder($"vərəq «{ws.Name}»");
        IEnumerable<IXLRow> data;

        if (basliqIdx >= 0)
        {
            data = hamSetirler.Skip(basliqIdx + 1);
            diaq.Append($" · başlıq {hamSetirler[basliqIdx].RowNumber()}-ci sətir · sütunlar: ")
                .Append($"Ad={SutunHerfi(adC)}, VÖEN={SutunHerfi(voenC)}, FİN={SutunHerfi(finC)}");
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
            var ad   = adC   > 0 ? row.Cell(adC).GetString().Trim()   : "";
            var voen = voenC > 0 ? row.Cell(voenC).GetString().Trim() : "";
            var fin  = finC  > 0 ? row.Cell(finC).GetString().Trim()  : "";
            var novu = novC  > 0 ? row.Cell(novC).GetString().Trim()  : "";

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
    public IActionResult MelumatBazasiExcel(string neticeJson)
    {
        AxtarisNeticeDto? netice;
        try { netice = JsonSerializer.Deserialize<AxtarisNeticeDto>(neticeJson); }
        catch { netice = null; }
        if (netice == null || netice.Setirler.Count == 0)
        {
            TempData["Error"] = "İxrac ediləcək nəticə tapılmadı.";
            return RedirectToAction(nameof(MelumatBazasi));
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
