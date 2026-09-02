using FinNex.Application.Interfaces.Kredit;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Admin.Controllers;

/// <summary>
/// Komitəsiz rədd səbəblərinin açar siyahısı (02.09.2026).
///
/// NİYƏ VAR: rədd səbəbi sərbəst mətn deyil, siyahıdan seçimdir — belə olanda
/// «bu ay 40 müraciətdən 12-si MKR-ə görə rədd olunub» tipli hesabat çıxarmaq
/// mümkündür. Siyahı isə zamanla artır, ona görə enum yox, cədvəldir və buradan
/// idarə olunur (build lazım deyil).
///
/// SƏBƏB SİLİNMİR — deaktiv edilir. Keçmiş müraciətlər bu sətrə istinad edir;
/// silinsə tarixçə «səbəbsiz» qalar. Deaktiv səbəb yeni rəddlərdə seçim
/// siyahısında görünmür, köhnə qeydlərdə isə görünməyə davam edir.
/// </summary>
[Area("Admin")]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.KreditAdmin)]
public class KreditReddSebebiController : Controller
{
    private readonly IKreditReddSebebiService _service;
    private readonly UserManager<AppUser> _userManager;

    public KreditReddSebebiController(IKreditReddSebebiService service, UserManager<AppUser> userManager)
    {
        _service = service;
        _userManager = userManager;
    }

    private async Task<int?> CariIsciIdAsync()
        => (await _userManager.GetUserAsync(HttpContext.User))?.IsciId;

    // GET /Admin/KreditReddSebebi
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Kredit — Rədd səbəbləri";
        return View(await _service.HamisiniGetirAsync());
    }

    // POST /Admin/KreditReddSebebi/Yarat
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yarat(string ad, int sira)
    {
        try
        {
            await _service.YaratAsync(ad, sira, await CariIsciIdAsync());
            TempData["Success"] = "Səbəb əlavə edildi.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/KreditReddSebebi/Yenile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yenile(int id, string ad, int sira)
    {
        try
        {
            await _service.YenileAsync(id, ad, sira, await CariIsciIdAsync());
            TempData["Success"] = "Səbəb yeniləndi.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/KreditReddSebebi/AktivliyiDeyis
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AktivliyiDeyis(int id, bool aktivdir)
    {
        try
        {
            await _service.AktivliyiDeyisAsync(id, aktivdir, await CariIsciIdAsync());
            TempData["Success"] = aktivdir ? "Səbəb aktivləşdirildi." : "Səbəb deaktiv edildi.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
