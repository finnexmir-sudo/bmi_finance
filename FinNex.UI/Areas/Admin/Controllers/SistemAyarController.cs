using FinNex.Application.Interfaces.Sorgular;
using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class SistemAyarController : Controller
{
    private readonly AppDbContext _db;
    private readonly IOracleSorguService _oracleSorguService;

    public SistemAyarController(AppDbContext db, IOracleSorguService oracleSorguService)
    {
        _db = db;
        _oracleSorguService = oracleSorguService;
    }

    public async Task<IActionResult> Index()
    {
        var ayar = await _db.SistemAyarlari.FirstOrDefaultAsync()
                   ?? new SistemAyar();
        ViewData["Title"] = "Sistem Ayarları";
        await YukleSorqular(ayar.PidTopluSmsOracleSorguId, ayar.PidOdenisGunuSorguId);
        return View(ayar);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Saxla(SistemAyar model)
    {
        var ayar = await _db.SistemAyarlari.FirstOrDefaultAsync();
        if (ayar == null)
        {
            ayar = new SistemAyar();
            _db.SistemAyarlari.Add(ayar);
        }

        ayar.KreditImapServer   = model.KreditImapServer?.Trim() ?? "imap.titan.email";
        ayar.KreditImapPort     = model.KreditImapPort > 0 ? model.KreditImapPort : 993;
        ayar.KreditImapEmail    = model.KreditImapEmail?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(model.KreditImapPassword))
            ayar.KreditImapPassword = model.KreditImapPassword.Trim();

        ayar.PidTopluSmsOracleSorguId = model.PidTopluSmsOracleSorguId;
        ayar.PidOdenisGunuSorguId     = model.PidOdenisGunuSorguId;

        await _db.SaveChangesAsync();
        TempData["Ugur"] = "Ayarlar yadda saxlandı.";
        return RedirectToAction(nameof(Index));
    }

    private async Task YukleSorqular(int? borcalanSeciliId = null, int? odenisGunuSeciliId = null)
    {
        var result = await _oracleSorguService.HamisiniGetirAsync();
        var aktiv = (result.Data ?? []).Where(x => x.Aktiv).ToList();
        ViewBag.OracleSorqular         = new SelectList(aktiv, "Id", "SorguAdi", borcalanSeciliId);
        ViewBag.OdenisGunuSorqular     = new SelectList(aktiv, "Id", "SorguAdi", odenisGunuSeciliId);
    }
}
