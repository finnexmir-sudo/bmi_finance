using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.HR.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    public class MaasNovuController : Controller
    {
        // ─────────── INDEX ───────────
        public IActionResult Index()
        {
            return View(new List<MaasNovu>());
        }

        // ─────────── CREATE GET ───────────
        [HttpGet]
        public IActionResult Create()
        {
            return View(new MaasNovuCreateVM());
        }

        // ─────────── CREATE POST ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MaasNovuCreateVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            TempData["Success"] = "Maaş növü uğurla yaradıldı.";
            return RedirectToAction(nameof(Index));
        }

        // ─────────── EDIT GET ───────────
        [HttpGet]
        public IActionResult Edit(int id)
        {
            // TODO: load from service
            return View(new MaasNovuCreateVM { Id = id });
        }

        // ─────────── EDIT POST ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(MaasNovuCreateVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            TempData["Success"] = "Maaş növü uğurla yeniləndi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
