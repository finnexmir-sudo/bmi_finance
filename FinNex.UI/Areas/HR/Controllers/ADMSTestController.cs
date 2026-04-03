using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = "HR,Admin")]
public class ADMSTestController : Controller
{
    private readonly AppDbContext _db;

    public ADMSTestController(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult Test()
    {
        return View();
    }

    public async Task<IActionResult> GetRecentLogs()
    {
        var bugun = DateTime.Today;

        var logs = await _db.Davamiyyetler
            .Include(x => x.Isci)
            .OrderByDescending(x => x.Tarix)
            .ThenByDescending(x => x.GirisVaxti)
            .Take(20)
            .Select(x => new
            {
                isciId = x.IsciId,
                isciAd = x.Isci.Ad + " " + x.Isci.Soyad,
                tarix = x.Tarix.ToString("dd.MM.yyyy"),
                girisVaxti = x.GirisVaxti.HasValue
                                ? x.GirisVaxti.Value.ToString("HH:mm")
                                : (string?)null,
                cixisVaxti = x.CixisVaxti.HasValue
                                ? x.CixisVaxti.Value.ToString("HH:mm")
                                : (string?)null,
                status = (int)x.Status
            })
            .ToListAsync();

        var todayCount = await _db.Davamiyyetler
            .CountAsync(x => x.Tarix == bugun);

        // Cihazın son əlaqə vaxtını ADMS controller-dən götürürük
        var admsElaqa = ADMSController.SonElaqa;
        var isOnline = admsElaqa.HasValue && (DateTime.Now - admsElaqa.Value).TotalSeconds < 120;
        var lastContact = admsElaqa?.ToString("dd.MM.yyyy HH:mm");

        return Json(new { isOnline, lastContact, todayCount, logs });
    }
}