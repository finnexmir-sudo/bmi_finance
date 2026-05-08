using FinNex.Application.DTOs.HR.Motivasya;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<AppUser> _userManager;

        public JetonTeklifiController(
            IJetonTeklifleriService teklifService,
            IUnitOfWork uow,
            UserManager<AppUser> userManager)
        {
            _teklifService = teklifService;
            _uow = uow;
            _userManager = userManager;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            int? departamentId = await GetRehberDepartamentIdAsync();
            var list = await _teklifService.GetGozleyenlerAsync(departamentId);
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

            if (!await HasAuthorityOverTeklifAsync(dto.TeklifId))
                return Json(new { success = false, message = "Bu tövsiyə üzərində səlahiyyətiniz yoxdur." });

            var result = await _teklifService.JetonVerAsync(dto, userId);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> Reddet([FromBody] int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

            if (!await HasAuthorityOverTeklifAsync(id))
                return Json(new { success = false, message = "Bu tövsiyə üzərində səlahiyyətiniz yoxdur." });

            var result = await _teklifService.ReddetAsync(id, userId);
            return Json(new { success = result.Success, message = result.Message });
        }

        // Returns the caller's departamentId when they are a Rehber (not HR/Admin).
        // Returns null for HR/Admin so they see all recommendations.
        private async Task<int?> GetRehberDepartamentIdAsync()
        {
            if (User.IsInRole(RoleNames.HR) || User.IsInRole(RoleNames.Admin))
                return null;

            var appUser = await _userManager.GetUserAsync(User);
            if (appUser?.IsciId == null) return null;

            var teyinat = await _uow.Repository<IsciTeyinati>()
                .Query()
                .Where(t => t.IsciId == appUser.IsciId.Value && t.Aktivdir)
                .FirstOrDefaultAsync();

            return teyinat?.DepartamentId;
        }

        // Returns false when the caller is a Rehber and the teklif belongs to a
        // different department.  HR and Admin always pass.
        private async Task<bool> HasAuthorityOverTeklifAsync(int teklifId)
        {
            if (User.IsInRole(RoleNames.HR) || User.IsInRole(RoleNames.Admin))
                return true;

            var callerDeptId = await GetRehberDepartamentIdAsync();
            if (callerDeptId == null) return false;

            var teklif = await _uow.Repository<JetonTeklifi>()
                .Query()
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.Aktivdir))
                .FirstOrDefaultAsync(x => x.Id == teklifId);

            if (teklif == null) return false;

            return teklif.Isci.IsciTeyinatlari
                .Any(t => t.Aktivdir && t.DepartamentId == callerDeptId.Value);
        }
    }
}
