using FinNex.Application.DTOs.Pid;
using FinNex.Application.Interfaces.Pid;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.Globalization;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize(Roles = "Admin,PID")]
public class MehkemeCedvelController : Controller
{
    private readonly IMehkemeCedvelService _service;
    private readonly UserManager<AppUser> _userManager;

    public MehkemeCedvelController(IMehkemeCedvelService service, UserManager<AppUser> userManager)
    {
        _service = service;
        _userManager = userManager;
    }

    private async Task<int> CurrentIsciIdAsync()
    {
        var u = await _userManager.GetUserAsync(User);
        return u?.IsciId ?? 0;
    }

    public async Task<IActionResult> Index(string? axtaris)
    {
        var list = await _service.HamisiniGetirAsync(axtaris);
        ViewData["Axtaris"] = axtaris;
        return View(list);
    }

    // ── Excel import (NPOI ilə .xls "Məhkəmə" sheet-i) ─────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? fayl)
    {
        if (fayl == null || fayl.Length == 0)
        {
            TempData["Error"] = "Fayl seçilməyib.";
            return RedirectToAction(nameof(Index));
        }

        List<MehkemeCedvelImportDto> isler;
        try
        {
            using var stream = fayl.OpenReadStream();
            var wb = new HSSFWorkbook(stream);
            var sheet = SheetTap(wb, "Məhkəmə") ?? SheetTap(wb, "hk") ?? wb.GetSheetAt(0);

            // Sütun xəritəsi (Excel "Məhkəmə 2021-2026" — başlıq 3-cü sətir, data 4-cü sətirdən):
            //  1=Sıra, 2=Ad, 6=Girovun növü, 7=Verilmə tarixi, 8=İş №/hakim
            //  9-dan (tarix, saat) cütləri → iclaslar
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
            return RedirectToAction(nameof(Index));
        }

        var (isS, iclasS) = await _service.ImportAsync(isler, await CurrentIsciIdAsync());
        TempData["Success"] = $"İmport: {isS} iş, {iclasS} iclas əlavə olundu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(MehkemeCedvelCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.BorcluAd))
        {
            TempData["Error"] = "Müştəri adı məcburidir.";
            return RedirectToAction(nameof(Index));
        }
        await _service.YaratAsync(dto, await CurrentIsciIdAsync());
        TempData["Success"] = "İş əlavə edildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        await _service.SilAsync(id, await CurrentIsciIdAsync());
        TempData["Success"] = "Silindi.";
        return RedirectToAction(nameof(Index));
    }

    // ── NPOI .xls xana köməkçiləri ───────────────────────
    private static ISheet? SheetTap(HSSFWorkbook wb, string contains)
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
}
