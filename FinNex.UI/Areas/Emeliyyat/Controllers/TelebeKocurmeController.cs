using System.Security.Claims;
using FinNex.Application.DTOs.Emeliyyat;
using FinNex.Application.Interfaces.Emeliyyat;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;

namespace FinNex.UI.Areas.Emeliyyat.Controllers;

[Area("Emeliyyat")]
[Authorize]
public class TelebeKocurmeController : Controller
{
    private readonly ITelebeKocurmeService _service;

    public TelebeKocurmeController(ITelebeKocurmeService service)
    {
        _service = service;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin() => User.IsInRole(RoleNames.Admin);

    public async Task<IActionResult> Index(int? il)
    {
        var model = await _service.HamisiniGetirAsync(il);
        ViewBag.Il = il;
        ViewBag.UserId = GetUserId();
        ViewBag.IsAdmin = IsAdmin();
        return View(model);
    }

    [HttpGet]
    public IActionResult Yarat()
    {
        var (h35025, h45023, h45011, h67013) = _service.StandartHesablar();
        return View(new TelebeKocurmeCreateDto
        {
            Tarix = DateTime.Today,
            AlanBank = "Kapital",
            XH = 0.1m,
            Hes35025 = h35025,
            Hes45023 = h45023,
            Hes45011 = h45011,
            Hes67013 = h67013
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(TelebeKocurmeCreateDto dto)
    {
        var res = await _service.YaratAsync(dto, GetUserId());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        if (!res.Success)
            return View(dto);
        TempData["AvtoYukle"] = "1";
        return RedirectToAction(nameof(Detal), new { id = res.Data });
    }

    // Mövcud qeydi təkrar göndər — məlumat dolu, yeni № ilə
    [HttpGet]
    public async Task<IActionResult> Tekrarla(int id)
    {
        var dto = await _service.TekrarMelumatiAsync(id);
        if (dto == null)
        {
            TempData["Error"] = "Qeyd tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Tekrar = true;
        return View("Yarat", dto);
    }

    // Batch sətri (Excel üçün)
    public class ExcelSetir
    {
        public string? Debet { get; set; }
        public string? Kredit { get; set; }
        public decimal Mebleg { get; set; }
        public string? Teyinat { get; set; }
    }

    // TƏLƏBƏ İMPORT şablonuna uyğun .xls qurur (başlıq + sətirlər)
    private static byte[] TelebeExcel(IEnumerable<ExcelSetir> setirler)
    {
        var wb = new HSSFWorkbook();
        var sh = wb.CreateSheet("TƏLƏBƏ İMPORT");
        var hdr = sh.CreateRow(0);
        string[] basliqlar = { "Sənədin\n№", "Əməliyyat\nkodu", "SHD", "Debet", "SHK",
                               "Kredit", "Məbləğ", "Operation\ncode", "Ölkə\nkodu", "Qeyd" };
        for (int c = 0; c < basliqlar.Length; c++)
            hdr.CreateCell(c).SetCellValue(basliqlar[c]);

        int i = 0;
        foreach (var s in setirler)
        {
            var r = sh.CreateRow(++i);                     // 2-ci sətirdən
            r.CreateCell(3).SetCellValue(s.Debet ?? "");   // D
            r.CreateCell(5).SetCellValue(s.Kredit ?? "");  // F
            r.CreateCell(6).SetCellValue((double)s.Mebleg);// G
            r.CreateCell(9).SetCellValue(s.Teyinat ?? ""); // J
        }
        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return ms.ToArray();
    }

    // "Əlavə et" — tələbəni DB-yə yazır (jurnal) və 3 muhasibat sətrini qaytarır (batch üçün)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Elave(TelebeKocurmeCreateDto dto)
    {
        var res = await _service.YaratAsync(dto, GetUserId());
        if (!res.Success)
            return Json(new { ok = false, message = res.Message });

        var setirler = _service.SetirlerHesabla(dto)
            .Select(s => new { s.Debet, s.Kredit, Mebleg = s.Mebleg, s.Teyinat });
        return Json(new { ok = true, message = res.Message, hevaleNo = dto.HevaleNo, setirler });
    }

    // Batch Excel — akkumulyasiya olunmuş bütün sətirləri bir fayla yazır
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExcelBatch(string data)
    {
        var opts = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rows = System.Text.Json.JsonSerializer.Deserialize<List<ExcelSetir>>(data ?? "[]", opts) ?? new();
        var bytes = TelebeExcel(rows);
        return File(bytes, "application/vnd.ms-excel", "TELEBE_IMPORT.xls");
    }

    // Tək qeydin Excel ixracı (Detal-dan)
    public async Task<IActionResult> ExcelIxrac(int id)
    {
        var m = await _service.DetalAsync(id);
        if (m == null) { TempData["Error"] = "Qeyd tapılmadı."; return RedirectToAction(nameof(Index)); }
        var setirler = m.Setirler.Select(s => new ExcelSetir { Debet = s.Debet, Kredit = s.Kredit, Mebleg = s.Mebleg, Teyinat = s.Teyinat });
        var bytes = TelebeExcel(setirler);
        return File(bytes, "application/vnd.ms-excel", $"Telebe_{(m.HevaleNo ?? id.ToString()).Replace("/", "-")}.xls");
    }

    public async Task<IActionResult> Detal(int id)
    {
        var model = await _service.DetalAsync(id);
        if (model == null)
        {
            TempData["Error"] = "Qeyd tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.UserId = GetUserId();
        ViewBag.IsAdmin = IsAdmin();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Redakte(int id)
    {
        var dto = await _service.RedakteMelumatiAsync(id);
        if (dto == null)
        {
            TempData["Error"] = "Qeyd tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        if (!IsAdmin() && dto.YaradanId != GetUserId())
        {
            TempData["Error"] = "Bu qeydi yalnız yaradan və ya Admin dəyişə bilər.";
            return RedirectToAction(nameof(Index));
        }
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Redakte(TelebeKocurmeEditDto dto)
    {
        var res = await _service.YenileAsync(dto, GetUserId(), IsAdmin());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        if (!res.Success)
            return View(dto);
        return RedirectToAction(nameof(Detal), new { id = dto.Id });
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
