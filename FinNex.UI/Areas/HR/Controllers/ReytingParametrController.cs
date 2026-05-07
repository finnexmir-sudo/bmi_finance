using FinNex.Application.DTOs.HR.Reyting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
    public class ReytingParametrController : Controller
    {
        private readonly IReytingService _reytingService;

        public ReytingParametrController(IReytingService reytingService)
        {
            _reytingService = reytingService;
        }

        // GET /HR/ReytingParametr
        public IActionResult Index() => View();

        // ── Kateqoriyalar ────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetKateqoriyalar()
        {
            var list = await _reytingService.KateqoriyalariGetirAsync();
            return Json(new { success = true, data = list });
        }

        [HttpPost]
        public async Task<IActionResult> KateqoriyaSaxla([FromBody] ReytingKateqoriyasiSaveDto dto)
        {
            var result = await _reytingService.KateqoriyaSaxlaAsync(dto);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> KateqoriyaSil([FromBody] int id)
        {
            var result = await _reytingService.KateqoriyaSilAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── Hadisə Parametrləri ──────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetParametrlər()
        {
            var list = await _reytingService.ParametrləriGetirAsync();
            return Json(new { success = true, data = list });
        }

        [HttpPost]
        public async Task<IActionResult> ParametrSaxla([FromBody] ReytingParametriSaveDto dto)
        {
            var result = await _reytingService.ParametrSaxlaAsync(dto);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
