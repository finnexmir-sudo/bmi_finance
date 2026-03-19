using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    //[Authorize(Roles = "Hr")]
    public class HRController : Controller
    {
        public IActionResult Index()
        {

            ViewData["Title"] = "HR Dashboard";
            return View();
        }
    }
}
