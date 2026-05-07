using FinNex.Application.DTOs.HR.Motivasya;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class JetonTeklifiController : Controller
    {
        private readonly IJetonTeklifleriService _teklifService;
        private readonly IUnitOfWork _uow;

        public JetonTeklifiController(
            IJetonTeklifleriService teklifService,
            IUnitOfWork uow)
        {
            _teklifService = teklifService;
            _uow = uow;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            var list = await _teklifService.GetGozleyenlerAsync();
            return Json(new { success = true, data = list });
        }

        [HttpGet]
        public async Task<IActionResult> GetJetonTeyinatlari()
        {
            var list = await _uow.Repository<JetonTeyinati>()
                .Query()
                .Where(x => x.Aktivdir)
                .OrderBy(x => x.Nov)
                .ThenBy(x => x.Ad)
                .Select(x => new { x.Id, x.Ad, x.Nov, x.SaatDeyeri, x.Ikon, x.RengKodu })
                .ToListAsync();
            return Json(new { success = true, data = list });
        }

        [HttpPost]
        public async Task<IActionResult> JetonVer([FromBody] JetonTeklifiVerDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var result = await _teklifService.JetonVerAsync(dto, userId);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> Reddet([FromBody] int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var result = await _teklifService.ReddetAsync(id, userId);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
