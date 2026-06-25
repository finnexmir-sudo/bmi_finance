using FinNex.Application.DTOs.Oracle;
using FinNex.Application.DTOs.Pid;
using FinNex.Application.Interfaces.Pid;
using FinNex.Domain;
using FinNex.Domain.Entities.Pid;
using FinNex.UI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NPOI.SS.UserModel;
using System.Globalization;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize(Roles = "Admin,PID")]
public class MehkemeIsiController : Controller
{
    private readonly IMehkemeIsiService _service;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _config;

    public MehkemeIsiController(
        IMehkemeIsiService service,
        UserManager<AppUser> userManager,
        IConfiguration config)
    {
        _service = service;
        _userManager = userManager;
        _config = config;
    }

    private async Task<int?> CurrentIsciIdAsync()
    {
        var u = await _userManager.GetUserAsync(User);
        return u?.IsciId;
    }

    private string DmsRoot =>
        _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";

    // ── Siyahı (canlı Oracle + proqram izləməsi) ──────────
    public async Task<IActionResult> Index()
    {
        var model = await _service.SiyahiGetirAsync();
        return View(model);
    }

    // ── Məhkəmə İşləri (izlənən bütün işlərin icmal siyahısı) ──
    public async Task<IActionResult> Isler()
    {
        var model = await _service.HamisiniGetirAsync();
        return View(model);
    }

    // ── İcra işləri (icrada olan / bağlanmış işlər — Excel "İcrada olan işlər") ──
    public async Task<IActionResult> IcraIsleri(string? status)
    {
        var model = await _service.IcraIsleriGetirAsync(status);
        ViewBag.Status = string.IsNullOrWhiteSpace(status) ? "aktiv" : status;
        return View(model);
    }

    // ── Yaxınlaşan görüşlər (bütün işlər üzrə gələcək iclaslar) ──
    public async Task<IActionResult> Gorusler()
    {
        var model = await _service.YaxinlasanGoruslerAsync();
        return View(model);
    }

