using System.Globalization;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    // PID — yalnız Problemli İşlər. İndilik Admin/HR/Rəhbər; departament/rol dəqiqləşməsi sonra.
    [Authorize(Roles = "Admin,HR,Rehber")]
    public class PidController : Controller
    {
        private readonly IUnitOfWork _uow;
        public PidController(IUnitOfWork uow) => _uow = uow;

        // ── Məhkəmə işləri siyahısı ──────────────────────────────
        public async Task<IActionResult> Index(string? axtar)
        {
            var query = _uow.Repository<PidMehkemeIsi>()
                .Query().AsNoTracking()
                .Where(x => !x.Silinib)
                .Include(x => x.Iclaslar)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(axtar))
                query = query.Where(x => x.BorcluAd.Contains(axtar)
                                      || (x.KreditHesabi != null && x.KreditHesabi.Contains(axtar))
                                      || (x.MehkemeSenedi != null && x.MehkemeSenedi.Contains(axtar)));

            var list = await query.OrderBy(x => x.Sira).ThenBy(x => x.Id).ToListAsync();
            ViewBag.Axtar = axtar;
            ViewData["Title"] = "PID — Məhkəmə İşləri";
            return View(list);
        }

        // ── Excel import (köhnə "Məhkəmə 2021-2026" sheet) ───────
        [HttpPost]
        [RequestSizeLimit(60_000_000)]
        public async Task<IActionResult> Import(IFormFile? fayl)
        {
            if (fayl == null || fayl.Length == 0)
            {
                TempData["Error"] = "Fayl seçilməyib.";
                return RedirectToAction(nameof(Index));
            }

            int elaveIs = 0, elaveIclas = 0;
            try
            {
                using var stream = fayl.OpenReadStream();
                var wb = new HSSFWorkbook(stream);
                var sheet = SheetTap(wb, "Məhkəmə") ?? wb.GetSheetAt(0);

                // Data ~4-cü sətirdən (0-əsaslı 3). Sütun xəritəsi:
                //  A0=Status, B1=Sıra, C2=Ad, G6=KreditNövü, H7=VerilməTarixi, I8=Sənəd/Hakim
                //  J9-dan başlayaraq (tarix, saat) cütləri → iclaslar
                for (int r = 3; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    var ad = Metn(row, 2);
                    if (string.IsNullOrWhiteSpace(ad)) continue;   // borclu adı yoxdursa keç

                    var isi = new PidMehkemeIsi
                    {
                        Status                 = Metn(row, 0),
                        Sira                   = (int?)Reqem(row, 1),
                        BorcluAd               = ad.Trim(),
                        KreditNovu             = Metn(row, 6),
                        MehkemeyeVerilmeTarixi = Tarix(row, 7),
                        MehkemeSenedi          = Metn(row, 8),
                    };

                    // İclaslar — 9-cu sütundan (tarix, saat) cütləri ilə
                    for (int c = 9; c <= 24; c += 2)
                    {
                        var t = Tarix(row, c);
                        var saat = Metn(row, c + 1);
                        if (t == null && string.IsNullOrWhiteSpace(saat)) continue;
                        isi.Iclaslar.Add(new PidMehkemeIclas { Tarix = t, Saat = saat });
                        elaveIclas++;
                    }

                    await _uow.Repository<PidMehkemeIsi>().YaratAsync(isi);
                    elaveIs++;
                }

                await _uow.YaddaSaxlaAsync();
                TempData["Success"] = $"İmport: {elaveIs} iş, {elaveIclas} iclas əlavə olundu.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"İmport xətası: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── Köməkçilər (NPOI .xls xana oxuma) ────────────────────
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
}
