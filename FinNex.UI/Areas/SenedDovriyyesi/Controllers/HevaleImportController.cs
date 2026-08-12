using FinNex.Application.Interfaces.Hevale;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.SenedDovriyyesi.Controllers;

/// <summary>
/// BMI (Oracle) həvalə jurnallarının FinNex-ə birdəfəlik köçürülməsi.
/// Yalnız Admin — bu, tarixi datanın köçürülməsidir, gündəlik əməliyyat deyil.
/// </summary>
[Area("SenedDovriyyesi")]
[Authorize(Roles = RoleNames.Admin)]
public class HevaleImportController : Controller
{
    private readonly IHevaleImportService _service;

    public HevaleImportController(IHevaleImportService service)
    {
        _service = service;
    }

    // GET: /SenedDovriyyesi/HevaleImport
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var res = await _service.VeziyyetAsync(ct);
        if (!res.Success)
        {
            TempData["Error"] = res.Message;
            return View(new Application.DTOs.Hevale.HevaleImportVeziyyetDto());
        }
        return View(res.Data ?? new Application.DTOs.Hevale.HevaleImportVeziyyetDto());
    }

    // POST: /SenedDovriyyesi/HevaleImport/Il
    // Bir ili idxal edir. İdempotentdir — təkrar çağırmaq təhlükəsizdir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Il(string jurnal, int? il, CancellationToken ct)
    {
        var res = await _service.IlIdxalAsync(jurnal, il, ct);
        TempData[res.Success ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }
}
