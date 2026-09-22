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

    // POST: .xlsx faylını oxuyur (Ad Soyad Ata adı | VÖEN | FİN sütunları, 1-ci sətir başlıq),
    // Oracle-da (yalnız SELECT) yoxlayır və nəticəni səhifədə göstərir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MelumatBazasiYukle(IFormFile fayl)
    {
        if (fayl == null || fayl.Length == 0)
        {
            TempData["Error"] = "Excel faylı seçilməyib.";
            return RedirectToAction(nameof(MelumatBazasi));
        }

        var setirler = new List<AxtarisSetriDto>();
        try
        {
            using var stream = fayl.OpenReadStream();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheet(1);
            var sira = 1;
            foreach (var row in ws.RangeUsed()!.RowsUsed().Skip(1)) // 1-ci sətir başlıqdır
            {
                var ad   = row.Cell(1).GetString().Trim();
                var voen = row.Cell(2).GetString().Trim();
                var fin  = row.Cell(3).GetString().Trim();
                if (ad.Length == 0 && voen.Length == 0 && fin.Length == 0) continue; // boş sətir
                setirler.Add(new AxtarisSetriDto { Sira = sira++, AdSoyadAta = ad, Voen = voen, Fin = fin });
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Excel faylı oxuna bilmədi: " + ex.Message;
            return RedirectToAction(nameof(MelumatBazasi));
        }

        if (setirler.Count == 0)
        {
            TempData["Error"] = "Excel faylında oxunacaq sətir tapılmadı. Sütunlar: A=Ad Soyad Ata adı, B=VÖEN, C=FİN (1-ci sətir başlıq).";
            return RedirectToAction(nameof(MelumatBazasi));
        }

        var netice = await _service.AxtarilanlariYoxlaAsync(setirler);
        return View("MelumatBazasi", netice);
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
        string[] basliqlar = { "№", "Ad Soyad Ata adı", "VÖEN", "FİN", "Tapıldı", "Mənbə", "Uyğun ad", "Uyğun regnom", "Uyğun sahə" };
        for (int c = 0; c < basliqlar.Length; c++) hdr.CreateCell(c).SetCellValue(basliqlar[c]);

        int r = 1;
        foreach (var setir in netice.Setirler)
        {
            if (setir.Uygunluqlar.Count == 0)
            {
                var row = sh.CreateRow(r++);
                row.CreateCell(0).SetCellValue(setir.Axtarilan.Sira);
                row.CreateCell(1).SetCellValue(setir.Axtarilan.AdSoyadAta ?? "");
                row.CreateCell(2).SetCellValue(setir.Axtarilan.Voen ?? "");
                row.CreateCell(3).SetCellValue(setir.Axtarilan.Fin ?? "");
                row.CreateCell(4).SetCellValue("Xeyr");
            }
            else
            {
                foreach (var u in setir.Uygunluqlar)
                {
                    var row = sh.CreateRow(r++);
                    row.CreateCell(0).SetCellValue(setir.Axtarilan.Sira);
                    row.CreateCell(1).SetCellValue(setir.Axtarilan.AdSoyadAta ?? "");
                    row.CreateCell(2).SetCellValue(setir.Axtarilan.Voen ?? "");
                    row.CreateCell(3).SetCellValue(setir.Axtarilan.Fin ?? "");
                    row.CreateCell(4).SetCellValue("Bəli");
                    row.CreateCell(5).SetCellValue(u.Menbe);
                    row.CreateCell(6).SetCellValue(u.AdSoyad ?? "");
                    row.CreateCell(7).SetCellValue(u.Regnom ?? "");
                    row.CreateCell(8).SetCellValue(u.UygunSahe ?? "");
                }
            }
        }

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        var ad = $"Melumat_bazasi_axtaris_{DateTime.Now:yyyyMMdd_HHmm}.xls";
        return File(ms.ToArray(), "application/vnd.ms-excel", ad);
    }
}
