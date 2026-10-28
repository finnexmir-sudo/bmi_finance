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
/// Kassa günlük kurs beyannaməsi TƏSDİQLƏNDİKDƏ bildiriş alacaq işçilərin
/// sabit siyahısını idarə edir. `KassaTesdiqEdiciController`-in eyni naxışı
/// (28.10.2026, istifadəçi tələbi: "kurslari rehber tesdiqledikden sonra
/// secdiyim iscilere bildiris gelmeyini isteyirem").
/// </summary>
[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class KassaBildirisAlaniController : Controller
{
    private readonly IKassaBildirisAlaniService _bildirisAlaniService;
    private readonly IUnitOfWork _uow;
    private readonly UserManager<AppUser> _userManager;

    public KassaBildirisAlaniController(
        IKassaBildirisAlaniService bildirisAlaniService,
        IUnitOfWork uow,
        UserManager<AppUser> userManager)
    {
        _bildirisAlaniService = bildirisAlaniService;
        _uow = uow;
        _userManager = userManager;
    }

    // GET /Admin/KassaBildirisAlani
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Kassa — Bildiriş alanları";

        var entities = await _bildirisAlaniService.HamisiniGetirAsync();

        var rows = new List<KassaBildirisAlaniListVM>();
        foreach (var e in entities)
        {
            var teyinat = await _uow.Repository<IsciTeyinat>().Query()
                .Where(t => t.IsciId == e.IsciId && t.Aktivdir && !t.Silinib)
                .Include(t => t.Departament)
                .Include(t => t.Vezife)
                .FirstOrDefaultAsync();

            rows.Add(new KassaBildirisAlaniListVM
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

    // GET /Admin/KassaBildirisAlani/Secim
    public async Task<IActionResult> Secim(string? axtaris)
    {
        ViewData["Title"] = "Bildiriş Alanı Seç";

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

        var artiqTeyinIdleri = (await _bildirisAlaniService.HamisiniGetirAsync())
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

    // POST /Admin/KassaBildirisAlani/TeyinEt
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TeyinEt(KassaBildirisAlaniTeyinVM vm)
    {
        if (vm.IsciId <= 0)
        {
            TempData["Error"] = "İşçi seçilmədi.";
            return RedirectToAction(nameof(Secim));
        }

        var yaradan = await GetCurrentIsciIdAsync();
        await _bildirisAlaniService.TeyinEtAsync(vm.IsciId, vm.Qeyd, yaradan);

        TempData["StatusMessage"] = "İşçi kassa kursu bildirişlərini alacaq siyahıya əlavə edildi.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/KassaBildirisAlani/LegvEt/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LegvEt(int id)
    {
        var legvEden = await GetCurrentIsciIdAsync();
        await _bildirisAlaniService.LegvEtAsync(id, legvEden);

        TempData["StatusMessage"] = "İşçi siyahıdan çıxarıldı.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> GetCurrentIsciIdAsync()
    {
        var appUser = await _userManager.GetUserAsync(User);
        return appUser?.IsciId;
    }
}
