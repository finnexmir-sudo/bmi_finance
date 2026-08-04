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

    // ── GRAY LIST tabı — ARH_DD (debet 45019…, kredit 89150…), müştəri üzrə qruplu ──
    public async Task<IActionResult> Gray()
    {
        var vm = await _service.GraySiyahiAsync();
        return View(vm);
    }

    // Gray list Excel — səhifə ilə eyni struktur: müştəri xülasəsi + hər ödəniş sətri
    public async Task<IActionResult> GrayExcel(string? axtaris)
    {
        var vm = await _service.GraySiyahiAsync();
        var setirlerF = vm.Setirler.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(axtaris))
        {
            var q = axtaris.Trim().ToLowerInvariant();
            setirlerF = setirlerF.Where(x => x.Musteri.ToLowerInvariant().Contains(q));
        }
        var filtered = setirlerF.ToList();

        var bu = DateTime.Today; var kecen = bu.AddMonths(-1);
        var basliqlar = new[] { "№", "Müştəri", "Tarix", "Məbləğ",
            $"Cari ay ({bu:MM.yyyy})", $"Keçən ay ({kecen:MM.yyyy})", "Cəmi", "Ödəniş sayı", "Təyinat" };

        // Hər müştəri üçün: xülasə sətri + altında ödəniş sətirləri (tarix DESC)
        var setirler = new List<object?[]>();
        int idx = 1;
        foreach (var m in filtered)
        {
            setirler.Add(new object?[]
            {
                idx++, m.Musteri,
                m.SonOdenisTarixi?.ToString("dd.MM.yyyy"), m.SonOdenisMeblegi,
                m.CariAy, m.KecenAy, m.Cemi, m.Say, null
            });
            foreach (var o in m.Odenisler)
                setirler.Add(new object?[]
                {
                    null, null, o.Tarix?.ToString("dd.MM.yyyy"), o.Mebleg,
                    null, null, null, null, o.Teyinat
                });
        }
        setirler.Add(new object?[]
        {
            null, "CƏMİ:", null, null,
            filtered.Sum(x => x.CariAy), filtered.Sum(x => x.KecenAy),
            filtered.Sum(x => x.Cemi), null, null
        });

        var bytes = ExcelExportHelper.Yarat("Gray list — Ödənişə Nəzarət", basliqlar, setirler);
        return File(bytes, ExcelExportHelper.ContentType, $"Gray_Odenis_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    // ── Excel export (Oracle canlı siyahı) ────────────────────────
    // Filtrlər (səhifədəki JS ilə eyni məntiq) query-dən gəlir ki, Excel yalnız
    // görünən (süzülmüş) sətirləri versin — status/item10 kiçik hərflə müqayisə olunur.
    public async Task<IActionResult> IndexExcel(
        string? axtaris, string? status, string? item10, string? item13, int? il, int? ay, bool onlyNo = false)
    {
        var vm = await _service.OracleSiyahiAsync();
        var setirlerF = vm.Setirler.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(status))
            setirlerF = setirlerF.Where(x => (x.Status ?? "").Trim().ToLowerInvariant() == status.Trim().ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(item10))
            setirlerF = setirlerF.Where(x => (x.Item10 ?? "").Trim().ToLowerInvariant() == item10.Trim().ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(item13))
            setirlerF = setirlerF.Where(x => (x.Item13 ?? "").Trim().ToLowerInvariant() == item13.Trim().ToLowerInvariant());
        // "Ödənişi olmayanlar" + il/ay birlikdə → seçilən ayda ödəniş etməyənlər
        // (son ödəniş həmin aydan əvvəl, ya da heç ödəniş yox). Əks halda adi filtrlər (səhifə JS ilə eyni).
        if (onlyNo && il.HasValue && ay.HasValue)
        {
            var sIl = il.Value; var sAy = ay.Value;
            setirlerF = setirlerF.Where(x => x.OdenisYox
                || (x.SonOdenisIl.HasValue && (x.SonOdenisIl.Value < sIl
                    || (x.SonOdenisIl.Value == sIl && x.SonOdenisAy < sAy))));
        }
        else
        {
            if (onlyNo)
                setirlerF = setirlerF.Where(x => x.OdenisYox);
            if (il.HasValue)
                setirlerF = setirlerF.Where(x => x.SonOdenisIl == il.Value);
            if (ay.HasValue)
                setirlerF = setirlerF.Where(x => x.SonOdenisAy == ay.Value);
        }
        if (!string.IsNullOrWhiteSpace(axtaris))
        {
            var q = axtaris.Trim().ToLowerInvariant();
            setirlerF = setirlerF.Where(x =>
                (x.Musteri + " " + x.KreditHesabi + " " + x.Ks + " " + (x.Status ?? "") + " " + (x.Item10 ?? "") + " " + (x.Item13 ?? ""))
                    .ToLowerInvariant().Contains(q));
        }

        var filtered = setirlerF.ToList();

        // "Ödəniş cəmi"/"Rüsum cəmi" səhifə ilə EYNİ məntiqlə: ay seçilibsə həmin ayın
        // cəmi (cari/keçən ay serverdəki ayrıca aqreqatlardan), seçilməyibsə tam cəm.
        Func<OdenisNezaretSatirDto, decimal?> ocem  = x => x.OdenisCemi;
        Func<OdenisNezaretSatirDto, decimal?> rusum = x => x.RusumCemi;
        var ocemBasliq  = "Ödəniş cəmi";
        var rusumBasliq = "Rüsum cəmi";
        if (il.HasValue && ay.HasValue)
        {
            var bu = DateTime.Today; var kecen = bu.AddMonths(-1);
            if (il.Value == bu.Year && ay.Value == bu.Month)
            { ocem = x => x.OdenisCariAy; rusum = x => x.RusumCariAy; }
            else if (il.Value == kecen.Year && ay.Value == kecen.Month)
            { ocem = x => x.OdenisKecenAy; rusum = x => x.RusumKecenAy; }
            ocemBasliq  = $"Ödəniş cəmi ({ay.Value:00}.{il.Value})";
            rusumBasliq = $"Rüsum cəmi ({ay.Value:00}.{il.Value})";
        }

        // Ümumi = ödəniş + rüsum (kredit üzrə) — işçi əl ilə toplamasın
        Func<OdenisNezaretSatirDto, decimal?> umumi = x =>
            (ocem(x).HasValue || rusum(x).HasValue) ? (ocem(x) ?? 0m) + (rusum(x) ?? 0m) : (decimal?)null;
        var umumiBasliq = il.HasValue && ay.HasValue ? $"Ümumi ({ay.Value:00}.{il.Value})" : "Ümumi (ödəniş+rüsum)";

        var basliqlar = new[] { "№", "Region", "Müştəri", "Kredit hesabı", "K.S.", "Kreditin növü",
            "Tam qalıq", "Qalıq", "V/K qalıq", "Item 01", "Item 10", "Item 13", "Sistem son əməliyyat",
            "Son ödəniş tarixi", "Son ödəniş məbləği", ocemBasliq, rusumBasliq, umumiBasliq, "Ödəniş sayı" };
        var setirler = filtered.Select((x, idx) => new object?[]
        {
            idx + 1, x.Region, x.Musteri, x.KreditHesabi, x.Ks, x.KreditinNovu,
            x.TamQaliq, x.Qaliq, x.VkQaliq, x.Status, x.Item10, x.Item13, x.SistemSonEmel,
            x.SonOdenisTarixi, x.SonOdenisMeblegi, ocem(x), rusum(x), umumi(x), x.OdenisSayi
        }).ToList();

        // Cədvəlin sonunda CƏMİ sətri (səhifədəki footer ilə eyni)
        setirler.Add(new object?[]
        {
            null, null, "CƏMİ:", null, null, null,
            null, null, null, null, null, null, null,
            null, null,
            filtered.Sum(x => ocem(x) ?? 0m),
            filtered.Sum(x => rusum(x) ?? 0m),
            filtered.Sum(x => umumi(x) ?? 0m),
            null
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
