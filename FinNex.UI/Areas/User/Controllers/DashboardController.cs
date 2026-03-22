using FinNex.Application.Interfaces;
using FinNex.UI.Areas.User.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Operator")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account", new { area = "" });

            var result = await _dashboardService.GetDashboardAsync(username);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(new UserDashboardViewModel());
            }

            var viewModel = UserDashboardViewModel.FromDto(result.Data!);
            ViewData["UserRole"] = viewModel.VezifeAdi;

            return View(viewModel);
        }
    }
}