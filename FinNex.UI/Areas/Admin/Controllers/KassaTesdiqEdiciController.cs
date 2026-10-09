using FinNex.Application.Interfaces.Kassa;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

/// <summary>
/// Kassa valyuta kursunu təsdiqləyə/imtina edə biləcək işçilərin siyahısını
/// idarə edir. Yalnız bu siyahıdakı aktiv işçilər Kassa → Exchange təsdiq
/// ekranına girə bilər (09.10.2026, istifadəçi tələbi: "təsdiqləyəcək
/// şəxsləri mən admin paneldən idarə edim").
/// </summary>
[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class KassaTesdiqEdiciController : Controller
{
    private readonly IKassaTesdiqEdiciService _tesdiqEdiciService;
    private readonly IUnitOfWork _uow;
    private readonly UserManager<AppUser> _userManager;

    public KassaTesdiqEdiciController(
        IKassaTesdiqEdiciService tesdiqEdiciService,
        IUnitOfWork uow,
        UserManager<AppUser> userManager)
    {
        _tesdiqEdiciService = tesdiqEdiciService;
        _uow = uow;
        _userManager = userManager;
    }

    // GET /Admin/KassaTesdiqEdici
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Kassa — Təsdiqedicilər";

        var entities = await _tesdiqEdiciService.HamisiniGetirAsync();

        var rows = new List<KassaTesdiqEdiciListVM>();
        foreach (var e in entities)
        {
            var teyinat = await _uow.Repository<IsciTeyinat>().Query()
                .Where(t => t.IsciId == e.IsciId && t.Aktivdir && !t.Silinib)
                .Include(t => t.Departament)
                .Include(t => t.Vezife)
                .FirstOrDefaultAsync();

            rows.Add(new KassaTesdiqEdiciListVM
            {
                Id = e.Id,
                IsciId = e.IsciId,
                TamAd = e.Isci.TamAd,
                FIN = e.Isci.FIN,
                SobeAdi = teyinat?.Departament?.Ad,
                VezifeAdi = teyinat?.Vezife?.Ad,
                AktivdirFrom = e.AktivdirFrom,
                AktivdirTo = e.AktivdirTo,
                Qeyd = e.Qeyd
            });
        }

        return View(rows);
    }

    // GET /Admin/KassaTesdiqEdici/Secim
    public async Task<IActionResult> Secim(string? axtaris)
    {
        ViewData["Title"] = "Təsdiqedici Seç";

        var iscilerQ = _uow.Repository<Isci>().Query()
            .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv);

        if (!string.IsNullOrWhiteSpace(axtaris))
        {
            var a = axtaris.Trim();
            iscilerQ = iscilerQ.Where(x =>
                x.Ad.Contains(a) || x.Soyad.Contains(a) ||
                (x.AtaAdi != null && x.AtaAdi.Contains(a)) || x.FIN.Contains(a));
        }

        var isciler = await iscilerQ
            .OrderBy(x => x.Sira).ThenBy(x => x.Ad).ThenBy(x => x.Soyad)
            .Take(500)
            .ToListAsync();

        var artiqTeyinIdleri = (await _tesdiqEdiciService.HamisiniGetirAsync())
            .Where(e => e.AktivdirTo == null || e.AktivdirTo >= DateTime.Now)
            .Select(e => e.IsciId)
            .ToHashSet();

        var rows = new List<IsciSecimVM>();
        foreach (var i in isciler)
        {
            var teyinat = await _uow.Repository<IsciTeyinat>().Query()
                .Where(t => t.IsciId == i.Id && t.Aktivdir && !t.Silinib)
                .Include(t => t.Departament)
                .Include(t => t.Vezife)
                .FirstOrDefaultAsync();

            rows.Add(new IsciSecimVM
            {
                IsciId = i.Id,
                TamAd = i.TamAd,
                FIN = i.FIN,
                SobeAdi = teyinat?.Departament?.Ad,
                VezifeAdi = teyinat?.Vezife?.Ad,
                ArtiqTeyindir = artiqTeyinIdleri.Contains(i.Id)
            });
        }

        ViewBag.Axtaris = axtaris;
        return View(rows);
    }

    // POST /Admin/KassaTesdiqEdici/TeyinEt
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TeyinEt(KassaTesdiqEdiciTeyinVM vm)
    {
        if (vm.IsciId <= 0)
        {
            TempData["Error"] = "İşçi seçilmədi.";
            return RedirectToAction(nameof(Secim));
        }

        var yaradan = await GetCurrentIsciIdAsync();
        await _tesdiqEdiciService.TeyinEtAsync(vm.IsciId, vm.Qeyd, yaradan);

        TempData["StatusMessage"] = "İşçi kassa kurslarını təsdiqləmə icazəsi aldı.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/KassaTesdiqEdici/LegvEt/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LegvEt(int id)
    {
        var legvEden = await GetCurrentIsciIdAsync();
        await _tesdiqEdiciService.LegvEtAsync(id, legvEden);

        TempData["StatusMessage"] = "İcazə ləğv edildi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> GetCurrentIsciIdAsync()
    {
        var appUser = await _userManager.GetUserAsync(User);
        return appUser?.IsciId;
    }
}
