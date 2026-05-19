using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class SistemAyarController : Controller
{
    private readonly AppDbContext _db;

    public SistemAyarController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var ayar = await _db.SistemAyarlari.FirstOrDefaultAsync()
                   ?? new SistemAyar();
        ViewData["Title"] = "Sistem Ayarları";
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

        await _db.SaveChangesAsync();
        TempData["Ugur"] = "Ayarlar yadda saxlandı.";
        return RedirectToAction(nameof(Index));
    }
}
