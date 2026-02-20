using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    public class MaasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
