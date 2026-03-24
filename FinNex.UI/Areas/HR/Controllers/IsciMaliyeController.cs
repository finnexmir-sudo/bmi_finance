using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.HR.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    public class IsciMaliyeController : Controller
    {
        // ─────────── INDEX ───────────
        public IActionResult Index()
        {
            return View(new List<IsciMaliye>());
        }

        // ─────────── CREATE GET ───────────
        [HttpGet]
        public IActionResult Create(int? isciId)
        {
            return View(new IsciMaliyeCreateVM { IsciId = isciId ?? 0 });
        }

        // ─────────── CREATE POST ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(IsciMaliyeCreateVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            TempData["Success"] = "İşçi maliyyə məlumatı uğurla yaradıldı.";
            return RedirectToAction(nameof(Index));
        }

        // ─────────── EDIT GET ───────────
        [HttpGet]
        public IActionResult Edit(int id)
        {
            // TODO: load from service
            return View(new IsciMaliyeCreateVM { Id = id });
        }

        // ─────────── EDIT POST ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(IsciMaliyeCreateVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            TempData["Success"] = "İşçi maliyyə məlumatı uğurla yeniləndi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
