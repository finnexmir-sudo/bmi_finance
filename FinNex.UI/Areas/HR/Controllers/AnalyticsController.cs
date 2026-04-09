using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class AnalyticsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AnalyticsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ── GET /HR/Analytics ────────────────────────────────────
        public IActionResult Index()
        {
            ViewData["Title"] = "HR Analitika";
            return View();
        }

        // ── GET /HR/Analytics/GetAnalyticsData ───────────────────
        [HttpGet]
        public async Task<IActionResult> GetAnalyticsData()
        {
            var bugun = DateTime.Today;
            var sonOnIkiAyBaslangic = bugun.AddMonths(-12);

            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => !x.Silinib)
                .ToListAsync();

            // Isci sayi
            var aktivSayi = isciler.Count(x => x.Status == IsciStatus.Aktiv);
            var passivSayi = isciler.Count(x => x.Status != IsciStatus.Aktiv);

            // Gender bolgusu
            var kisiSayi = isciler.Count(x => x.Cins == Cins.Kisi && x.Status == IsciStatus.Aktiv);
            var qadinSayi = isciler.Count(x => x.Cins == Cins.Qadin && x.Status == IsciStatus.Aktiv);

            // Departament statistikasi
            var teyinatlar = await _unitOfWork.Repository<IsciTeyinat>()
                .Query()
                .Where(x => x.BitmeTarixi == null && !x.Silinib)
                .Include(x => x.Departament)
                .Include(x => x.Isci)
                .Where(x => x.Isci.Status == IsciStatus.Aktiv)
                .ToListAsync();

            var departamentStat = teyinatlar
                .GroupBy(x => x.Departament?.Ad ?? "Bilinməyən")
                .Select(g => new { Ad = g.Key, Sayi = g.Count() })
                .OrderByDescending(x => x.Sayi)
                .ToList();

            // Orta is muddeti (ay)
            var aktivIsciler = isciler.Where(x => x.Status == IsciStatus.Aktiv).ToList();
            double ortaIsMuddeti = aktivIsciler.Any()
                ? aktivIsciler.Average(x => (bugun - x.IsheQebulTarixi).TotalDays / 30.0)
                : 0;

            // Isci donusum faizi (son 12 ayda isden cixan / orta isci sayi * 100)
            var sonOnIkiAydaCixanlar = isciler
                .Count(x => x.IsdenAyrilmaTarixi.HasValue &&
                            x.IsdenAyrilmaTarixi.Value >= sonOnIkiAyBaslangic);

            var ortaIsciSayi = aktivSayi + (sonOnIkiAydaCixanlar / 2.0);
            var donusumFaizi = ortaIsciSayi > 0
                ? Math.Round(sonOnIkiAydaCixanlar / ortaIsciSayi * 100, 1)
                : 0;

            // Aylik ise qebul/cixis trendi (son 12 ay)
            var trendData = new List<object>();
            for (int i = 11; i >= 0; i--)
            {
                var ay = bugun.AddMonths(-i);
                var ayBaslangic = new DateTime(ay.Year, ay.Month, 1);
                var aySon = ayBaslangic.AddMonths(1);

                var qebulSayi = isciler.Count(x =>
                    x.IsheQebulTarixi >= ayBaslangic && x.IsheQebulTarixi < aySon);

                var cixisSayi = isciler.Count(x =>
                    x.IsdenAyrilmaTarixi.HasValue &&
                    x.IsdenAyrilmaTarixi.Value >= ayBaslangic &&
                    x.IsdenAyrilmaTarixi.Value < aySon);

                trendData.Add(new
                {
                    Ay = ayBaslangic.ToString("MMM yyyy"),
                    Qebul = qebulSayi,
                    Cixis = cixisSayi
                });
            }

            return Json(new
            {
                AktivIsciSayi = aktivSayi,
                PassivIsciSayi = passivSayi,
                KisiSayi = kisiSayi,
                QadinSayi = qadinSayi,
                DepartamentStat = departamentStat,
                OrtaIsMuddeti = Math.Round(ortaIsMuddeti, 1),
                DonusumFaizi = donusumFaizi,
                SonOnIkiAydaCixanlar = sonOnIkiAydaCixanlar,
                AylikTrend = trendData
            });
        }
    }
}
