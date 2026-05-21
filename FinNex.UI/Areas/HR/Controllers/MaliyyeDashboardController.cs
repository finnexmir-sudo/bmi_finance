using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
public class MaliyyeDashboardController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public MaliyyeDashboardController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    // ── GET /HR/MaliyyeDashboard ─────────────────────────────
    public IActionResult Index()
    {
        ViewData["Title"] = "Maliyyə Dashboard";
        return View();
    }

    // ── GET /HR/MaliyyeDashboard/GetSummary ──────────────────
    [HttpGet]
    public async Task<IActionResult> GetSummary()
    {
        var bugun  = DateTime.Today;
        var buAy   = bugun.Month;
        var buIl   = bugun.Year;
        var ayBasl = new DateTime(buIl, buAy, 1);
        var ayBiti = ayBasl.AddMonths(1);

        // Bu ay ödənilmiş xərclər
        var buAyOdenilmis = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib && x.Status == XercStatus.Odenildi
                     && x.XercTarixi >= ayBasl && x.XercTarixi < ayBiti)
            .SumAsync(x => (decimal?)x.Mebleg) ?? 0;

        // Bu ay gözləmədə olan xərclər (workflow-da)
        var buAyGozleme = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib
                     && (x.Status == XercStatus.Muraciet
                      || x.Status == XercStatus.SobeReisiTesdiq
                      || x.Status == XercStatus.HrTesdiq)
                     && x.XercTarixi >= ayBasl && x.XercTarixi < ayBiti)
            .SumAsync(x => (decimal?)x.Mebleg) ?? 0;

        // Bu ay büdcə planı (bütün departamentlər)
        var buAyPlan = await _unitOfWork.Repository<Budce>()
            .Query()
            .Where(x => !x.Silinib && x.Il == buIl && x.Ay == buAy)
            .SumAsync(x => (decimal?)x.PlanMebleg) ?? 0;

        // Aktiv gözlənilən xərclər (gələcək)
        var gozlenilenCem = await _unitOfWork.Repository<GozlenilenXerc>()
            .Query()
            .Where(x => !x.Silinib && x.Status == GozlenilenXercStatus.Aktiv)
            .SumAsync(x => (decimal?)x.TahminiMebleg) ?? 0;

        // Bu ay aktiv dövri ödənişlər sayı
        var dovriSay = await _unitOfWork.Repository<DovriOdenis>()
            .Query()
            .CountAsync(x => !x.Silinib && x.Aktiv
                          && x.NovbatiOdenisTarixi >= ayBasl
                          && x.NovbatiOdenisTarixi < ayBiti);

        // Gecikmiş dövri ödənişlər
        var gecikmisDovri = await _unitOfWork.Repository<DovriOdenis>()
            .Query()
            .CountAsync(x => !x.Silinib && x.Aktiv && x.NovbatiOdenisTarixi < bugun);

        var isletmeFaizi = buAyPlan > 0
            ? Math.Round((buAyOdenilmis + buAyGozleme) / buAyPlan * 100, 1)
            : 0;

        return Json(new
        {
            buAyOdenilmis,
            buAyGozleme,
            buAyPlan,
            gozlenilenCem,
            dovriSay,
            gecikmisDovri,
            isletmeFaizi,
            azad = buAyPlan - buAyOdenilmis - buAyGozleme
        });
    }

    // ── GET /HR/MaliyyeDashboard/GetAylikTrend ────────────────
    // Son 6 ayın Plan vs Faktiki
    [HttpGet]
    public async Task<IActionResult> GetAylikTrend()
    {
        var bugun = DateTime.Today;
        var aylar = Enumerable.Range(0, 6)
            .Select(i => bugun.AddMonths(-5 + i))
            .Select(d => new { d.Year, d.Month })
            .ToList();

        var xercler = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib && x.Status == XercStatus.Odenildi
                     && x.XercTarixi >= new DateTime(aylar[0].Year, aylar[0].Month, 1))
            .Select(x => new { x.Mebleg, x.XercTarixi.Year, x.XercTarixi.Month })
            .ToListAsync();

        var minYm = aylar[0].Year * 100 + aylar[0].Month;
        var maxYm = aylar[^1].Year * 100 + aylar[^1].Month;
        var budceler = await _unitOfWork.Repository<Budce>()
            .Query()
            .Where(x => !x.Silinib
                     && (x.Il * 100 + x.Ay) >= minYm
                     && (x.Il * 100 + x.Ay) <= maxYm)
            .Select(x => new { x.Il, x.Ay, x.PlanMebleg })
            .ToListAsync();

        var result = aylar.Select(a => new
        {
            etiket  = $"{AyAdi(a.Month)} {a.Year}",
            plan    = budceler.Where(b => b.Il == a.Year && b.Ay == a.Month).Sum(b => b.PlanMebleg),
            faktiki = xercler.Where(x => x.Year == a.Year && x.Month == a.Month).Sum(x => x.Mebleg)
        });

        return Json(result);
    }

    // ── GET /HR/MaliyyeDashboard/GetKateqoriyaBreakdown ──────
    // Bu ayın kateqoriya paylanması (ödənilmiş)
    [HttpGet]
    public async Task<IActionResult> GetKateqoriyaBreakdown()
    {
        var bugun  = DateTime.Today;
        var ayBasl = new DateTime(bugun.Year, bugun.Month, 1);
        var ayBiti = ayBasl.AddMonths(1);

        var data = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib && x.Status == XercStatus.Odenildi
                     && x.XercTarixi >= ayBasl && x.XercTarixi < ayBiti)
            .Include(x => x.Kateqoriya)
            .GroupBy(x => new { x.KateqoriyaId, x.Kateqoriya.Ad, x.Kateqoriya.Ikon })
            .Select(g => new
            {
                ad    = g.Key.Ad,
                ikon  = g.Key.Ikon ?? "bi-tag",
                mebleg = g.Sum(x => x.Mebleg),
                say    = g.Count()
            })
            .OrderByDescending(x => x.mebleg)
            .ToListAsync();

        return Json(data);
    }

    // ── GET /HR/MaliyyeDashboard/GetTopDept ──────────────────
    // Bu ay ən çox xərc edən departamentlər (top 8)
    [HttpGet]
    public async Task<IActionResult> GetTopDept()
    {
        var bugun  = DateTime.Today;
        var ayBasl = new DateTime(bugun.Year, bugun.Month, 1);
        var ayBiti = ayBasl.AddMonths(1);

        var data = await _unitOfWork.Repository<Xerc>()
            .Query()
            .Where(x => !x.Silinib && x.Status == XercStatus.Odenildi
                     && x.XercTarixi >= ayBasl && x.XercTarixi < ayBiti
                     && x.DepartamentId.HasValue)
            .Include(x => x.Departament)
            .GroupBy(x => new { x.DepartamentId, x.Departament!.Ad })
            .Select(g => new
            {
                departament = g.Key.Ad,
                mebleg      = g.Sum(x => x.Mebleg),
                say         = g.Count()
            })
            .OrderByDescending(x => x.mebleg)
            .Take(8)
            .ToListAsync();

        return Json(data);
    }

    // ── GET /HR/MaliyyeDashboard/GetYaxindaDovri ─────────────
    // Növbəti 14 gündəki dövri ödənişlər
    [HttpGet]
    public async Task<IActionResult> GetYaxindaDovri()
    {
        var bugun = DateTime.Today;
        var son   = bugun.AddDays(14);

        var data = await _unitOfWork.Repository<DovriOdenis>()
            .Query()
            .Where(x => !x.Silinib && x.Aktiv && x.NovbatiOdenisTarixi < son.AddDays(1))
            .Include(x => x.Kateqoriya)
            .Include(x => x.Departament)
            .OrderBy(x => x.NovbatiOdenisTarixi)
            .Take(10)
            .ToListAsync();

        return Json(data.Select(x => new
        {
            x.Id,
            x.Ad,
            kateqoriya  = x.Kateqoriya.Ad,
            departament = x.Departament.Ad,
            x.Mebleg,
            tarix   = x.NovbatiOdenisTarixi.ToString("dd.MM.yyyy"),
            gecikib = x.NovbatiOdenisTarixi.Date < bugun,
            buGun   = x.NovbatiOdenisTarixi.Date == bugun
        }));
    }

    // ── GET /HR/MaliyyeDashboard/GetYaxindaGozlenilen ────────
    // Növbəti 30 gündəki gözlənilən xərclər
    [HttpGet]
    public async Task<IActionResult> GetYaxindaGozlenilen()
    {
        var bugun = DateTime.Today;
        var son   = bugun.AddDays(30);

        var data = await _unitOfWork.Repository<GozlenilenXerc>()
            .Query()
            .Where(x => !x.Silinib && x.Status == GozlenilenXercStatus.Aktiv
                     && x.GozlenilenTarix.Date <= son)
            .Include(x => x.Kateqoriya)
            .Include(x => x.Departament)
            .OrderBy(x => x.GozlenilenTarix)
            .Take(10)
            .ToListAsync();

        return Json(data.Select(x => new
        {
            x.Id,
            x.Ad,
            kateqoriya  = x.Kateqoriya.Ad,
            departament = x.Departament?.Ad ?? "—",
            x.TahminiMebleg,
            tarix     = x.GozlenilenTarix.ToString("dd.MM.yyyy"),
            prioritet = x.Prioritet.ToString(),
            kecmis    = x.GozlenilenTarix.Date < bugun
        }));
    }

    private static string AyAdi(int ay) => ay switch
    {
        1 => "Yan", 2 => "Fev", 3 => "Mar", 4 => "Apr",
        5 => "May", 6 => "İyn", 7 => "İyl", 8 => "Avq",
        9 => "Sen", 10 => "Okt", 11 => "Noy", 12 => "Dek",
        _ => ay.ToString()
    };
}
