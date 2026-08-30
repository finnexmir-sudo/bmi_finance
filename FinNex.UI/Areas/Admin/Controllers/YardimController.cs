using System.Security.Claims;
using FinNex.Application.DTOs.Yardim;
using FinNex.Application.Helpers.Yardim;
using FinNex.Application.Interfaces.Yardim;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace FinNex.UI.Areas.Admin.Controllers;

/// <summary>
/// Səhifə təlimatlarının redaktoru — YALNIZ ADMIN (istifadəçi qərarı 27.08.2026:
/// «mətni mən təsdiqləyərəm, Admin»).
///
/// Mətn bazadadır, ona görə düzəliş üçün build/deploy LAZIM DEYİL — söz
/// dəyişikliyi release gözləsə, sənəd bir aya köhnələr.
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class YardimController : Controller
{
    private readonly ISehifeYardimiService _service;
    private readonly IActionDescriptorCollectionProvider _actions;

    public YardimController(
        ISehifeYardimiService service,
        IActionDescriptorCollectionProvider actions)
    {
        _service = service;
        _actions = actions;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Axtaris = q;
        return View(await _service.SiyahiAsync(q, adminmi: true));
    }

    [HttpGet]
    public async Task<IActionResult> Redakte(int id = 0, string? acar = null)
    {
        // id=0 → yeni qeyd. Açar «?» panelindəki «indi yaz» düyməsindən gəlir,
        // yəni admin səhifədən birbaşa redaktəyə düşür və açarı əl ilə yazmır.
        var dto = id > 0
            ? await _service.RedakteMelumatiAsync(id)
            : await _service.YeniMelumatAsync(acar ?? "");

        if (dto == null)
        {
            TempData["Error"] = "Təlimat tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Redakte(YardimUpsertDto dto)
    {
        var res = await _service.YaddaSaxlaAsync(dto, GetUserId());
        TempData[res.Success ? "Success" : "Error"] = res.Message;

        // Xətada forma İTMƏSİN — uzun təlimat mətni yenidən yazılmasın.
        if (!res.Success) return View(dto);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var res = await _service.SilAsync(id, GetUserId());
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// ƏHATƏ — sistemdəki hansı səhifənin təlimatı var, hansının yox.
    ///
    /// Səhifə siyahısı ƏL İLƏ SAXLANMIR: marşrut cədvəlindən (ActionDescriptor)
    /// oxunur. Yeni səhifə əlavə olunanda burada avtomatik görünür — əks halda
    /// siyahı ilk gündən köhnəlməyə başlayardı.
    /// </summary>
    public async Task<IActionResult> Ehate(string? sahe)
    {
        var acarlar = SehifeAcarlari(sahe);
        var netice  = await _service.EhateAsync(acarlar);

        ViewBag.Saheler = SehifeAcarlari(null)
            .Select(a => a.Split('/')[0]).Distinct().OrderBy(x => x).ToList();
        ViewBag.Sahe    = sahe;
        ViewBag.Yazilan = netice.Count(x => x.Yazilib);
        ViewBag.Cemi    = netice.Count;
        return View(netice);
    }

    /// <summary>
    /// Yardım verilə bilən səhifələrin açarları.
    ///
    /// Süzgəc: yalnız GET və yalnız View qaytaran əməllər sayılır — POST,
    /// JSON endpoint və fayl ixracı «səhifə» deyil, onlara təlimat yazılmır.
    /// Tam dəqiq süzgəc mümkün deyil (dönüş tipi runtime-da bilinir), ona görə
    /// ad üzrə açıq-aşkar qeyri-səhifələr də kənarlaşdırılır.
    /// </summary>
    private List<string> SehifeAcarlari(string? sahe)
    {
        // Səhifə OLMAYAN, amma GET olan tipik əməllər — siyahını şişirtməsinlər.
        var kenar = new[]
        {
            "sil", "delete", "excel", "ixrac", "export", "word", "pdf", "yukle",
            "download", "json", "api", "panel", "preview", "onizleme", "ping"
        };

        var siyahi = new List<string>();

        foreach (var ad in _actions.ActionDescriptors.Items)
        {
            if (ad is not ControllerActionDescriptor c) continue;

            // Yalnız GET
            var metodlar = c.ActionConstraints?
                .OfType<Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint>()
                .SelectMany(x => x.HttpMethods).ToList();
            if (metodlar != null && metodlar.Count > 0 && !metodlar.Contains("GET")) continue;

            var area = c.RouteValues.TryGetValue("area", out var a) ? a : null;
            var emel = c.ActionName ?? "";
            if (kenar.Any(k => emel.ToLowerInvariant().Contains(k))) continue;

            var acar = YardimAcar.Qur(area, c.ControllerName, emel);
            if (sahe != null && !acar.StartsWith(sahe.ToLowerInvariant() + "/")) continue;

            siyahi.Add(acar);
        }

        return siyahi.Distinct().OrderBy(x => x).ToList();
    }
}
