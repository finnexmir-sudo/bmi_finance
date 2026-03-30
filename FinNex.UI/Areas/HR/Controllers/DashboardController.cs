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

            // ── İşçilər ──────────────────────────────────────
            var iscilerResult = await _isciService.HamisiniGetirAsync();
            var isciler = iscilerResult.Success && iscilerResult.Data != null
                ? iscilerResult.Data.ToList()
                : new();

            ViewBag.UmumiIsci = isciler.Count;
            ViewBag.AktivIsci = isciler.Count(x => x.Status == IsciStatus.Aktiv);

            ViewBag.SonIsciler = isciler
                .OrderByDescending(x => x.Id)
                .Take(5)
                .Select(x => new
                {
                    TamAd = x.TamAd,
                    Departament = x.SobeAdi ?? "-",
                    Vezife = x.VezifeAdi ?? "-",
                    ElaveTarixi = "—"
                })
                .ToList();

            // ── Məzuniyyətlər ─────────────────────────────────
            var mezResult = await _mezuniyyetService.GetListAsync();
            var mezler = mezResult.Success && mezResult.Data != null
                ? mezResult.Data.ToList()
                : new();

            // Hazırda aktiv (təsdiqlənmiş) məzuniyyətdə olanlar
            ViewBag.Mezuniyyetde = mezler.Count(x =>
                x.Status == MezuniyyetStatus.Tesdiqlenib &&
                x.BaslamaTarixi.Date <= bugun &&
                x.BitmeTarixi.Date >= bugun);

            // Bu həftə bitəcək məzuniyyətlər
            ViewBag.MezuniyyetBitir = mezler.Count(x =>
                x.Status == MezuniyyetStatus.Tesdiqlenib &&
                x.BitmeTarixi.Date > bugun &&
                x.BitmeTarixi.Date <= bugun.AddDays(7));

            // ── Bugünün davamiyyəti ───────────────────────────
            var davResult = await _davamiyyetService.TarixUzreAsync(bugun);
            var davlar = davResult?.ToList() ?? new();

            ViewBag.Gelenler = davlar.Count(x =>
                x.Status == DavamiyyetStatus.Isde ||
                x.Status == DavamiyyetStatus.Gecikme ||
                x.Status == DavamiyyetStatus.Icazeli);

            ViewBag.Gecikenler = davlar.Count(x => x.Status == DavamiyyetStatus.Gecikme);
            ViewBag.Gecikme = (int)ViewBag.Gecikenler;
            ViewBag.Mezuniyyetdebugun = (int)ViewBag.Mezuniyyetde;

            int gelmeyen = (int)ViewBag.AktivIsci
                         - (int)ViewBag.Gelenler
                         - (int)ViewBag.Mezuniyyetde;
            ViewBag.Gelmeyen = gelmeyen < 0 ? 0 : gelmeyen;

            ViewData["Title"] = "HR Dashboard";
            return View();
        }
    }
}