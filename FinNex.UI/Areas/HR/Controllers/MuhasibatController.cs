using FinNex.Application.Services.HR;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.HR},{RoleNames.Muhasib}")]
    public class MuhasibatController : Controller
    {
        private readonly IMuhasibatHesabService _hesabService;

        public MuhasibatController(IMuhasibatHesabService hesabService)
        {
            _hesabService = hesabService;
        }

        // Mühasibat (proводka) hesabları — açar→hesab. Avans Debet hesabı və s.
        public async Task<IActionResult> Hesablar()
        {
            var list = await _hesabService.HamisiAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HesabSaxla(string acar, string ad, string hesabNomresi, bool aktiv = true)
        {
            if (string.IsNullOrWhiteSpace(acar) || string.IsNullOrWhiteSpace(hesabNomresi))
            {
                TempData["Error"] = "Açar və hesab nömrəsi boş ola bilməz.";
                return RedirectToAction(nameof(Hesablar));
            }

            await _hesabService.UpsertAsync(acar.Trim(), ad?.Trim() ?? acar, hesabNomresi.Trim(), aktiv);
            TempData["Success"] = $"Hesab yadda saxlanıldı: {acar.Trim()}";
            return RedirectToAction(nameof(Hesablar));
        }
    }
}
