using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
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

        // Cihazın son əlaqə vaxtını SDK servisindən və ya ADMSController-dən götürürük
        var sdkElaqa = FinNex.Application.BackgroundJobs.ZkTecoSdkService.SonElaqa;
        var admsElaqa = ADMSController.SonElaqa;
        var sonElaqa = sdkElaqa > admsElaqa ? sdkElaqa : (admsElaqa ?? sdkElaqa);
        var isOnline = FinNex.Application.BackgroundJobs.ZkTecoSdkService.IsOnline
                       || (admsElaqa.HasValue && (DateTime.Now - admsElaqa.Value).TotalSeconds < 60);
        var lastContact = sonElaqa?.ToString("dd.MM.yyyy HH:mm");

        return Json(new { isOnline, lastContact, todayCount, logs });
    }
}