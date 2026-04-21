using FinNex.Application.Interfaces.Kredit;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Kredit;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

/// <summary>
/// Kredit komitəsi üzvlərini idarə edir. Üzv sayı dinamikdir (5-7 arası),
/// rol (Sədr / Sədr müavini / Üzv / Katib) təyin edilir. Komitə qərar
/// imzalarkən yalnız aktiv üzvlər görünür.
/// </summary>
[Area("Admin")]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.KreditAdmin)]
public class KomiteUzvuController : Controller
{
    private readonly IKomiteUzvuService _komiteService;
    private readonly IUnitOfWork _uow;
    private readonly UserManager<AppUser> _userManager;

    public KomiteUzvuController(
        IKomiteUzvuService komiteService,
        IUnitOfWork uow,
        UserManager<AppUser> userManager)
    {
        _komiteService = komiteService;
        _uow = uow;
        _userManager = userManager;
    }

    // GET /Admin/KomiteUzvu?yalnizAktiv=true
    public async Task<IActionResult> Index(bool yalnizAktiv = true)
    {
        ViewData["Title"] = "Kredit Komitəsi üzvləri";

        var entities = await _komiteService.HamisiniGetirAsync(yalnizAktiv);

        var rows = new List<KomiteUzvuListVM>();
        foreach (var e in entities)
        {
            var teyinat = await _uow.Repository<IsciTeyinat>().Query()
                .Where(t => t.IsciId == e.IsciId && t.Aktivdir && !t.Silinib)
                .Include(t => t.Departament)
                .Include(t => t.Vezife)
                .FirstOrDefaultAsync();

            rows.Add(new KomiteUzvuListVM
            {
                Id = e.Id,
                IsciId = e.IsciId,
                TamAd = e.Isci.TamAd,
                FIN = e.Isci.FIN,
                SobeAdi = teyinat?.Departament?.Ad,
                VezifeAdi = teyinat?.Vezife?.Ad,
                Rol = e.Rol,
                AktivdirFrom = e.AktivdirFrom,
                AktivdirTo = e.AktivdirTo,
                Qeyd = e.Qeyd
            });
        }

        ViewBag.YalnizAktiv = yalnizAktiv;
        return View(rows);
    }

    // GET /Admin/KomiteUzvu/Secim
    public async Task<IActionResult> Secim(string? axtaris)
    {
        ViewData["Title"] = "Komitə Üzvü Seç";

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
            .OrderBy(x => x.Soyad).ThenBy(x => x.Ad)
            .Take(500)
            .ToListAsync();

        var artiqUzvIdleri = (await _komiteService.HamisiniGetirAsync(yalnizAktiv: true))
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
                ArtiqTeyindir = artiqUzvIdleri.Contains(i.Id)
            });
        }

        ViewBag.Axtaris = axtaris;
        return View(rows);
    }

    // POST /Admin/KomiteUzvu/TeyinEt
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TeyinEt(KomiteUzvuTeyinVM vm)
    {
        if (vm.IsciId <= 0)
        {
            TempData["Error"] = "İşçi seçilmədi.";
            return RedirectToAction(nameof(Secim));
        }

        var yaradan = await GetCurrentIsciIdAsync();
        await _komiteService.TeyinEtAsync(vm.IsciId, vm.Rol, vm.Qeyd, yaradan);

        TempData["StatusMessage"] = "Komitə üzvü əlavə edildi.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/KomiteUzvu/RolDeyis
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RolDeyis(KomiteUzvuRolDeyisVM vm)
    {
        var yenileyen = await GetCurrentIsciIdAsync();
        await _komiteService.RolDeyisAsync(vm.KomiteUzvuId, vm.YeniRol, yenileyen);

        TempData["StatusMessage"] = "Rol yeniləndi.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/KomiteUzvu/LegvEt/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LegvEt(int id)
    {
        var legvEden = await GetCurrentIsciIdAsync();
        await _komiteService.LegvEtAsync(id, legvEden);

        TempData["StatusMessage"] = "Üzvlük ləğv edildi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> GetCurrentIsciIdAsync()
    {
        var appUser = await _userManager.GetUserAsync(User);
        return appUser?.IsciId;
    }
}
