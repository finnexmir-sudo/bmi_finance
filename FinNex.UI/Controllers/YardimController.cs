using System.Security.Claims;
using FinNex.Application.Helpers.Yardim;
using FinNex.Application.Interfaces.Yardim;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Controllers
{
    /// <summary>
    /// Səhifə təlimatları — istifadəçi tərəfi (27.08.2026).
    ///
    ///  · <c>Panel</c>  — «?» düyməsinin JSON mənbəyi (hər səhifədən çağırılır)
    ///  · <c>Index</c>  — bütün təlimatların indeksi (modul üzrə, axtarışlı)
    ///  · <c>Sehife</c> — qısa ünvanla birbaşa link (<c>/Yardim/Sehife/mezuniyyet-muracieti</c>)
    ///
    /// Admin redaktoru AYRI yerdədir: Areas/Admin/YardimController.
    /// </summary>
    [Authorize]
    public class YardimController : Controller
    {
        private readonly ISehifeYardimiService _service;

        public YardimController(ISehifeYardimiService service)
        {
            _service = service;
        }

        private bool IsAdmin() => User.IsInRole(RoleNames.Admin);

        /// <summary>
        /// «?» düyməsi bunu çağırır. Açar CLIENT tərəfdən gəlir, çünki düymə
        /// layout-dadır və cari səhifənin marşrutunu yalnız o bilir.
        ///
        /// Qeyd tapılmasa da 200 qaytarılır (`var: false`) — səhifə yardım
        /// üzündən xəta göstərməməlidir.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Panel(string? acar)
        {
            if (string.IsNullOrWhiteSpace(acar))
                return Json(new { var = false });

            var d = await _service.PanelAsync(acar, IsAdmin());

            // Sayğac yalnız REAL baxışda artır — yazılmamış səhifə statistikanı
            // şişirtməsin (məqsəd «hansı səhifə qarışıqdır» sualına cavabdır).
            if (d.Var && !d.Hazirlanir)
                await _service.BaxisArtirAsync(acar);

            return Json(new
            {
                var        = d.Var,
                hazirlanir = d.Hazirlanir,
                basliq     = d.Basliq,
                modul      = d.Modul,
                xulase     = d.Xulase,
                metn       = d.Metn,
                slug       = d.Slug,
                acar       = d.Acar,
                yenilenme  = d.Yenilenme?.ToString("dd.MM.yyyy"),
                // Admin panelin içindən birbaşa redaktəyə keçə bilsin
                adminmi    = IsAdmin()
            });
        }

        /// <summary>Bütün təlimatların indeksi — «ümumi səhifə».</summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? q)
        {
            ViewBag.Axtaris = q;
            ViewBag.IsAdmin = IsAdmin();
            var siyahi = await _service.SiyahiAsync(q, IsAdmin());
            return View(siyahi);
        }

        /// <summary>
        /// Qısa ünvanla açılan tam səhifə — çatda link paylaşmaq üçün.
        ///
        /// ⚠️ AÇIQ MARŞRUT ATRİBUTU MƏCBURİDİR: standart şablon
        /// <c>{controller}/{action}/{id?}</c>-dır, yəni ünvandakı üçüncü hissə
        /// <c>id</c>-yə bağlanardı və <c>slug</c> HƏMİŞƏ boş gələrdi — səhifə
        /// «tapılmadı» verərdi, heç bir xəta çıxmadan.
        /// </summary>
        [HttpGet]
        [Route("Yardim/Sehife/{slug}")]
        public async Task<IActionResult> Sehife(string slug)
        {
            var d = await _service.SlugaGoreAsync(slug ?? "", IsAdmin());
            if (d == null)
            {
                TempData["Error"] = "Belə təlimat tapılmadı.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.IsAdmin = IsAdmin();
            return View(d);
        }
    }
}
