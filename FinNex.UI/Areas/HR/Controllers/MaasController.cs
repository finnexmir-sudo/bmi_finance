using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.HR.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    public class MaasController : Controller
    {
        // ─────────── INDEX ───────────
        public IActionResult Index(int? isciId, int? il, int? ay, MaasStatus? status)
        {
            ViewBag.IsciId = isciId;
            ViewBag.Il = il ?? DateTime.Today.Year;
            ViewBag.Ay = ay ?? DateTime.Today.Month;
            ViewBag.Status = status;
            return View(new List<Maas>());
        }

        // ─────────── CREATE GET ───────────
        [HttpGet]
        public IActionResult Create(int? isciId)
        {
            var vm = new MaasCreateVM
            {
                Il = DateTime.Today.Year,
                Ay = DateTime.Today.Month,
                IsciId = isciId ?? 0
            };
            return View(vm);
        }

        // ─────────── CREATE POST ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MaasCreateVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            TempData["Success"] = "Maaş uğurla yaradıldı.";
            return RedirectToAction(nameof(Index));
        }

        // ─────────── DETAIL ───────────
        [HttpGet]
        public IActionResult Detail(int id)
        {
            // TODO: load from service
            var vm = new MaasDetailVM { Id = id };
            return View(vm);
        }
    }
}