    // ── Excel export (hər grid səhifəsi) ──────────────────────────
    // Aktiv Müştərilər — TAM Oracle sorğusu (bütün sütunlar/sətirlər), yalnız görünən cədvəl deyil.
    public async Task<IActionResult> IndexExcel()
    {
        OracleNetice n;
        try
        {
            n = await _service.SiyahiXamGetirAsync();
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Excel export alınmadı: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
        var bytes = ExcelExportHelper.YaratXam("Aktiv Müştərilər", n.Sutunlar, n.Setirler);
        return File(bytes, ExcelExportHelper.ContentType, $"Aktiv_Musteriler_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> IslerExcel()
    {
        var list = await _service.HamisiniGetirAsync();
        var basliqlar = new[] { "№", "Müştəri", "Qeydiyyat №", "Növ", "Təminat", "Status",
            "Əsas borc", "Məhkəmə xərci", "Məhkəməyə verilmə", "Qətnamə tarixi",
            "Növbəti iclas", "Hakim", "İclas sayı" };
        var setirler = list.Select((x, idx) => new object?[]
        {
            idx + 1, x.BorcluAd, x.QeydiyyatNomresi, NovAd(x.Nov), x.Teminat, StatusAd(x.Status),
            x.EsasBorc, x.MehkemeXerci, x.BaslamaTarixi, x.QetnameTarixi,
            x.NovbetiIclasTarix.HasValue
                ? x.NovbetiIclasTarix.Value.ToString("dd.MM.yyyy") + (string.IsNullOrWhiteSpace(x.NovbetiIclasSaat) ? "" : " " + x.NovbetiIclasSaat)
                : "",
            x.Hakim, x.MerheleCount
        });
        var bytes = ExcelExportHelper.Yarat("Məhkəmə İşləri", basliqlar, setirler);
        return File(bytes, ExcelExportHelper.ContentType, $"Mehkeme_Isleri_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> IcraIsleriExcel(string? status)
    {
        var list = await _service.IcraIsleriGetirAsync(status);
        var basliqlar = new[] { "№", "Müştəri", "Kredit hesabı", "Subkod", "Status", "Status mətni",
            "Qalan borc", "Son ödəniş", "Qeydiyyatı", "Əmək haqqı barədə", "Adına sorğu",
            "DYP sorğu tarixi", "Əmlaka həbs", "Stop", "İcra məmuru", "İcraçı son işlər",
            "Doğum tarixi", "Zamin", "Zamin sayı", "Qətnamə tarixi", "İş yeri", "Qeyd" };
        var setirler = list.Select((x, idx) => new object?[]
        {
            idx + 1, x.BorcluAd, x.QeydiyyatNomresi, x.Subkod, StatusAd(x.Status), x.MehkemeStatusMetn,
            x.QalanBorc, x.SonOdenisTarixi, x.Qeydiyyati, x.EmekHaqqiMelumati, x.AdinaSorgu,
            x.DypSorguTarixi, x.EmlakaHebs, x.Stop, x.IcraMemuru, x.IcraSonIsler,
            x.DogumTarixi, x.Zamin, x.ZaminSayi, x.QetnameTarixi, x.IsYeri, x.IcraQeyd
        });
        var bytes = ExcelExportHelper.Yarat("İcra İşləri", basliqlar, setirler);
        return File(bytes, ExcelExportHelper.ContentType, $"Icra_Isleri_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> GoruslerExcel()
    {
        var list = await _service.YaxinlasanGoruslerAsync();
        var basliqlar = new[] { "№", "Borclu", "Qeydiyyat №", "Tarix", "Saat", "Növ", "İş №", "Hakim", "Qeyd" };
        var setirler = list.Select((x, idx) => new object?[]
        {
            idx + 1, x.BorcluAd, x.QeydiyyatNomresi, x.Tarix, x.Saat, MerheleTipiAd(x.MerheleTipi),
            x.IsNomresi, x.Hakim, x.Qeyd
        });
        var bytes = ExcelExportHelper.Yarat("Yaxınlaşan görüşlər", basliqlar, setirler);
        return File(bytes, ExcelExportHelper.ContentType, $"Yaxinlasan_Goruslar_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    // ── Enum → mətn (export üçün) ──
    private static string StatusAd(MehkemeIsiStatus s) => s switch
    {
        MehkemeIsiStatus.Hazirlanir => "Hazırlanır",
        MehkemeIsiStatus.Mehkemede  => "Məhkəmədə",
        MehkemeIsiStatus.Icra       => "İcrada",
        MehkemeIsiStatus.Tamamlandi => "Tamamlandı",
        MehkemeIsiStatus.Baghlandi  => "Bağlandı",
        _                           => s.ToString()
    };
    private static string NovAd(MehkemeIsiNov n) => n switch
    {
        MehkemeIsiNov.Ipoteka    => "İpoteka",
        MehkemeIsiNov.Istehlak   => "İstehlak",
        MehkemeIsiNov.KartKredit => "Kart krediti",
        _                        => "Digər"
    };
    private static string MerheleTipiAd(MerheleTipi t) => t switch
    {
        MerheleTipi.MehkemeIclasi          => "Məhkəmə iclası",
        MerheleTipi.QetnameGeldi           => "Qətnamə gəldi",
        MerheleTipi.EkspertizayaGonderildi => "Ekspertizaya göndərildi",
        MerheleTipi.GeriQaytarildi         => "Geri qaytarıldı",
        MerheleTipi.IddiaVerildi           => "İddia verildi",
        MerheleTipi.QerarVerildi           => "Qərar verildi",
        MerheleTipi.IcraBaglandi           => "İcra bağlandı",
        MerheleTipi.Odendi                 => "Ödənildi",
        _                                  => "Digər"
    };

    // ── Excel "Məhkəmə" sheet → MehkemeIsi arxivi ──────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Import(IFormFile? fayl, bool onizleme = true)
    {
        if (fayl == null || fayl.Length == 0)
        {
            TempData["Error"] = "Fayl seçilməyib.";
            return RedirectToAction(nameof(Isler));
        }

        List<MehkemeCedvelImportDto> isler;
        try
        {
            using var stream = fayl.OpenReadStream();
            var ext = System.IO.Path.GetExtension(fayl.FileName).ToLowerInvariant();
            IWorkbook wb = ext == ".xlsx"
                ? new NPOI.XSSF.UserModel.XSSFWorkbook(stream)
                : new NPOI.HSSF.UserModel.HSSFWorkbook(stream);
            var sheet = SheetTap(wb, "Məhkəmə") ?? SheetTap(wb, "hk") ?? wb.GetSheetAt(0);

            // Sütun xəritəsi (0-əsaslı, "Məhkəmə" vərəqi, data sətir 4-dən):
            // 0=status, 1=Sıra, 2=hesab, 3=subkod, 4=Ad, 8=Girovun növü,
            // 9=Verilmə tarixi, 10=İş №/hakim, 11-dən (tarix,saat) cütləri → iclaslar.
            isler = new List<MehkemeCedvelImportDto>();
            for (int r = 3; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                var ad = Metn(row, 4);
                if (string.IsNullOrWhiteSpace(ad)) continue;

                var d = new MehkemeCedvelImportDto
                {
                    Sira = (int?)Reqem(row, 1),
                    StatusMetn = Metn(row, 0),
                    Hesab = Metn(row, 2),
                    Subkod = Metn(row, 3),
                    BorcluAd = ad.Trim(),
                    GirovunNovu = Metn(row, 8),
                    MehkemeyeVerilmeTarixi = Tarix(row, 9),
                    MehkemeIsNomresi = Metn(row, 10)
                };
                for (int c = 11; c <= 40; c += 2)
                {
                    var t = Tarix(row, c);
                    var saat = Metn(row, c + 1);
                    if (t == null && string.IsNullOrWhiteSpace(saat)) continue;
                    d.Iclaslar.Add(new MehkemeCedvelIclasImportDto { Tarix = t, Saat = saat });
                }
                isler.Add(d);
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "İmport xətası: " + ex.Message;
            return RedirectToAction(nameof(Isler));
        }

        var isciId = await CurrentIsciIdAsync() ?? 0;
        var (isS, merheleS) = await _service.ExcelImportAsync(isler, isciId, onizleme);
        var movcud = isler.Count - isS;
        if (onizleme)
        {
            TempData["Success"] = $"🔎 ÖNİZLƏMƏ (DB-yə yazılmadı) — {isler.Count} sətir: "
                + $"{isS} yeni iş, {merheleS} iclas əlavə olunacaq"
                + (movcud > 0 ? $"; {movcud} mövcud (atlanacaq)" : "") + ".";
        }
        else
        {
            TempData[isS > 0 ? "Success" : "Error"] = isS > 0
                ? $"✅ İdxal edildi — {isS} yeni iş, {merheleS} iclas əlavə olundu"
                  + (movcud > 0 ? $"; {movcud} mövcud (atlandı)" : "") + "."
                : "Yeni iş tapılmadı — bütün sətirlər artıq mövcuddur.";
        }
        return RedirectToAction(nameof(Isler));
    }

    // ── Excel "İCRADA OLAN İŞLƏR" sheet → İcra fazası idxalı ──
    // onizleme=true (defolt — checkbox işarəli): yalnız sayır, DB-yə yazmır.
    // checkbox işarəsiz → onizleme=false → real idxal.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> IcraImport(IFormFile? fayl, bool onizleme = true)
    {
        if (fayl == null || fayl.Length == 0)
        {
            TempData["Error"] = "Fayl seçilməyib.";
            return RedirectToAction(nameof(IcraIsleri));
        }

        List<IcraCedvelImportDto> isler;
        try
        {
            using var stream = fayl.OpenReadStream();
            var ext = System.IO.Path.GetExtension(fayl.FileName).ToLowerInvariant();
            IWorkbook wb = ext == ".xlsx"
                ? new NPOI.XSSF.UserModel.XSSFWorkbook(stream)
                : new NPOI.HSSF.UserModel.HSSFWorkbook(stream);
            var sheet = SheetTap(wb, "İCRADA") ?? SheetTap(wb, "ICRADA") ?? SheetTap(wb, "İcra") ?? wb.GetSheetAt(0);

            // Sütun xəritəsi (0-əsaslı): 1=son ödəniş/status, 2=qalan borc, 3=qeydiyyatı,
            // 4=ə/h, 5=DYP tarix, 6=adına sorğu, 7=əmlaka həbs, 8=stop, 9=icra məmuru,
            // 10=son işlər, 11=doğum, 12=ZAMİN adı, 13=borclu adı, 14=hesab, 15=subkod,
            // 16=qətnamə tarixi, 17=iş yeri, 18/19=qeydlər
            var qruplar = new Dictionary<string, IcraCedvelImportDto>(StringComparer.OrdinalIgnoreCase);
            for (int r = 1; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                var hesab = Metn(row, 14);
                if (string.IsNullOrWhiteSpace(hesab)) continue;   // hesabsız sətir atılır
                hesab = hesab.Trim();
                var subkod = NormSubkod(Metn(row, 15));
                var key = hesab + "|" + subkod;

                if (!qruplar.TryGetValue(key, out var d))
                {
                    d = new IcraCedvelImportDto { Hesab = hesab, Subkod = subkod };
                    qruplar[key] = d;
                }

                var ad = Metn(row, 13);
                if (!string.IsNullOrWhiteSpace(ad) && string.IsNullOrWhiteSpace(d.BorcluAd))
                    d.BorcluAd = ad.Trim();

                var zaminAd = Metn(row, 12);
                if (!string.IsNullOrWhiteSpace(zaminAd))
                {
                    // Zamin sətri — bu sətrin icra məlumatı zaminə aiddir
                    d.Zaminler.Add(new IcraZaminImportDto
                    {
                        Ad               = zaminAd.Trim(),
                        DogumTarixi      = Metn(row, 11),
                        EmekHaqqiTutulma = Metn(row, 4),
                        DypSorgu         = Metn(row, 5),
                        AdinaSorgu       = Metn(row, 6),
                        EmlakaHebs       = Metn(row, 7),
                        Stop             = Metn(row, 8),
                        IcraMemuru       = Metn(row, 9),
                        IcraSonIsler     = Metn(row, 10),
                        IsYeri           = Metn(row, 17),
                        IcraQeyd         = Metn(row, 18) ?? Metn(row, 19)
                    });
                }
                else
                {
                    // Borclu (iş) sətri — iş səviyyəsi sahələri (ilk boş olmayan dəyər saxlanır)
                    var sonOdenis  = Tarix(row, 1);
                    var statusMetn = Metn(row, 1);
                    if (sonOdenis.HasValue) d.SonOdenisTarixi ??= sonOdenis;
                    else if (!string.IsNullOrWhiteSpace(statusMetn)) d.StatusMetn ??= statusMetn;

                    d.QalanBorc         ??= (decimal?)Reqem(row, 2);
                    d.Qeydiyyati        ??= Metn(row, 3);
                    d.EmekHaqqiMelumati ??= Metn(row, 4);
                    d.DypSorguTarixi    ??= Tarix(row, 5);
                    d.AdinaSorgu        ??= Metn(row, 6);
                    d.EmlakaHebs        ??= Metn(row, 7);
                    d.Stop              ??= Metn(row, 8);
                    d.IcraMemuru        ??= Metn(row, 9);
                    d.IcraSonIsler      ??= Metn(row, 10);
                    d.DogumTarixi       ??= Tarix(row, 11);
                    d.QetnameTarixi     ??= Tarix(row, 16);
                    d.IsYeri            ??= Metn(row, 17);
                    d.IcraQeyd          ??= Metn(row, 18) ?? Metn(row, 19);

                    if (statusMetn != null && statusMetn.ToUpperInvariant().Contains("BAĞLAND"))
                        d.Arxiv = true;
                }
            }

            foreach (var d in qruplar.Values)
                if (d.Subkod == "42") d.Arxiv = true;   // PID daxili kodu — aktiv deyil (arxiv)

            isler = qruplar.Values.ToList();
        }
        catch (Exception ex)
        {
            TempData["Error"] = "İmport xətası: " + ex.Message;
            return RedirectToAction(nameof(IcraIsleri));
        }

        var isciId = await CurrentIsciIdAsync() ?? 0;
        var n = await _service.IcraCedvelImportAsync(isler, isciId, onizleme);

        var mesaj = (n.Onizleme ? "🔎 ÖNİZLƏMƏ (DB-yə yazılmadı) — " : "✅ İdxal edildi — ")
            + $"{n.CemQrup} iş qrupu: {n.YeniIs} yeni, {n.Yukseldilen} icraya yüksəldildi, {n.Atlanan} dəyişmədi; "
            + $"{n.Aktiv} aktiv / {n.Arxiv} arxiv; {n.Zamin} zamin"
            + (n.Xeta > 0 ? $"; {n.Xeta} hesabsız" : "") + ".";
        if (n.Numuneler.Count > 0)
            mesaj += " Nümunə → " + string.Join("  •  ", n.Numuneler);
        TempData["Success"] = mesaj;
        return RedirectToAction(nameof(IcraIsleri));
    }

    private static string NormSubkod(string? s)
    {
        s = (s ?? "").Trim();
        if (s.Length == 1 && char.IsDigit(s[0])) return "0" + s;   // "6" → "06"
        return s;
    }

    // ── NPOI xana köməkçiləri (Excel "Məhkəmə" sheet) ──────
    private static ISheet? SheetTap(IWorkbook wb, string contains)
    {
        for (int i = 0; i < wb.NumberOfSheets; i++)
            if (wb.GetSheetName(i).Contains(contains, StringComparison.OrdinalIgnoreCase))
                return wb.GetSheetAt(i);
        return null;
    }

    private static string? Metn(IRow row, int c)
    {
        var cell = row.GetCell(c);
        if (cell == null) return null;
        return cell.CellType switch
        {
            CellType.String  => string.IsNullOrWhiteSpace(cell.StringCellValue) ? null : cell.StringCellValue.Trim(),
            CellType.Numeric => cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
            CellType.Boolean => cell.BooleanCellValue ? "1" : "0",
            CellType.Formula => SafeFormula(cell),
            _ => null
        };
    }

    private static string? SafeFormula(ICell cell)
    {
        try { return cell.StringCellValue?.Trim(); }
        catch { try { return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture); } catch { return null; } }
    }

    private static double? Reqem(IRow row, int c)
    {
        var cell = row.GetCell(c);
        if (cell == null) return null;
        if (cell.CellType == CellType.Numeric) return cell.NumericCellValue;
        if (cell.CellType == CellType.String && double.TryParse(cell.StringCellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
        return null;
    }

    private static readonly string[] _tarixFormatlar = { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "dd/MM/yyyy" };

    private static DateTime? Tarix(IRow row, int c)
    {
        var cell = row.GetCell(c);
        if (cell == null) return null;
        try
        {
            if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                return cell.DateCellValue;
        }
        catch { /* tarix deyil */ }

        var s = Metn(row, c);
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParseExact(s, _tarixFormatlar, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2))
            return dt2;
        return null;
    }

    // ── Qərardad yaz (inline, AJAX — qeyd yoxdursa yaradır) ─
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QerardadYaz(MehkemeKreditAcarDto acar, string? qerardad)
    {
        if (string.IsNullOrWhiteSpace(acar.KreditHesabi))
            return Json(new { success = false, message = "Kredit hesabı tapılmadı." });

        var isciId = await CurrentIsciIdAsync() ?? 0;
        var id = await _service.QerardadYazAsync(acar, qerardad, isciId);
        return Json(new { success = true, id });
    }

    // ── İş aç (izləmə qeydi yarat → Detal) ─────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IsAch(MehkemeKreditAcarDto acar, string? zaminler)
    {
        if (string.IsNullOrWhiteSpace(acar.KreditHesabi))
        {
            TempData["Error"] = "Kredit hesabı tapılmadı.";
            return RedirectToAction("Index");
        }

        var isciId = await CurrentIsciIdAsync() ?? 0;
        var rec = await _service.IsAchAsync(acar, isciId);
        await SnapshotZaminlerAsync(rec.Id, zaminler, isciId);
        return RedirectToAction("Detal", new { id = rec.Id });
    }

    // ── Aç + zaminləri ana sorğu datasından avtomatik doldur ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcVeBax(int id, string? zaminler)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        await SnapshotZaminlerAsync(id, zaminler, isciId);
        return RedirectToAction("Detal", new { id });
    }

    private async Task SnapshotZaminlerAsync(int mehkemeIsiId, string? zaminlerJson, int isciId)
    {
        if (string.IsNullOrWhiteSpace(zaminlerJson)) return;
        try
        {
            var list = System.Text.Json.JsonSerializer.Deserialize<List<MehkemeZaminDto>>(zaminlerJson);
            if (list != null && list.Count > 0)
                await _service.ZaminleriSnapshotEtAsync(mehkemeIsiId, list, isciId);
        }
        catch { /* JSON səhvdirsə snapshot atlanır */ }
    }

    // ── Yarat formu ───────────────────────────────────────
    public IActionResult Yarat() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(MehkemeIsiCreateDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        if (string.IsNullOrWhiteSpace(dto.QeydiyyatNomresi) || string.IsNullOrWhiteSpace(dto.BorcluAd))
        {
            ModelState.AddModelError("", "Qeydiyyat nömrəsi və borclu adı məcburidir.");
            return View(dto);
        }

        var isciId = await CurrentIsciIdAsync() ?? 0;
        var entity = await _service.YaratAsync(dto, isciId);
        TempData["Success"] = "Məhkəmə işi yaradıldı.";
        return RedirectToAction("Detal", new { id = entity.Id });
    }

    // ── Detal ─────────────────────────────────────────────
    public async Task<IActionResult> Detal(int id)
    {
        var model = await _service.DetailGetirAsync(id);
        if (model == null) return NotFound();
        return View(model);
    }

    // ── Yenilə (inline, AJAX) ─────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yenile(int id, MehkemeIsiUpdateDto dto)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        var ok = await _service.YenileAsync(id, dto, isciId);
        if (!ok) return Json(new { success = false, message = "Tapılmadı." });
        return Json(new { success = true, message = "Yeniləndi." });
    }

    // ── Məhkəmə fazası yenilə (yalnız litiqasiya sahələri) ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MehkemeFaza(int id, MehkemeIsiUpdateDto dto)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        var ok = await _service.MehkemeFazaYenileAsync(id, dto, isciId);
        TempData[ok ? "Success" : "Error"] = ok ? "Məhkəmə fazası yeniləndi." : "Tapılmadı.";
        return RedirectToAction("Detal", new { id });
    }

    // ── İcra fazası yenilə (yalnız icra/borclu sahələri) ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IcraFaza(int id, MehkemeIsiUpdateDto dto)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        var ok = await _service.IcraFazaYenileAsync(id, dto, isciId);
        TempData[ok ? "Success" : "Error"] = ok ? "İcra fazası yeniləndi." : "Tapılmadı.";
        return RedirectToAction("Detal", new { id });
    }

    // ── Sil ───────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        var ok = await _service.SilAsync(id, isciId);
        TempData[ok ? "Success" : "Error"] = ok ? "Silindi." : "Tapılmadı.";
        return RedirectToAction("Index");
    }

    // ── Oracle kredit siyahısı ────────────────────────────
    [HttpGet]
    public async Task<IActionResult> OracleKreditler(string nomre)
    {
        if (string.IsNullOrWhiteSpace(nomre))
            return Json(new { success = false, message = "Nömrə daxil edin." });

        try
        {
            var rows = await _service.OracleKreditlerGetirAsync(nomre);
            if (rows.Count == 0)
                return Json(new { success = false, message = "Bu nömrəyə aktiv kredit tapılmadı." });
            return Json(new { success = true, rows });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ── Mərhələ əlavə et ─────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MerheleElave(MehkemeMerheleCreateDto dto, IFormFile? fayl)
    {
        if (dto.MehkemeIsiId <= 0)
            return Json(new { success = false, message = "İş ID tapılmadı." });

        var isciId = await CurrentIsciIdAsync() ?? 0;
        await _service.MerheleElavEtAsync(dto, fayl, DmsRoot, isciId);
        TempData["Success"] = "Mərhələ əlavə edildi.";
        return RedirectToAction("Detal", new { id = dto.MehkemeIsiId });
    }

    // ── Mərhələ sil ───────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MerheleSil(int merheleId, int ishId)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        await _service.MerheleSilAsync(merheleId, isciId);
        return RedirectToAction("Detal", new { id = ishId });
    }

    // ── Məhkəmə xərci əlavə / sil ─────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XercElave(MehkemeXerciCreateDto dto)
    {
        if (dto.MehkemeIsiId <= 0 || dto.Mebleg <= 0)
        {
            TempData["Error"] = "Xərc məbləği düzgün deyil.";
            return RedirectToAction("Detal", new { id = dto.MehkemeIsiId });
        }
        var isciId = await CurrentIsciIdAsync() ?? 0;
        await _service.XercElaveEtAsync(dto, isciId);
        TempData["Success"] = "Məhkəmə xərci əlavə edildi.";
        return RedirectToAction("Detal", new { id = dto.MehkemeIsiId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XercSil(int xerciId, int ishId)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        await _service.XercSilAsync(xerciId, isciId);
        return RedirectToAction("Detal", new { id = ishId });
    }

    // ── Sənəd yüklə / sil ─────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SenedYukle(MehkemeSenedCreateDto dto, IFormFile? fayl)
    {
        if (dto.MehkemeIsiId <= 0 || fayl == null || fayl.Length == 0)
        {
            TempData["Error"] = "Fayl seçilməyib.";
            return RedirectToAction("Detal", new { id = dto.MehkemeIsiId });
        }
        var isciId = await CurrentIsciIdAsync() ?? 0;
        await _service.SenedYukleAsync(dto, fayl, DmsRoot, isciId);
        TempData["Success"] = "Sənəd yükləndi.";
        return RedirectToAction("Detal", new { id = dto.MehkemeIsiId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SenedSil(int senedId, int ishId)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        await _service.SenedSilAsync(senedId, isciId);
        return RedirectToAction("Detal", new { id = ishId });
    }

    // ── Zamin (icra subyekti) ─────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ZaminElave(ZaminIcraCreateDto dto)
    {
        if (dto.MehkemeIsiId <= 0 || string.IsNullOrWhiteSpace(dto.Ad))
        {
            TempData["Error"] = "Zamin adı və iş ID lazımdır.";
            return RedirectToAction("Detal", new { id = dto.MehkemeIsiId });
        }
        var isciId = await CurrentIsciIdAsync() ?? 0;
        await _service.ZaminElaveEtAsync(dto, isciId);
        TempData["Success"] = "Zamin əlavə edildi.";
        return RedirectToAction("Detal", new { id = dto.MehkemeIsiId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ZaminYenile(ZaminIcraUpdateDto dto)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        await _service.ZaminYenileAsync(dto, isciId);
        TempData["Success"] = "Zamin yeniləndi.";
        return RedirectToAction("Detal", new { id = dto.MehkemeIsiId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ZaminSil(int zaminId, int ishId)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        await _service.ZaminSilAsync(zaminId, isciId);
        return RedirectToAction("Detal", new { id = ishId });
    }

    // ── Zaminləri Oracle-dan çək (kimlik avtomatik) ──────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ZaminleriYukle(int ishId)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        var sayi = await _service.ZaminleriOracledanYukleAsync(ishId, isciId);
        TempData["Success"] = sayi > 0
            ? $"{sayi} zamin Oracle-dan yükləndi."
            : "Yeni zamin tapılmadı (artıq yüklənib və ya Oracle-da bu kreditin zamini yoxdur).";
        return RedirectToAction("Detal", new { id = ishId });
    }
}
