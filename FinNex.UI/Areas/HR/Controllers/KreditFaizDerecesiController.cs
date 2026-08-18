using System.Security.Claims;
using FinNex.Application.DTOs.Kredit;
using FinNex.Application.Interfaces.Kredit;
using FinNex.Application.Interfaces.Kurval;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// VM 98.2.1 — bazar (MB / banklararası) kredit faiz dərəcəsi.
    ///
    /// İşçi krediti bazar dərəcəsindən aşağı faizlə verilirsə fərq hesabi gəlir
    /// sayılır və vergi/DSMF/İTSS/işsizlik bazalarına düşür. Bazar dərəcəsini
    /// mühasib burada saxlayır.
    ///
    /// DƏRƏCƏ REDAKTƏ EDİLMİR, YENİ SƏTİR ƏLAVƏ OLUNUR — keçmiş dövr yenidən
    /// hesablananda öz vaxtındakı dərəcə tapılmalıdır. Redaktə yalnız SƏHV
    /// yazılmış sətri düzəltmək üçündür.
    ///
    /// Clean Architecture: yalnız servis inject olunur, `IUnitOfWork` YOX.
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
    public class KreditFaizDerecesiController : Controller
    {
        private readonly IKreditFaizDerecesiService _service;
        private readonly IBmiValyutaService _valyuta;

        public KreditFaizDerecesiController(
            IKreditFaizDerecesiService service, IBmiValyutaService valyuta)
        {
            _service = service;
            _valyuta = valyuta;
        }

        private int GetUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        // Valyuta açılan siyahısı — BMI `kurval`-dan. Oracle əlçatmaz olsa
        // siyahı ehtiyat siyahıya düşür (BmiValyutaService), forma işləyir.
        // Seçimi `asp-for` özü idarə edir — burada yalnız siyahı ötürülür.
        private async Task DoldurValyutaAsync()
            => ViewBag.Valyutalar = await _valyuta.SiyahiAsync();

        public async Task<IActionResult> Index()
        {
            var list = await _service.HamisiniGetirAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await DoldurValyutaAsync();
            return View(new KreditFaizDerecesiCreateDto
            {
                Tarix = DateTime.Today,
                ValyutaKodu = FinNex.Domain.Entities.HR.KreditFaizDerecesi.AznKodu
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KreditFaizDerecesiCreateDto dto)
        {
            var res = await _service.YaratAsync(dto, GetUserId());
            if (!res.Success)
            {
                TempData["Error"] = res.Message;
                await DoldurValyutaAsync();
                return View(dto);
            }

            TempData["Success"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var hamisi = await _service.HamisiniGetirAsync();
            var e = hamisi.FirstOrDefault(x => x.Id == id);
            if (e == null) return NotFound();

            await DoldurValyutaAsync();
            return View(new KreditFaizDerecesiCreateDto
            {
                Id          = e.Id,
                Tarix       = e.Tarix,
                ValyutaKodu = e.ValyutaKodu,
                Derece      = e.Derece,
                Qeyd        = e.Qeyd
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(KreditFaizDerecesiCreateDto dto)
        {
            var res = await _service.YenileAsync(dto, GetUserId());
            if (!res.Success)
            {
                TempData["Error"] = res.Message;
                await DoldurValyutaAsync();
                return View(dto);
            }

            TempData["Success"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var res = await _service.SilAsync(id, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
