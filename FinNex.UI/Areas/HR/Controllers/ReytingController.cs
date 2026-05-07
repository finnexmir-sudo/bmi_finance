using FinNex.Application.DTOs.HR.Reyting;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class ReytingController : Controller
    {
        private readonly IReytingService _reytingService;

        public ReytingController(IReytingService reytingService)
        {
            _reytingService = reytingService;
        }

        // GET /HR/Reyting
        public IActionResult Index() => View();

        // GET /HR/Reyting/GetData?il=2026&ay=5
        [HttpGet]
        public async Task<IActionResult> GetData(int il, int ay)
        {
            if (il == 0) il = DateTime.Now.Year;
            if (ay == 0) ay = DateTime.Now.Month;
            var list = await _reytingService.IsciReytinqleriGetirAsync(il, ay);
            return Json(new { success = true, data = list });
        }

        // GET /HR/Reyting/GetIsci?isciId=1&il=2026&ay=5
        [HttpGet]
        public async Task<IActionResult> GetIsci(int isciId, int il, int ay)
        {
            var dto = await _reytingService.IsciReytinqiGetirAsync(isciId, il, ay);
            return Json(new { success = dto != null, data = dto });
        }

        // POST /HR/Reyting/Hesabla
        [HttpPost]
        public async Task<IActionResult> Hesabla([FromBody] HesablaRequestDto dto)
        {
            Result result;
            if (dto.IsciId.HasValue)
                result = await _reytingService.ReytinqHesablaAsync(dto.IsciId.Value, dto.Il, dto.Ay);
            else
                result = await _reytingService.TopluHesablaAsync(dto.Il, dto.Ay);

            return Json(new { success = result.Success, message = result.Message });
        }

        // POST /HR/Reyting/ManualElave
        [HttpPost]
        public async Task<IActionResult> ManualElave([FromBody] ManualReytingElavEtDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var result = await _reytingService.ManualXalElavEtAsync(dto, userId);
            return Json(new { success = result.Success, message = result.Message });
        }
    }

    public class HesablaRequestDto
    {
        public int? IsciId { get; set; }
        public int Il { get; set; }
        public int Ay { get; set; }
    }
}
