using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    public class HRController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "HR Dashboard";
            return View();
        }
    }
}
