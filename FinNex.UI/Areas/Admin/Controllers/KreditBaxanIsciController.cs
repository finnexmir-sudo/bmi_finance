using FinNex.Application.Interfaces.Kredit;
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
/// Kredit müraciətlərinə baxa biləcək işçilərin siyahısını idarə edir.
/// Yalnız bu siyahıdakı aktiv işçilər KreditMuraciet bölməsinə girə bilər.
/// </summary>
[Area("Admin")]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.KreditAdmin)]
public class KreditBaxanIsciController : Controller
{
    private readonly IKreditBaxanIsciService _baxanIsciService;
    private readonly IUnitOfWork _uow;
    private readonly UserManager<AppUser> _userManager;

    public KreditBaxanIsciController(
        IKreditBaxanIsciService baxanIsciService,
        IUnitOfWork uow,
        UserManager<AppUser> userManager)
    {
        _baxanIsciService = baxanIsciService;
        _uow = uow;
        _userManager = userManager;
    }

    // GET /Admin/KreditBaxanIsci
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Kredit — Müraciətə baxan işçilər";

        var entities = await _baxanIsciService.HamisiniGetirAsync();

        var teyinler = new List<KreditBaxanIsciListVM>();
        foreach (var e in entities)
        {
            var teyinat = await _uow.Repository<IsciTeyinat>().Query()
                .Where(t => t.IsciId == e.IsciId && t.Aktivdir && !t.Silinib)
                .Include(t => t.Departament)
                .Include(t => t.Vezife)
                .FirstOrDefaultAsync();

            teyinler.Add(new KreditBaxanIsciListVM
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

        return View(teyinler);
    }

    // GET /Admin/KreditBaxanIsci/Secim
    // İşçilər siyahısı — hansılarının artıq təyin edildiyini işarə etmək üçün
    public async Task<IActionResult> Secim(string? axtaris)
    {
        ViewData["Title"] = "İşçi Seç";

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

        var artiqTeyinIdleri = (await _baxanIsciService.HamisiniGetirAsync())
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

    // POST /Admin/KreditBaxanIsci/TeyinEt
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TeyinEt(KreditBaxanIsciTeyinVM vm)
    {
        if (vm.IsciId <= 0)
        {
            TempData["Error"] = "İşçi seçilmədi.";
            return RedirectToAction(nameof(Secim));
        }

        var yaradan = await GetCurrentIsciIdAsync();
        await _baxanIsciService.TeyinEtAsync(vm.IsciId, vm.Qeyd, yaradan);

        TempData["StatusMessage"] = "İşçi kredit müraciətlərinə baxma icazəsi aldı.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/KreditBaxanIsci/LegvEt/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LegvEt(int id)
    {
        var legvEden = await GetCurrentIsciIdAsync();
        await _baxanIsciService.LegvEtAsync(id, legvEden);

        TempData["StatusMessage"] = "İcazə ləğv edildi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> GetCurrentIsciIdAsync()
    {
        var appUser = await _userManager.GetUserAsync(User);
        return appUser?.IsciId;
    }
}
