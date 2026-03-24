using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.HR.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    public class MaasParametriController : Controller
    {
        // ─────────── INDEX ───────────
        public IActionResult Index()
        {
            return View(new List<MaasParametri>());
        }

        // ─────────── CREATE GET ───────────
        [HttpGet]
        public IActionResult Create()
        {
            return View(new MaasParametriCreateVM
            {
                BaslamaTarixi = DateTime.Today
            });
        }

        // ─────────── CREATE POST ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MaasParametriCreateVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            TempData["Success"] = "Maaş parametri uğurla yaradıldı.";
            return RedirectToAction(nameof(Index));
        }

        // ─────────── EDIT GET ───────────
        [HttpGet]
        public IActionResult Edit(int id)
        {
            // TODO: load from service
            return View(new MaasParametriCreateVM { Id = id, BaslamaTarixi = DateTime.Today });
        }

        // ─────────── EDIT POST ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(MaasParametriCreateVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            TempData["Success"] = "Maaş parametri uğurla yeniləndi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
