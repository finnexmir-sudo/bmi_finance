using FinNex.Application.Services.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.HR}")]
    public class EmrController : Controller
    {
        private readonly IEmrService _emrService;

        public EmrController(IEmrService emrService)
        {
            _emrService = emrService;
        }

        // Əmr sayğacları — cari vəziyyət + son nömrəni əl ilə təyin etmə
        public async Task<IActionResult> Index()
        {
            var saygaclar = await _emrService.SayghaclarAsync();
            return View(saygaclar);
        }

        // HR son nömrəni təyin edir — növbəti generasiya (SonNomre+1) oradan davam edir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SonNomreTeyinEt(int nov, int il, int sonNomre)
        {
            if (il < 2000 || il > 2100)
            {
                TempData["Error"] = "İl düzgün deyil (2000–2100).";
                return RedirectToAction(nameof(Index));
            }
            if (sonNomre < 0) sonNomre = 0;

            await _emrService.SonNomreTeyinEtAsync((EmrNovu)nov, il, sonNomre);
            TempData["Success"] = $"Sayğac yeniləndi — növbəti əmr nömrəsi {sonNomre + 1}-dən başlayacaq.";
            return RedirectToAction(nameof(Index));
        }
    }
}
