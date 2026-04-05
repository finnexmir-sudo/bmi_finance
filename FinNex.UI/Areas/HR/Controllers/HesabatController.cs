using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
public class HesabatController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public HesabatController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── GET /HR/Hesabat ─────────────────────────────────────
    public IActionResult Index()
    {
        ViewData["Title"] = "HR Hesabatları";
        return View();
    }

    // ── Maaş Hesabatı ───────────────────────────────────────
    public async Task<IActionResult> MaasHesabati(int? il, int? ay)
    {
        var cIl = il ?? DateTime.Now.Year;
        var cAy = ay ?? DateTime.Now.Month;

        ViewBag.SecilmisIl = cIl;
        ViewBag.SecilmisAy = cAy;
        ViewData["Title"] = $"Maaş Hesabatı — {cIl}/{cAy:D2}";

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetMaasData(int il, int ay)
    {
        var maaslar = await _unitOfWork.Repository<Maas>()
            .Query()
            .Where(x => !x.Silinib && x.Il == il && x.Ay == ay)
            .Include(x => x.Isci)
                .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Departament)
            .OrderBy(x => x.Isci.Soyad)
            .ThenBy(x => x.Isci.Ad)
            .ToListAsync();

        var data = maaslar.Select(m =>
        {
            var teyinat = m.Isci.IsciTeyinatlari.FirstOrDefault();
            return new
            {
                isciAdSoyad = $"{m.Isci.Ad} {m.Isci.Soyad}",
                departament = teyinat?.Departament?.Ad ?? "Teyinatsiz",
                departamentId = teyinat?.DepartamentId ?? 0,
                brutMebleg = m.BrutMebleg,
                netMebleg = m.NetMebleg,
                status = m.Status.ToString(),
                statusId = (int)m.Status
            };
        })
        .GroupBy(x => x.departament)
        .Select(g => new
        {
            departament = g.Key,
            isciler = g.ToList(),
            cemibrut = g.Sum(x => x.brutMebleg),
            ceminet = g.Sum(x => x.netMebleg)
        })
        .OrderBy(x => x.departament)
        .ToList();

        var umumibrut = data.Sum(x => x.cemibrut);
        var umuminet = data.Sum(x => x.ceminet);

        return Json(new { departamentlar = data, umumibrut, umuminet });
    }

    // ── Davamiyyət Hesabatı ─────────────────────────────────
    public async Task<IActionResult> DavamiyyetHesabati(int? il, int? ay)
    {
        var cIl = il ?? DateTime.Now.Year;
        var cAy = ay ?? DateTime.Now.Month;

        ViewBag.SecilmisIl = cIl;
        ViewBag.SecilmisAy = cAy;
        ViewData["Title"] = $"Davamiyyət Hesabatı — {cIl}/{cAy:D2}";

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetDavamiyyetData(int il, int ay)
    {
        var baslama = new DateTime(il, ay, 1);
        var bitme = baslama.AddMonths(1);

        var davamiyyetler = await _unitOfWork.Repository<Davamiyyet>()
            .Query()
            .Where(x => !x.Silinib && x.Tarix >= baslama && x.Tarix < bitme)
            .Include(x => x.Isci)
                .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Departament)
            .ToListAsync();

        var grouped = davamiyyetler
            .GroupBy(x => new { x.IsciId, AdSoyad = $"{x.Isci.Ad} {x.Isci.Soyad}" })
            .Select(g =>
            {
                var teyinat = g.First().Isci.IsciTeyinatlari.FirstOrDefault();
                return new
                {
                    isciAdSoyad = g.Key.AdSoyad,
                    departament = teyinat?.Departament?.Ad ?? "Teyinatsiz",
                    departamentId = teyinat?.DepartamentId ?? 0,
                    isde = g.Count(x => x.Status == DavamiyyetStatus.Isde),
                    gecikme = g.Count(x => x.Status == DavamiyyetStatus.Gecikme),
                    qayib = g.Count(x => x.Status == DavamiyyetStatus.Qayib),
                    icazeli = g.Count(x => x.Status == DavamiyyetStatus.Icazeli),
                    cemiGun = g.Count()
                };
            })
            .GroupBy(x => x.departament)
            .Select(g => new
            {
                departament = g.Key,
                isciler = g.OrderBy(x => x.isciAdSoyad).ToList(),
                cemiIsde = g.Sum(x => x.isde),
                cemiGecikme = g.Sum(x => x.gecikme),
                cemiQayib = g.Sum(x => x.qayib),
                cemiIcazeli = g.Sum(x => x.icazeli),
                cemiGun = g.Sum(x => x.cemiGun)
            })
            .OrderBy(x => x.departament)
            .ToList();

        return Json(new { departamentlar = grouped });
    }

    // ── Balans Hesabatı ─────────────────────────────────────
    public async Task<IActionResult> BalansHesabati(int? il)
    {
        var cIl = il ?? DateTime.Now.Year;

        ViewBag.SecilmisIl = cIl;
        ViewData["Title"] = $"Balans Hesabatı — {cIl}";

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetBalansData(int il)
    {
        var balanslar = await _unitOfWork.Repository<MezuniyyetBalans>()
            .Query()
            .Where(x => !x.Silinib && x.Il == il)
            .Include(x => x.Isci)
                .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Departament)
            .ToListAsync();

        var grouped = balanslar
            .GroupBy(x => x.IsciId)
            .Select(g =>
            {
                var isci = g.First().Isci;
                var teyinat = isci.IsciTeyinatlari.FirstOrDefault();

                MezuniyyetBalans? GetBalans(MezuniyyetNovu nov) =>
                    g.FirstOrDefault(x => x.Nov == nov);

                var illik = GetBalans(MezuniyyetNovu.Illik);
                var xestelik = GetBalans(MezuniyyetNovu.Xestelik);
                var ezamiyyet = GetBalans(MezuniyyetNovu.Ezamiyyet);

                return new
                {
                    isciAdSoyad = $"{isci.Ad} {isci.Soyad}",
                    departament = teyinat?.Departament?.Ad ?? "Teyinatsiz",
                    departamentId = teyinat?.DepartamentId ?? 0,
                    illikToplam = illik?.ToplamGun ?? 0,
                    illikIstifade = illik?.IstifadeOlunanGun ?? 0,
                    illikQaliq = illik?.QaliqGun ?? 0,
                    xestelikToplam = xestelik?.ToplamGun ?? 0,
                    xestelikIstifade = xestelik?.IstifadeOlunanGun ?? 0,
                    xestelikQaliq = xestelik?.QaliqGun ?? 0,
                    ezamiyyetToplam = ezamiyyet?.ToplamGun ?? 0,
                    ezamiyyetIstifade = ezamiyyet?.IstifadeOlunanGun ?? 0,
                    ezamiyyetQaliq = ezamiyyet?.QaliqGun ?? 0
                };
            })
            .GroupBy(x => x.departament)
            .Select(g => new
            {
                departament = g.Key,
                isciler = g.OrderBy(x => x.isciAdSoyad).ToList()
            })
            .OrderBy(x => x.departament)
            .ToList();

        return Json(new { departamentlar = grouped });
    }
}
