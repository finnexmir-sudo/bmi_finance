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
    // Filtrlər (səhifədəki JS ilə eyni məntiq) query-dən gəlir ki, Excel yalnız
    // görünən (süzülmüş) sətirləri versin — status/item10 kiçik hərflə müqayisə olunur.
    public async Task<IActionResult> IndexExcel(
        string? axtaris, string? status, string? item10, int? il, int? ay, bool onlyNo = false)
    {
        var vm = await _service.OracleSiyahiAsync();
        var setirlerF = vm.Setirler.AsEnumerable();

        if (onlyNo)
            setirlerF = setirlerF.Where(x => x.OdenisYox);
        if (!string.IsNullOrWhiteSpace(status))
            setirlerF = setirlerF.Where(x => (x.Status ?? "").Trim().ToLowerInvariant() == status.Trim().ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(item10))
            setirlerF = setirlerF.Where(x => (x.Item10 ?? "").Trim().ToLowerInvariant() == item10.Trim().ToLowerInvariant());
        if (il.HasValue)
            setirlerF = setirlerF.Where(x => x.SonOdenisIl == il.Value);
        if (ay.HasValue)
            setirlerF = setirlerF.Where(x => x.SonOdenisAy == ay.Value);
        if (!string.IsNullOrWhiteSpace(axtaris))
        {
            var q = axtaris.Trim().ToLowerInvariant();
            setirlerF = setirlerF.Where(x =>
                (x.Musteri + " " + x.KreditHesabi + " " + x.Ks + " " + (x.Status ?? "") + " " + (x.Item10 ?? ""))
                    .ToLowerInvariant().Contains(q));
        }

        var filtered = setirlerF.ToList();
        var basliqlar = new[] { "№", "Region", "Müştəri", "Kredit hesabı", "K.S.", "Kreditin növü",
            "Tam qalıq", "Qalıq", "V/K qalıq", "Status", "Item 10", "Sistem son əməliyyat",
            "Son ödəniş tarixi", "Son ödəniş məbləği", "Ödəniş cəmi", "Ödəniş sayı" };
        var setirler = filtered.Select((x, idx) => new object?[]
        {
            idx + 1, x.Region, x.Musteri, x.KreditHesabi, x.Ks, x.KreditinNovu,
            x.TamQaliq, x.Qaliq, x.VkQaliq, x.Status, x.Item10, x.SistemSonEmel,
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
