using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    public class IsciMaasTarixcesiController : Controller
    {
        // ─────────── INDEX ───────────
        public IActionResult Index(int? isciId)
        {
            ViewBag.IsciId = isciId;
            return View(new List<IsciMaasTarixcesi>());
        }
    }
}
