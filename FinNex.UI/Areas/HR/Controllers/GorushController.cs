using FinNex.Application.DTOs.Communication;
using FinNex.Application.DTOs.Structur;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain.Entities.Communication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// HR → Görüşlər idarəetməsi (04.11.2026) — bütün şirkət görüşlərinə YALNIZ OXUMA
    /// baxışı. Görüşün özünü yaratmaq/redaktə etmək/ləğv etmək hələ də yalnız işçinin
    /// (təşkilatçının) öz səhifəsindən (`/User/Gorush`) mümkündür — bura toxunulmayıb.
    /// Sidebarda "Ezamiyyət İzləmə"-nin yanında görünür, çünki Davamiyyət-dəki
    /// "Tədbirdə" statusu elə bu modulun (`GorushIshtirakci`) datasından gəlir, amma
    /// HR-ın bu günə qədər ona baxa biləcəyi ayrıca bir ekranı yox idi.
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = "Admin,HR,Rehber,SobeReisi")]
    public class GorushController : Controller
    {
        private readonly IGorushService _gorush;
        private readonly IDepartmentService _departament;

        public GorushController(IGorushService gorush, IDepartmentService departament)
        {
            _gorush = gorush;
            _departament = departament;
        }

        public async Task<IActionResult> Index()
        {
            var departamentler = await _departament.HamisiniGetirAsync();
            ViewBag.Departamentler = departamentler.Success ? departamentler.Data! : new List<DepartmentListDto>();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetGorushler(
            DateTime? tarixFrom, DateTime? tarixTo, int? status, int? departamentId, string? axtaris)
        {
            var filtr = new GorushFiltrDto
            {
                TarixFrom = tarixFrom,
                TarixTo = tarixTo,
                Status = status.HasValue ? (GorushStatus?)status.Value : null,
                DepartamentId = departamentId,
                Axtaris = axtaris
            };
            var netice = await _gorush.GetHamisiniAsync(filtr);
            if (!netice.Success) return Json(new List<object>());

            return Json(netice.Data!.Select(g => new
            {
                g.Id,
                g.Bashliq,
                g.TeshkilatciAd,
                tarix = g.Tarix.ToString("dd.MM.yyyy"),
                baslamaSaati = g.BaslamaSaati.ToString(@"hh\:mm"),
                bitisSaati = g.BitisSaati.HasValue ? g.BitisSaati.Value.ToString(@"hh\:mm") : null,
                g.Yer,
                nov = (int)g.Nov,
                status = (int)g.Status,
                g.IshtirakciSayi,
                statusText = g.HesablanmisStatusText,
                statusClass = g.HesablanmisStatusClass
            }));
        }

        // ── GET /HR/Gorush/Detay/5 — yalnız oxuma ──────────────
        public async Task<IActionResult> Detay(int id)
        {
            var netice = await _gorush.GetDetayAsync(id, 0);
            if (!netice.Success || netice.Data == null)
            {
                TempData["Error"] = "Görüş tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = netice.Data.Bashliq;
            return View(netice.Data);
        }
    }
}
