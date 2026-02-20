using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    public class DavamiyyetController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
