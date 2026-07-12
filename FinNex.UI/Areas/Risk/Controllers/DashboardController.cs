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

    // Risk hesabatlarının siyahısı (kartlar)
    public async Task<IActionResult> Index()
    {
        var model = await _service.HesabatlarAsync();
        return View(model);
    }

    // Bir hesabatı icra edib dinamik cədvəl göstərir
    public async Task<IActionResult> Hesabat(int id)
    {
        var model = await _service.IcraEtAsync(id);
        if (model == null)
        {
            TempData["Error"] = "Hesabat tapılmadı və ya aktiv deyil.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Hesabatlar = await _service.HesabatlarAsync();
        return View(model);
    }

    // Hesabatın nəticəsini Excel-ə ixrac edir
    public async Task<IActionResult> Excel(int id)
    {
        var m = await _service.IcraEtAsync(id);
        if (m == null)
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
}
