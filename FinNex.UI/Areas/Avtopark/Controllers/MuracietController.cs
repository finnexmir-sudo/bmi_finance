using FinNex.Application.DTOs.Avtopark;
using FinNex.Application.Interfaces.Avtopark;
using FinNex.Application.Services.Avtopark;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.Avtopark.Controllers
{
    /// <summary>
    /// İşçinin öz maşın müraciətləri — bütün işçilər görür.
    /// </summary>
    [Area("Avtopark")]
    [Authorize]
    public class MuracietController : AvtoparkControllerBase
    {
        private readonly IMasinMuracietService _muraciet;
        private readonly IMasinService _masin;

        public MuracietController(
            IMasinMuracietService muraciet,
            IMasinService masin,
            UserManager<AppUser> userManager) : base(userManager)
        {
            _muraciet = muraciet;
            _masin = masin;
        }

        public async Task<IActionResult> Index()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null)
            {
                TempData["Error"] = "Hesabınıza işçi profili bağlanmayıb — Admin ilə əlaqə saxlayın.";
                return View(new List<MasinMuracietListDto>());
            }

            var list = await _muraciet.GetIsciMuracietleriAsync(isciId.Value);

            // Rəhbər addımının olub-olmaması SERVİSİN qaydasından gəlir —
            // şərti burada və ya Razor-da yenidən qurmuruq (CLAUDE.md).
            var rehberAddimi = MasinMuracietService.RehberAddimiVar(Rehberdirmi());
            foreach (var m in list) m.RehberAddimiVar = rehberAddimi;

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await DoldurMasinlarAsync();

            var indi = DateTime.Now;
            return View(new MasinMuracietCreateDto
            {
                // Növbəti tam saat — istifadəçi çox vaxt "indidən" istəyir,
                // dəqiqəli vaxt isə formada çirkin görünür.
                PlanBaslama = indi.Date.AddHours(indi.Hour + 1),
                PlanBitme = indi.Date.AddHours(indi.Hour + 3)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MasinMuracietCreateDto dto)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return IsciProfiliYoxdur();

            // Bu iki sahə FORMADAN QƏBUL EDİLMİR — POST-u dəyişən istifadəçi
            // başqasının adından müraciət yaza və ya öz müraciətini avtomatik
            // təsdiqlədə bilərdi.
            dto.IsciId = isciId.Value;
            dto.MuracietSahibiRehberdirmi = Rehberdirmi();

            if (!ModelState.IsValid)
            {
                await DoldurMasinlarAsync();
                return View(dto);
            }

            var res = await _muraciet.YaratAsync(dto, GetUserId());
            if (!res.Success)
            {
                // TempData YOX — layout onu `.fn-alert` kimi göstərir və
                // `user-area.js` 4 saniyəyə silir; istifadəçi səbəbi oxumağa
                // macal tapmır (CLAUDE.md — «submit heç nə etmir» tələsi).
                // ModelState isə qalıcı validasiya xülasəsində qalır.
                ModelState.AddModelError(string.Empty, res.Message ?? "Əməliyyat alınmadı.");
                await DoldurMasinlarAsync();
                return View(dto);
            }

            TempData["Success"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Legv(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return IsciProfiliYoxdur();

            var res = await _muraciet.LegvEtAsync(id, isciId.Value, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Yalnız AKTİV maşınlar — təmirdə/istifadədən çıxmış maşın siyahıda
        /// görünməməlidir. Servis onsuz da bloklayır; siyahı ikinci qatdır ki,
        /// istifadəçi seçib xəta almasın.
        /// </summary>
        private async Task DoldurMasinlarAsync()
        {
            var masinlar = await _masin.HamisiniGetirAsync(yalnizAktiv: true);
            ViewBag.Masinlar = masinlar
                .Select(m => new SelectListItem(
                    m.IndiColdedir ? $"{m.TamAd} — hazırda {m.IndiKimde}-dədir" : m.TamAd,
                    m.Id.ToString()))
                .ToList();
        }
    }
}
