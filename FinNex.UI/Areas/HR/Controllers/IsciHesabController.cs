using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize]
    public class IsciHesabController : Controller
    {
        private readonly IIsciService _service;

        public IsciHesabController(IIsciService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _service.GetIsciHesabSiyahisiAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Saxla(int isciId, string? hesab)
        {
            var result = await _service.HesabYenileAsync(isciId, hesab);
            return Json(new { ok = result.Success, error = result.Message });
        }
    }
}
