using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class JetonAnalitikaController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private const int AnomaliyaLimit = 10;

        public JetonAnalitikaController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Jeton Analitika";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            // ── İşçi → Departament xəritəsi ──────────────────────────
            var teyinatlar = await _unitOfWork.Repository<IsciTeyinat>()
                .Query()
                .Include(t => t.Departament)
                .Where(t => t.Aktivdir && !t.Silinib)
                .ToListAsync();

            var isciDeptMap = teyinatlar
                .GroupBy(t => t.IsciId)
                .ToDictionary(g => g.Key, g => g.First().Departament?.Ad ?? "Digər");

            // ── Bütün jetonları yüklə ─────────────────────────────────
            var butunJetonlar = await _unitOfWork.Repository<IsciJetonu>()
                .Query()
                .Include(x => x.JetonTeyinati)
                .Where(x => !x.Silinib)
                .OrderByDescending(x => x.QazanmaTarixi)
                .ToListAsync();

            // ── KPI ───────────────────────────────────────────────────
            var aktiveMusbetler = butunJetonlar
                .Where(x => x.Status == IsciJetonuStatus.Aktiv && x.JetonTeyinati.Nov == JetonNovu.Musbat)
                .ToList();
            var aktiveMenfiler = butunJetonlar
                .Where(x => x.Status == IsciJetonuStatus.Aktiv && x.JetonTeyinati.Nov == JetonNovu.Menfi)
                .ToList();

            var toplamSaat = aktiveMusbetler.Sum(x => x.JetonTeyinati.SaatDeyeri);
            var toplamAzn = toplamSaat; // 1 saat = 1 AZN

            // ── Gözləyən xərcləmə sorğuları ──────────────────────────
            var xerclemer = await _unitOfWork.Repository<JetonRedimTelebi>()
                .Query()
                .Where(x => !x.Silinib && x.Status == RedimStatus.Gozlenilir)
                .ToListAsync();

            var gozleyenSayi = xerclemer.Count;
            var yukselekSayi = xerclemer.Count(x => x.CemiSaat > 24);

            // ── Trend (son 6 ay) ──────────────────────────────────────
            var son6Ay = DateTime.Now.AddMonths(-5);
            var trendBaslanqic = new DateTime(son6Ay.Year, son6Ay.Month, 1);

            var trendJetonlar = butunJetonlar
                .Where(x => x.QazanmaTarixi >= trendBaslanqic)
                .ToList();

            var trend = trendJetonlar
                .GroupBy(x => new { x.QazanmaTarixi.Year, x.QazanmaTarixi.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    ay = AyAdGetir(g.Key.Year, g.Key.Month),
                    musbat = g.Count(x => x.JetonTeyinati.Nov == JetonNovu.Musbat),
                    menfi = g.Count(x => x.JetonTeyinati.Nov == JetonNovu.Menfi)
                }).ToList();

            // ── Departament müqayisəsi ─────────────────────────────────
            var deptData = butunJetonlar
                .GroupBy(x => isciDeptMap.GetValueOrDefault(x.IsciId, "Digər"))
                .Select(g => new
                {
                    ad = g.Key,
                    musbat = g.Count(x => x.JetonTeyinati.Nov == JetonNovu.Musbat),
                    menfi = g.Count(x => x.JetonTeyinati.Nov == JetonNovu.Menfi),
                    aktivSaat = g
                        .Where(x => x.JetonTeyinati.Nov == JetonNovu.Musbat && x.Status == IsciJetonuStatus.Aktiv)
                        .Sum(x => x.JetonTeyinati.SaatDeyeri)
                })
                .OrderByDescending(x => x.musbat + x.menfi)
                .Take(10)
                .ToList();

            // ── Qara jeton səbəbləri ──────────────────────────────────
            var qaraJetonlar = butunJetonlar
                .Where(x => x.JetonTeyinati.Nov == JetonNovu.Menfi)
                .ToList();

            var sebebler = qaraJetonlar
                .GroupBy(x => x.Sebeb.Length > 40 ? x.Sebeb.Substring(0, 40) : x.Sebeb)
                .Select(g => new { sebeb = g.Key, sayi = g.Count() })
                .OrderByDescending(x => x.sayi)
                .Take(8)
                .ToList();

            // ── Anomaliyalar ──────────────────────────────────────────
            var anomGruplar = butunJetonlar
                .GroupBy(x => new { x.VerenUserId, x.QazanmaTarixi.Year, x.QazanmaTarixi.Month })
                .Where(g => g.Count() > AnomaliyaLimit)
                .ToList();

            var verenIds = anomGruplar.Select(g => g.Key.VerenUserId).Distinct().ToList();
            var verenUsers = await _userManager.Users
                .Where(u => verenIds.Contains(u.Id))
                .ToListAsync();
            var userMap = verenUsers.ToDictionary(u => u.Id, u => u.UserName ?? "—");

            var anomaliyalar = anomGruplar
                .Select(g => new
                {
                    verenAd = userMap.GetValueOrDefault(g.Key.VerenUserId, "—"),
                    sayi = g.Count(),
                    ay = AyAdGetir(g.Key.Year, g.Key.Month),
                    risk = g.Count() > AnomaliyaLimit * 2 ? "critical" : "warning"
                })
                .OrderByDescending(x => x.sayi)
                .ToList();

            return Json(new
            {
                kpi = new
                {
                    toplamMusbetJetonSayi = aktiveMusbetler.Count,
                    toplamMenfiJetonSayi = aktiveMenfiler.Count,
                    toplamSaat,
                    toplamAzn,
                    gozleyenXercleme = gozleyenSayi,
                    yukselekSahiyyetli = yukselekSayi
                },
                trend,
                departament = deptData,
                qaraJetonSebebler = sebebler,
                anomaliyalar
            });
        }

        private static string AyAdGetir(int year, int month)
        {
            var aylar = new[] { "", "Yan", "Fev", "Mar", "Apr", "May", "İyn",
                                "İyl", "Avq", "Sen", "Okt", "Noy", "Dek" };
            return $"{aylar[month]} {year}";
        }
    }
}
