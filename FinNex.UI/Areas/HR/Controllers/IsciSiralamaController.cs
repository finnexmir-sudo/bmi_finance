using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// İşçilərin göstərmə sırasını drag-and-drop ilə təyin edən səhifə.
    /// Saxlananadək hər siyahıda (Maaş, Bonus/Overtime, TopluHesabla və s.)
    /// bu sıraya görə düzülür.
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib + "," + RoleNames.Rehber)]
    public class IsciSiralamaController : Controller
    {
        private readonly IIsciSiralamaService _service;

        public IsciSiralamaController(IIsciSiralamaService service)
        {
            _service = service;
        }

        // ── GET /HR/IsciSiralama ─────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var siyahi = await _service.GetSiyahiAsync();
            ViewData["Title"] = "İşçi Sıralaması";
            return View(siyahi);
        }

        // ── POST /HR/IsciSiralama/Saxla ──────────────────────────────────────
        // Body: { "isciIdler": [3, 5, 1, 8, ...] }  — yeni sıralama
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Saxla([FromBody] SaxlaSorgu sorgu)
        {
            if (sorgu?.IsciIdler == null || sorgu.IsciIdler.Count == 0)
                return Json(new { ok = false, mesaj = "Sıra boşdur." });

            var r = await _service.SaxlaAsync(sorgu.IsciIdler);
            return Json(new
            {
                ok    = r.Success,
                say   = r.Data,
                mesaj = r.Success ? $"Sıralama yadda saxlanıldı — {r.Data} dəyişiklik." : r.Message
            });
        }

        public class SaxlaSorgu
        {
            public List<int> IsciIdler { get; set; } = new();
        }
    }
}
