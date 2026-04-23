using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// İşçilərin ümumi iş stajı idarəetməsi. Default olaraq bankda işə gəlmə
    /// tarixi (IsheQebulTarixi) istifadə olunur. Əgər işçi bu banka gəlməzdən
    /// əvvəl başqa yerlərdə işləyibsə, HR burada UmumiIsStajiBaslangic-ı daxil
    /// edir — xəstəlik ödənişində staj faizi (60/80/100%) bu tarixə əsasən
    /// hesablanır.
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
    public class IsciStajiController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public IsciStajiController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET /HR/IsciStaji
        public async Task<IActionResult> Index(string? axtaris = null)
        {
            var q = _unitOfWork.Repository<Isci>().Query()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv);

            if (!string.IsNullOrWhiteSpace(axtaris))
            {
                var t = axtaris.Trim();
                q = q.Where(x => x.Ad.Contains(t) || x.Soyad.Contains(t)
                              || (x.FIN != null && x.FIN.Contains(t)));
            }

            var isciler = await q
                .Include(x => x.IsciTeyinatlari.Where(t => !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .OrderBy(x => x.Soyad).ThenBy(x => x.Ad)
                .ToListAsync();

            ViewBag.Axtaris = axtaris;
            ViewData["Title"] = "İşçi Stajı";
            return View(isciler);
        }

        // POST /HR/IsciStaji/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int isciId, DateTime? umumiIsStajiBaslangic)
        {
            try
            {
                var isci = await _unitOfWork.Repository<Isci>().IdIleGetirAsync(isciId);
                if (isci == null) return Json(new { success = false, message = "İşçi tapılmadı." });

                if (umumiIsStajiBaslangic.HasValue && umumiIsStajiBaslangic.Value > DateTime.Today)
                    return Json(new { success = false, message = "Staj başlanğıc tarixi gələcəkdə ola bilməz." });

                isci.UmumiIsStajiBaslangic = umumiIsStajiBaslangic;
                isci.YenilenmeTarixi = DateTime.Now;
                await _unitOfWork.Repository<Isci>().YenileAsync(isci);
                await _unitOfWork.YaddaSaxlaAsync();

                // Güncəl staj göstəriş üçün
                var bas = isci.UmumiIsStajiBaslangic ?? isci.IsheQebulTarixi;
                var ref_ = DateTime.Today;
                int il = ref_.Year - bas.Year;
                int ay = ref_.Month - bas.Month;
                if (ref_.Day < bas.Day) ay--;
                if (ay < 0) { il--; ay += 12; }
                int faiz = il < 8 ? 60 : il < 12 ? 80 : 100;

                return Json(new
                {
                    success = true,
                    message = "Staj yeniləndi.",
                    stajIl = il,
                    stajAy = ay,
                    faiz,
                    menbe = isci.UmumiIsStajiBaslangic.HasValue ? "ümumi" : "bank"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Xəta: {ex.Message}" });
            }
        }
    }
}
