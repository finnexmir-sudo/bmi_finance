using System.Text.Json;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Pid;
using FinNex.Application.Interfaces.Sorgular;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class PidSmsController : Controller
{
    private readonly IPidSmsService _smsService;
    private readonly IPidSmsSablonService _sablonService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IOracleService _oracle;
    private readonly IOracleSorguService _sorguService;
    private readonly ISistemAyarService _sistemAyar;

    public PidSmsController(
        IPidSmsService smsService,
        IPidSmsSablonService sablonService,
        UserManager<AppUser> userManager,
        IOracleService oracle,
        IOracleSorguService sorguService,
        ISistemAyarService sistemAyar)
    {
        _smsService = smsService;
        _sablonService = sablonService;
        _userManager = userManager;
        _oracle = oracle;
        _sorguService = sorguService;
        _sistemAyar = sistemAyar;
    }

    private async Task<int?> CurrentIsciIdAsync()
    {
        var u = await _userManager.GetUserAsync(User);
        return u?.IsciId;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Sablonlar = await _sablonService.HamisiniGetirAsync(yalnizAktiv: true);
        ViewBag.Loglar = await _smsService.SonGonderilenler(200);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Gonder(string telefon, string metn, int? sablonId)
    {
        if (string.IsNullOrWhiteSpace(telefon) || string.IsNullOrWhiteSpace(metn))
        {
            TempData["Error"] = "Telefon və mətn boş ola bilməz.";
            return RedirectToAction("Index");
        }

        var isciId = await CurrentIsciIdAsync();
        if (isciId is null)
        {
            TempData["Error"] = "İşçi məlumatı tapılmadı.";
            return RedirectToAction("Index");
        }

        var log = await _smsService.GonderAsync(telefon, metn, sablonId, isciId.Value);
        if (log.Status == Domain.Entities.Pid.PidSmsStatus.Xeta)
            TempData["Error"] = $"SMS göndərilmədi: {log.Xeta}";
        else
            TempData["Success"] = $"SMS göndərildi → {log.Telefon}";

        return RedirectToAction("Index");
    }

    // ── Şablonlar ─────────────────────────────────────────

    public async Task<IActionResult> Sablonlar()
    {
        ViewBag.Sablonlar = await _sablonService.HamisiniGetirAsync(yalnizAktiv: false);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SablonYarat(string ad, string metn, string? aciqlama)
    {
        if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(metn))
        {
            TempData["Error"] = "Ad və mətn məcburidir.";
            return RedirectToAction("Sablonlar");
        }
        var isciId = await CurrentIsciIdAsync() ?? 0;
        await _sablonService.YaratAsync(ad, metn, aciqlama, isciId);
        TempData["Success"] = "Şablon yaradıldı.";
        return RedirectToAction("Sablonlar");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SablonYenile(int id, string ad, string metn, string? aciqlama, bool aktiv)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        var ok = await _sablonService.YenileAsync(id, ad, metn, aciqlama, aktiv, isciId);
        TempData[ok ? "Success" : "Error"] = ok ? "Şablon yeniləndi." : "Şablon tapılmadı.";
        return RedirectToAction("Sablonlar");
    }

    // ── Toplu SMS ──────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> BorcalanlarGetir()
    {
        var ayar = await _sistemAyar.GetirAsync();
        if (ayar?.PidTopluSmsOracleSorguId is null)
            return Json(new { error = "Sistem ayarlarında Oracle sorğusu seçilməyib." });

        var sorquResult = await _sorguService.IdIleGetirAsync(ayar.PidTopluSmsOracleSorguId.Value);
        if (!sorquResult.Success || sorquResult.Data is null)
            return Json(new { error = "Oracle sorğusu tapılmadı." });

        if (!sorquResult.Data.Aktiv)
            return Json(new { error = "Oracle sorğusu deaktividir." });

        try
        {
            var rows = await _oracle.SelectAsync(sorquResult.Data.SorguMetni);
            var result = rows.Select(r => new
            {
                ad      = GetStr(r, "AD"),
                telefon = GetStr(r, "TELEFON")
            }).Where(x => !string.IsNullOrWhiteSpace(x.telefon)).ToList();

            return Json(new { data = result, sorguAdi = sorquResult.Data.SorguAdi });
        }
        catch (Exception ex)
        {
            return Json(new { error = $"Oracle xətası: {ex.Message}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TopluGonder([FromForm] string alicilarJson, [FromForm] string smsMetni)
    {
        if (string.IsNullOrWhiteSpace(alicilarJson) || string.IsNullOrWhiteSpace(smsMetni))
            return Json(new { error = "Alıcı siyahısı və ya SMS mətni boşdur." });

        var isciId = await CurrentIsciIdAsync();
        if (isciId is null)
            return Json(new { error = "İşçi məlumatı tapılmadı." });

        List<(string Ad, string Telefon)>? alicilar;
        try
        {
            var items = JsonSerializer.Deserialize<List<AliciItem>>(alicilarJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            alicilar = items?.Select(x => (x.Ad ?? "", x.Telefon ?? "")).ToList();
        }
        catch
        {
            return Json(new { error = "Alıcı siyahısı formatı yanlışdır." });
        }

        if (alicilar is null || alicilar.Count == 0)
            return Json(new { error = "Göndəriləcək alıcı yoxdur." });

        var (ugur, xeta) = await _smsService.TopluGonderAsync(alicilar, smsMetni, isciId.Value);
        return Json(new { ugur, xeta });
    }

    private static string GetStr(Dictionary<string, object?> row, string key)
    {
        var found = row.FirstOrDefault(kv =>
            string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));
        return found.Value?.ToString()?.Trim() ?? "";
    }

    private sealed class AliciItem
    {
        public string? Ad { get; set; }
        public string? Telefon { get; set; }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SablonSil(int id)
    {
        var isciId = await CurrentIsciIdAsync() ?? 0;
        var ok = await _sablonService.SilAsync(id, isciId);
        TempData[ok ? "Success" : "Error"] = ok ? "Şablon silindi." : "Şablon tapılmadı.";
        return RedirectToAction("Sablonlar");
    }
}
