using FinNex.Application.DTOs.Pid;
using FinNex.Application.Interfaces.Pid;
using FinNex.Domain;
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

    // ── Excel "Məhkəmə" sheet → MehkemeIsi arxivi ──────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Import(IFormFile? fayl)
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

            // Sütun xəritəsi: 1=Sıra, 2=Ad, 6=Girovun növü, 7=Verilmə tarixi,
            //                 8=İş №/hakim, 9-dan (tarix,saat) cütləri → iclaslar
            isler = new List<MehkemeCedvelImportDto>();
            for (int r = 3; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                var ad = Metn(row, 2);
                if (string.IsNullOrWhiteSpace(ad)) continue;

                var d = new MehkemeCedvelImportDto
                {
                    Sira = (int?)Reqem(row, 1),
                    BorcluAd = ad.Trim(),
                    GirovunNovu = Metn(row, 6),
                    MehkemeyeVerilmeTarixi = Tarix(row, 7),
                    MehkemeIsNomresi = Metn(row, 8)
                };
                for (int c = 9; c <= 40; c += 2)
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
        var (isS, merheleS) = await _service.ExcelImportAsync(isler, isciId);
        TempData[isS > 0 ? "Success" : "Error"] = isS > 0
            ? $"İmport: {isS} yeni iş, {merheleS} iclas əlavə olundu."
            : "Yeni iş tapılmadı — bütün sətirlər artıq mövcuddur.";
        return RedirectToAction(nameof(Isler));
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
