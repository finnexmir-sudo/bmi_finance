using FinNex.Application.DTOs.HR.Jeton;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
    public class JetonTeyinatiController : Controller
    {
        private readonly IJetonService _jetonService;

        public JetonTeyinatiController(IJetonService jetonService)
        {
            _jetonService = jetonService;
        }

        public async Task<IActionResult> Index()
        {
            var teyinatlar = await _jetonService.JetonTeyinatlariGetirAsync();
            ViewBag.Teyinatlar = teyinatlar;
            ViewData["Title"] = "Jeton Kataloqu";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            var list = await _jetonService.JetonTeyinatlariGetirAsync();
            return Json(new { success = true, data = list });
        }

        [HttpPost]
        public async Task<IActionResult> Yarat([FromBody] JetonTeyinatiCreateDto dto)
        {
            var result = await _jetonService.JetonTeyinatiYaratAsync(dto);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> Yenile([FromBody] JetonTeyinatiUpdateDto dto)
        {
            var result = await _jetonService.JetonTeyinatiYenileAsync(dto);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
