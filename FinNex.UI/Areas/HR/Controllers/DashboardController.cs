// Areas/HR/Controllers/DashboardController.cs
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = "HR,Admin")]
    public class DashboardController : Controller
    {
        private readonly IIsciService _isciService;
        private readonly IMezuniyyetService _mezuniyyetService;
        private readonly IDavamiyyetService _davamiyyetService;

        public DashboardController(
            IIsciService isciService,
            IMezuniyyetService mezuniyyetService,
            IDavamiyyetService davamiyyetService)
        {
            _isciService = isciService;
            _mezuniyyetService = mezuniyyetService;
            _davamiyyetService = davamiyyetService;
        }

        public async Task<IActionResult> Index()
        {
            var bugun = DateTime.Today;
            var buAyBaslangic = new DateTime(bugun.Year, bugun.Month, 1);

            // ── İşçilər ──────────────────────────────────────
            var iscilerResult = await _isciService.HamisiniGetirAsync();
            var isciler = iscilerResult.Success && iscilerResult.Data != null
                ? iscilerResult.Data.ToList()
                : new();

            var aktivisciSayi = isciler.Count(x => x.Status == IsciStatus.Aktiv);

            ViewBag.UmumiIsci = isciler.Count;
            ViewBag.AktivIsci = aktivisciSayi;
            ViewBag.SonIsciler = isciler
                .OrderByDescending(x => x.Id)
                .Take(5)
                .Select(x => new
                {
                    TamAd = x.TamAd,
                    Departament = x.SobeAdi ?? "—",
                    Vezife = x.VezifeAdi ?? "—",
                    ElaveTarixi = "—"
                })
                .ToList();

            // ── Məzuniyyətlər ─────────────────────────────────
            var mezResult = await _mezuniyyetService.GetListAsync();
            var mezler = mezResult.Success && mezResult.Data != null
                ? mezResult.Data.ToList()
                : new();

            ViewBag.Mezuniyyetde = mezler.Count(x =>
                x.Status == MezuniyyetStatus.Tesdiqlenib &&
                x.BaslamaTarixi.Date <= bugun &&
                x.BitmeTarixi.Date >= bugun);

            ViewBag.MezuniyyetBitir = mezler.Count(x =>
                x.Status == MezuniyyetStatus.Tesdiqlenib &&
                x.BitmeTarixi.Date > bugun &&
                x.BitmeTarixi.Date <= bugun.AddDays(7));

            // ── Bugünün davamiyyəti ───────────────────────────
            var davResult = await _davamiyyetService.TarixUzreAsync(bugun);
            var davlar = davResult?.ToList() ?? new();

            var gelenSayi = davlar.Count(x => x.Status == DavamiyyetStatus.Isde ||
                                                 x.Status == DavamiyyetStatus.Gecikme ||
                                                 x.Status == DavamiyyetStatus.Icazeli);
            var gecikenSayi = davlar.Count(x => x.Status == DavamiyyetStatus.Gecikme);
            var cixisEden = davlar.Count(x => x.CixisVaxti != null);

            ViewBag.Gelenler = gelenSayi;
            ViewBag.Gecikenler = gecikenSayi;
            ViewBag.Gecikme = gecikenSayi;
            ViewBag.Mezuniyyetdebugun = (int)ViewBag.Mezuniyyetde;
            ViewBag.CixisEden = cixisEden;

            int gelmeyen = aktivisciSayi - gelenSayi - (int)ViewBag.Mezuniyyetde;
            ViewBag.Gelmeyen = gelmeyen < 0 ? 0 : gelmeyen;

            // ── Davamiyyət faizi (bu ay) ──────────────────────
            var isGunleriSayi = 0;
            for (var d = buAyBaslangic; d <= bugun; d = d.AddDays(1))
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    isGunleriSayi++;

            var buAyDavResult = await _davamiyyetService.AraliqUzreAsync(buAyBaslangic, bugun);
            var buAyDavlar = buAyDavResult?.ToList() ?? new();

            var gelmelerSayi = buAyDavlar.Count(x =>
                x.Status == DavamiyyetStatus.Isde ||
                x.Status == DavamiyyetStatus.Gecikme);
            var maxGelmeMumkun = aktivisciSayi * isGunleriSayi;

            ViewBag.DavamiyyetFaizi = maxGelmeMumkun > 0
                ? (int)((double)gelmelerSayi / maxGelmeMumkun * 100)
                : 0;

            // ── Son girişlər (bugün, ən yeni 15 qeyd) ────────
            ViewBag.SonGirisler = davlar
                .Where(x => x.GirisVaxti != null)
                .OrderByDescending(x => x.GirisVaxti)
                .Take(15)
                .Select(x => new
                {
                    Ad = x.IsciTamAd ?? "—",
                    Departament = x.DepartamentAd ?? "—",
                    GirisVaxti = x.GirisVaxti?.ToString("HH:mm"),
                    CixisVaxti = x.CixisVaxti?.ToString("HH:mm"),
                    Status = (int)x.Status
                })
                .ToList();

            ViewData["Title"] = "HR Dashboard";
            return View();
        }
    }
}