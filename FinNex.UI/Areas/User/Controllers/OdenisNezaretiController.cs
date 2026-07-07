using FinNex.Application.DTOs.Pid;
using FinNex.Application.Interfaces.Pid;
using FinNex.Domain;
using FinNex.Domain.Entities.Pid;
using FinNex.UI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize(Roles = "Admin,PID")]
public class OdenisNezaretiController : Controller
{
    private readonly IOdenisNezaretiService _service;
    private readonly UserManager<AppUser> _userManager;

    public OdenisNezaretiController(IOdenisNezaretiService service, UserManager<AppUser> userManager)
    {
        _service = service;
        _userManager = userManager;
    }

    private async Task<int> CurrentIsciIdAsync()
    {
        var u = await _userManager.GetUserAsync(User);
        return u?.IsciId ?? 0;
    }

    // Oracle canlı siyahı — Aktiv Müştərilər + ARH_DD son ödəniş (yalnız oxuma)
    public async Task<IActionResult> Index()
    {
        var vm = await _service.OracleSiyahiAsync();
        return View(vm);
    }

    // ── Excel export (Oracle canlı siyahı) ────────────────────────
    public async Task<IActionResult> IndexExcel()
    {
        var vm = await _service.OracleSiyahiAsync();
        var basliqlar = new[] { "№", "Region", "Müştəri", "Kredit hesabı", "K.S.", "Kreditin növü",
            "Tam qalıq", "Qalıq", "V/K qalıq", "Status", "Sistem son əməliyyat",
            "Son ödəniş tarixi", "Son ödəniş məbləği", "Ödəniş cəmi", "Ödəniş sayı" };
        var setirler = vm.Setirler.Select((x, idx) => new object?[]
        {
            idx + 1, x.Region, x.Musteri, x.KreditHesabi, x.Ks, x.KreditinNovu,
            x.TamQaliq, x.Qaliq, x.VkQaliq, x.Status, x.SistemSonEmel,
            x.SonOdenisTarixi, x.SonOdenisMeblegi, x.OdenisCemi, x.OdenisSayi
        });
        var bytes = ExcelExportHelper.Yarat("Ödənişə Nəzarət", basliqlar, setirler);
        return File(bytes, ExcelExportHelper.ContentType, $"Odenise_Nezaret_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(OdenisNezaretiCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.MusteriAdi))
        {
            TempData["Error"] = "Müştəri adı məcburidir.";
            return RedirectToAction(nameof(Index));
        }
        await _service.YaratAsync(dto, await CurrentIsciIdAsync());
        TempData["Success"] = "Qeyd əlavə edildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yenile(OdenisNezaretiUpdateDto dto)
    {
        var ok = await _service.YenileAsync(dto, await CurrentIsciIdAsync());
        TempData[ok ? "Success" : "Error"] = ok ? "Yeniləndi." : "Qeyd tapılmadı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        await _service.SilAsync(id, await CurrentIsciIdAsync());
        TempData["Success"] = "Qeyd silindi.";
        return RedirectToAction(nameof(Index));
    }
}
