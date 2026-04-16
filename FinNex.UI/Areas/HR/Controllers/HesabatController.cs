using ClosedXML.Excel;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
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
                    xestelik = g.Count(x => x.Status == DavamiyyetStatus.Xestelik),
                    ezamiyyet = g.Count(x => x.Status == DavamiyyetStatus.Ezamiyyet),
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
                cemiXestelik = g.Sum(x => x.xestelik),
                cemiEzamiyyet = g.Sum(x => x.ezamiyyet),
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

    // ── Excel İxrac — Maaş ──────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ExportMaasExcel(int il, int ay)
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

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Maaş Hesabatı");

        // Başlıq
        ws.Cell(1, 1).Value = "İşçi";
        ws.Cell(1, 2).Value = "Departament";
        ws.Cell(1, 3).Value = "Gross (AZN)";
        ws.Cell(1, 4).Value = "Net (AZN)";
        ws.Cell(1, 5).Value = "Status";

        var headerRange = ws.Range(1, 1, 1, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e2a3b");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var m in maaslar)
        {
            var teyinat = m.Isci.IsciTeyinatlari.FirstOrDefault();
            ws.Cell(row, 1).Value = $"{m.Isci.Ad} {m.Isci.Soyad}";
            ws.Cell(row, 2).Value = teyinat?.Departament?.Ad ?? "Teyinatsız";
            ws.Cell(row, 3).Value = m.BrutMebleg;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Value = m.NetMebleg;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Value = m.Status.ToString();
            row++;
        }

        // Cəmi sətri
        ws.Cell(row, 1).Value = "CƏMİ";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = maaslar.Sum(x => x.BrutMebleg);
        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = maaslar.Sum(x => x.NetMebleg);
        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 4).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fileName = $"Maas_Hesabat_{il}_{ay:D2}.xlsx";
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // ── Excel İxrac — Davamiyyət ────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ExportDavamiyyetExcel(int il, int ay)
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
                    departament = teyinat?.Departament?.Ad ?? "Teyinatsız",
                    isde = g.Count(x => x.Status == DavamiyyetStatus.Isde),
                    gecikme = g.Count(x => x.Status == DavamiyyetStatus.Gecikme),
                    qayib = g.Count(x => x.Status == DavamiyyetStatus.Qayib),
                    icazeli = g.Count(x => x.Status == DavamiyyetStatus.Icazeli),
                    xestelik = g.Count(x => x.Status == DavamiyyetStatus.Xestelik),
                    ezamiyyet = g.Count(x => x.Status == DavamiyyetStatus.Ezamiyyet),
                    cemiGun = g.Count()
                };
            })
            .OrderBy(x => x.departament)
            .ThenBy(x => x.isciAdSoyad)
            .ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Davamiyyət");

        ws.Cell(1, 1).Value = "İşçi";
        ws.Cell(1, 2).Value = "Departament";
        ws.Cell(1, 3).Value = "İşdə";
        ws.Cell(1, 4).Value = "Gecikmə";
        ws.Cell(1, 5).Value = "Qayıb";
        ws.Cell(1, 6).Value = "İcazəli";
        ws.Cell(1, 7).Value = "Xəstəlik";
        ws.Cell(1, 8).Value = "Ezamiyyət";
        ws.Cell(1, 9).Value = "Cəmi gün";

        var headerRange = ws.Range(1, 1, 1, 9);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e2a3b");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var r in grouped)
        {
            ws.Cell(row, 1).Value = r.isciAdSoyad;
            ws.Cell(row, 2).Value = r.departament;
            ws.Cell(row, 3).Value = r.isde;
            ws.Cell(row, 4).Value = r.gecikme;
            ws.Cell(row, 5).Value = r.qayib;
            ws.Cell(row, 6).Value = r.icazeli;
            ws.Cell(row, 7).Value = r.xestelik;
            ws.Cell(row, 8).Value = r.ezamiyyet;
            ws.Cell(row, 9).Value = r.cemiGun;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fileName = $"Davamiyyet_Hesabat_{il}_{ay:D2}.xlsx";
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // ── Excel İxrac — Balans ────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ExportBalansExcel(int il)
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
                    departament = teyinat?.Departament?.Ad ?? "Teyinatsız",
                    illikToplam = illik?.ToplamGun ?? 0,
                    illikIstifade = illik?.IstifadeOlunanGun ?? 0,
                    illikQaliq = illik?.QaliqGun ?? 0,
                    xestelikIstifade = xestelik?.IstifadeOlunanGun ?? 0,
                    ezamiyyetIstifade = ezamiyyet?.IstifadeOlunanGun ?? 0
                };
            })
            .OrderBy(x => x.departament)
            .ThenBy(x => x.isciAdSoyad)
            .ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Balans");

        ws.Cell(1, 1).Value = "İşçi";
        ws.Cell(1, 2).Value = "Departament";
        ws.Cell(1, 3).Value = "İllik Toplam";
        ws.Cell(1, 4).Value = "İllik İstifadə";
        ws.Cell(1, 5).Value = "İllik Qalıq";
        ws.Cell(1, 6).Value = "Xəstəlik (İstifadə)";
        ws.Cell(1, 7).Value = "Ezamiyyət (İstifadə)";

        var headerRange = ws.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e2a3b");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var r in grouped)
        {
            ws.Cell(row, 1).Value = r.isciAdSoyad;
            ws.Cell(row, 2).Value = r.departament;
            ws.Cell(row, 3).Value = r.illikToplam;
            ws.Cell(row, 4).Value = r.illikIstifade;
            ws.Cell(row, 5).Value = r.illikQaliq;
            ws.Cell(row, 6).Value = r.xestelikIstifade;
            ws.Cell(row, 7).Value = r.ezamiyyetIstifade;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fileName = $"Balans_Hesabat_{il}.xlsx";
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // ── Vergi Hesabatı ──────────────────────────────────────
    public IActionResult VergiHesabati(int? il)
    {
        var cIl = il ?? DateTime.Now.Year;
        ViewBag.SecilmisIl = cIl;
        ViewData["Title"] = $"Vergi Hesabatı — {cIl}";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetVergiData(int il)
    {
        var maaslar = await _unitOfWork.Repository<Maas>()
            .Query()
            .Where(x => !x.Silinib && x.Il == il)
            .Include(x => x.Isci)
                .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Departament)
            .Include(x => x.Detallar)
            .ToListAsync();

        // İşçi bazasında qruplaşdır — bütün ayları cəmlə
        var grouped = maaslar
            .GroupBy(x => x.IsciId)
            .Select(g =>
            {
                var isci = g.First().Isci;
                var teyinat = isci.IsciTeyinatlari.FirstOrDefault();

                var brutCemi = g.Sum(m => m.BrutMebleg);
                var gelirVergisi = g.Sum(m =>
                    m.Detallar.Where(d => d.MaasNovuId == 6).Sum(d => d.Mebleg));
                var dsmf = g.Sum(m =>
                    m.Detallar.Where(d => d.MaasNovuId == 7).Sum(d => d.Mebleg));
                var issizlik = g.Sum(m =>
                    m.Detallar.Where(d => d.MaasNovuId == 8).Sum(d => d.Mebleg));

                return new
                {
                    isciAdSoyad = $"{isci.Ad} {isci.Soyad}",
                    departament = teyinat?.Departament?.Ad ?? "Teyinatsız",
                    departamentId = teyinat?.DepartamentId ?? 0,
                    brutMebleg = brutCemi,
                    gelirVergisi,
                    dsmf,
                    issizlik,
                    cemiTutulma = gelirVergisi + dsmf + issizlik
                };
            })
            .GroupBy(x => x.departament)
            .Select(g => new
            {
                departament = g.Key,
                isciler = g.OrderBy(x => x.isciAdSoyad).ToList(),
                cemiBrut = g.Sum(x => x.brutMebleg),
                cemiGelirVergisi = g.Sum(x => x.gelirVergisi),
                cemiDsmf = g.Sum(x => x.dsmf),
                cemiIssizlik = g.Sum(x => x.issizlik),
                cemiTutulma = g.Sum(x => x.cemiTutulma)
            })
            .OrderBy(x => x.departament)
            .ToList();

        var umumiBrut = grouped.Sum(x => x.cemiBrut);
        var umumiGelirVergisi = grouped.Sum(x => x.cemiGelirVergisi);
        var umumiDsmf = grouped.Sum(x => x.cemiDsmf);
        var umumiIssizlik = grouped.Sum(x => x.cemiIssizlik);
        var umumiTutulma = grouped.Sum(x => x.cemiTutulma);

        return Json(new
        {
            departamentlar = grouped,
            umumiBrut,
            umumiGelirVergisi,
            umumiDsmf,
            umumiIssizlik,
            umumiTutulma
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportVergiExcel(int il)
    {
        var maaslar = await _unitOfWork.Repository<Maas>()
            .Query()
            .Where(x => !x.Silinib && x.Il == il)
            .Include(x => x.Isci)
                .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Departament)
            .Include(x => x.Detallar)
            .ToListAsync();

        var grouped = maaslar
            .GroupBy(x => x.IsciId)
            .Select(g =>
            {
                var isci = g.First().Isci;
                var teyinat = isci.IsciTeyinatlari.FirstOrDefault();

                var brutCemi = g.Sum(m => m.BrutMebleg);
                var gelirVergisi = g.Sum(m =>
                    m.Detallar.Where(d => d.MaasNovuId == 6).Sum(d => d.Mebleg));
                var dsmf = g.Sum(m =>
                    m.Detallar.Where(d => d.MaasNovuId == 7).Sum(d => d.Mebleg));
                var issizlik = g.Sum(m =>
                    m.Detallar.Where(d => d.MaasNovuId == 8).Sum(d => d.Mebleg));

                return new
                {
                    isciAdSoyad = $"{isci.Ad} {isci.Soyad}",
                    departament = teyinat?.Departament?.Ad ?? "Teyinatsız",
                    brutMebleg = brutCemi,
                    gelirVergisi,
                    dsmf,
                    issizlik,
                    cemiTutulma = gelirVergisi + dsmf + issizlik
                };
            })
            .OrderBy(x => x.departament)
            .ThenBy(x => x.isciAdSoyad)
            .ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Vergi Hesabatı");

        ws.Cell(1, 1).Value = "İşçi";
        ws.Cell(1, 2).Value = "Departament";
        ws.Cell(1, 3).Value = "Gross Məbləğ (AZN)";
        ws.Cell(1, 4).Value = "Gəlir Vergisi (AZN)";
        ws.Cell(1, 5).Value = "DSMF (AZN)";
        ws.Cell(1, 6).Value = "İşsizlik (AZN)";
        ws.Cell(1, 7).Value = "Cəmi Tutulma (AZN)";

        var headerRange = ws.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e2a3b");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var r in grouped)
        {
            ws.Cell(row, 1).Value = r.isciAdSoyad;
            ws.Cell(row, 2).Value = r.departament;
            ws.Cell(row, 3).Value = r.brutMebleg;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Value = r.gelirVergisi;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Value = r.dsmf;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Value = r.issizlik;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = r.cemiTutulma;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        // Cəmi sətri
        ws.Cell(row, 1).Value = "CƏMİ";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = grouped.Sum(x => x.brutMebleg);
        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = grouped.Sum(x => x.gelirVergisi);
        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = grouped.Sum(x => x.dsmf);
        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 5).Style.Font.Bold = true;
        ws.Cell(row, 6).Value = grouped.Sum(x => x.issizlik);
        ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = grouped.Sum(x => x.cemiTutulma);
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 7).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fileName = $"Vergi_Hesabat_{il}.xlsx";
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
