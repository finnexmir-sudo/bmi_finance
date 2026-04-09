using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class PerformansController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public PerformansController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ── GET /HR/Performans ───────────────────────────────────
        public async Task<IActionResult> Index(int? il, int? rubu, int? departamentId)
        {
            var cIl = il ?? DateTime.Now.Year;

            await FilterSiyahilariniDoldur(cIl, rubu, departamentId);

            ViewBag.SecilmisIl = cIl;
            ViewBag.SecilmisRubu = rubu;
            ViewBag.SecilmisDepartamentId = departamentId;
            ViewData["Title"] = "Performans Qiymətləndirmə";
            return View();
        }

        // ── GET /HR/Performans/GetData ───────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetData(int? il, int? rubu, int? departamentId)
        {
            var cIl = il ?? DateTime.Now.Year;

            var query = _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query()
                .Where(x => !x.Silinib && x.Il == cIl)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Departament)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Vezife)
                .Include(x => x.QiymetlendirenIsci)
                .Include(x => x.Kriteriyalar)
                .AsQueryable();

            if (rubu.HasValue)
                query = query.Where(x => x.Rubu == rubu.Value);

            var list = await query.OrderByDescending(x => x.YaradilmaTarixi).ToListAsync();

            if (departamentId.HasValue)
            {
                list = list.Where(x => x.Isci.IsciTeyinatlari
                    .Any(t => t.DepartamentId == departamentId.Value)).ToList();
            }

            var data = list.Select(x =>
            {
                var teyinat = x.Isci.IsciTeyinatlari.FirstOrDefault();
                return new
                {
                    x.Id,
                    IsciAd = $"{x.Isci.Ad} {x.Isci.Soyad}",
                    Departament = teyinat?.Departament?.Ad ?? "—",
                    Vezife = teyinat?.Vezife?.Ad ?? "—",
                    DovrTipi = x.DovrTipi == PerformansDovrTipi.Rublik ? "Rüblük" : "İllik",
                    x.Il,
                    x.Rubu,
                    Status = (int)x.Status,
                    StatusAd = StatusAdi(x.Status),
                    x.YekunQiymet,
                    x.IsciOrtalamaQiymet,
                    x.MudirOrtalamaQiymet,
                    KriteriyaSayi = x.Kriteriyalar.Count
                };
            });

            return Json(data);
        }

        // ── GET /HR/Performans/Create ────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await YaratmaFormSiyahilariDoldur();
            ViewData["Title"] = "Yeni Performans Qiymətləndirmə";
            return View();
        }

        // ── POST /HR/Performans/Create ───────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int isciId, int qiymetlendirenIsciId,
            int dovrTipi, int il, int? rubu)
        {
            if (isciId <= 0 || qiymetlendirenIsciId <= 0)
            {
                TempData["Error"] = "İşçi və qiymətləndirən seçilməlidir.";
                await YaratmaFormSiyahilariDoldur();
                return View();
            }

            var performans = new PerformansQiymetlendirme
            {
                IsciId = isciId,
                QiymetlendirenIsciId = qiymetlendirenIsciId,
                DovrTipi = (PerformansDovrTipi)dovrTipi,
                Il = il,
                Rubu = rubu,
                Status = PerformansStatus.Gozlemede
            };

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YaratAsync(performans);
            await _unitOfWork.YaddaSaxlaAsync();

            // Default kriteriyalar
            var defaultKriteriyalar = new List<(string ad, decimal ceki)>
            {
                ("İş keyfiyyəti", 30),
                ("Vaxtında icra", 20),
                ("Komanda işi", 20),
                ("Təşəbbüskarlıq", 15),
                ("Peşəkar inkişaf", 15)
            };

            foreach (var (ad, ceki) in defaultKriteriyalar)
            {
                var kriteriya = new PerformansKriteriya
                {
                    PerformansId = performans.Id,
                    KriteriyaAdi = ad,
                    Ceki = ceki
                };
                await _unitOfWork.Repository<PerformansKriteriya>().YaratAsync(kriteriya);
            }
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = "Performans qiymətləndirmə uğurla yaradıldı.";
            return RedirectToAction(nameof(Detal), new { id = performans.Id });
        }

        // ── GET /HR/Performans/Detal/5 ───────────────────────────
        public async Task<IActionResult> Detal(int id)
        {
            var performans = await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query()
                .Where(x => x.Id == id && !x.Silinib)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Departament)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Vezife)
                .Include(x => x.QiymetlendirenIsci)
                .Include(x => x.Kriteriyalar)
                .FirstOrDefaultAsync();

            if (performans == null)
            {
                TempData["Error"] = "Performans qiymətləndirmə tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = $"Performans — {performans.Isci.Ad} {performans.Isci.Soyad}";
            return View(performans);
        }

        // ── POST /HR/Performans/IsciQiymetlendir ─────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IsciQiymetlendir(int performansId,
            string? isciSherhi, string? inkisafPlani,
            List<int> kriteriyaIds, List<decimal> isciQiymetleri, List<string?> isciSherhler)
        {
            var performans = await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query()
                .Where(x => x.Id == performansId && !x.Silinib)
                .Include(x => x.Kriteriyalar)
                .FirstOrDefaultAsync();

            if (performans == null)
            {
                TempData["Error"] = "Qiymətləndirmə tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            // Kriteriyalari yenilə
            for (int i = 0; i < kriteriyaIds.Count; i++)
            {
                var krit = performans.Kriteriyalar.FirstOrDefault(k => k.Id == kriteriyaIds[i]);
                if (krit != null)
                {
                    krit.IsciQiymeti = isciQiymetleri[i];
                    krit.IsciSherhi = isciSherhler.Count > i ? isciSherhler[i] : null;
                    await _unitOfWork.Repository<PerformansKriteriya>().YenileAsync(krit);
                }
            }

            // Orta qiymeti hesabla
            decimal toplamCeki = performans.Kriteriyalar.Sum(k => k.Ceki);
            decimal ortaQiymet = toplamCeki > 0
                ? performans.Kriteriyalar
                    .Where(k => k.IsciQiymeti.HasValue)
                    .Sum(k => k.IsciQiymeti!.Value * k.Ceki) / toplamCeki
                : 0;

            performans.IsciOrtalamaQiymet = Math.Round(ortaQiymet, 2);
            performans.IsciSherhi = isciSherhi;
            performans.InkisafPlani = inkisafPlani;
            performans.IsciQiymetlendirmeTarixi = DateTime.Now;
            performans.Status = PerformansStatus.IsciQiymetlendirdi;

            // Yekun qiymeti hesabla
            HesablaYekunQiymet(performans);

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(performans);
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = "İşçi qiymətləndirməsi uğurla qeydə alındı.";
            return RedirectToAction(nameof(Detal), new { id = performansId });
        }

        // ── POST /HR/Performans/MudirQiymetlendir ────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MudirQiymetlendir(int performansId,
            string? mudirSherhi, string? inkisafPlani,
            List<int> kriteriyaIds, List<decimal> mudirQiymetleri, List<string?> mudirSherhler)
        {
            var performans = await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query()
                .Where(x => x.Id == performansId && !x.Silinib)
                .Include(x => x.Kriteriyalar)
                .FirstOrDefaultAsync();

            if (performans == null)
            {
                TempData["Error"] = "Qiymətləndirmə tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            for (int i = 0; i < kriteriyaIds.Count; i++)
            {
                var krit = performans.Kriteriyalar.FirstOrDefault(k => k.Id == kriteriyaIds[i]);
                if (krit != null)
                {
                    krit.MudirQiymeti = mudirQiymetleri[i];
                    krit.MudirSherhi = mudirSherhler.Count > i ? mudirSherhler[i] : null;
                    await _unitOfWork.Repository<PerformansKriteriya>().YenileAsync(krit);
                }
            }

            decimal toplamCeki = performans.Kriteriyalar.Sum(k => k.Ceki);
            decimal ortaQiymet = toplamCeki > 0
                ? performans.Kriteriyalar
                    .Where(k => k.MudirQiymeti.HasValue)
                    .Sum(k => k.MudirQiymeti!.Value * k.Ceki) / toplamCeki
                : 0;

            performans.MudirOrtalamaQiymet = Math.Round(ortaQiymet, 2);
            performans.MudirSherhi = mudirSherhi;
            if (!string.IsNullOrEmpty(inkisafPlani))
                performans.InkisafPlani = inkisafPlani;
            performans.MudirQiymetlendirmeTarixi = DateTime.Now;
            performans.Status = PerformansStatus.MudirQiymetlendirdi;

            HesablaYekunQiymet(performans);

            // Her iki teref qiymetlendirdi - tamamlandi
            if (performans.IsciOrtalamaQiymet > 0 && performans.MudirOrtalamaQiymet > 0)
                performans.Status = PerformansStatus.Tamamlandi;

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(performans);
            await _unitOfWork.YaddaSaxlaAsync();

            TempData["Success"] = "Müdir qiymətləndirməsi uğurla qeydə alındı.";
            return RedirectToAction(nameof(Detal), new { id = performansId });
        }

        // ── Köməkçilər ──────────────────────────────────────
        private void HesablaYekunQiymet(PerformansQiymetlendirme p)
        {
            if (p.IsciOrtalamaQiymet > 0 && p.MudirOrtalamaQiymet > 0)
                p.YekunQiymet = Math.Round((p.IsciOrtalamaQiymet + p.MudirOrtalamaQiymet) / 2, 2);
            else if (p.IsciOrtalamaQiymet > 0)
                p.YekunQiymet = p.IsciOrtalamaQiymet;
            else if (p.MudirOrtalamaQiymet > 0)
                p.YekunQiymet = p.MudirOrtalamaQiymet;
        }

        private string StatusAdi(PerformansStatus s) => s switch
        {
            PerformansStatus.Gozlemede => "Gözləmədə",
            PerformansStatus.IsciQiymetlendirdi => "İşçi qiymətləndirdi",
            PerformansStatus.MudirQiymetlendirdi => "Müdir qiymətləndirdi",
            PerformansStatus.Tamamlandi => "Tamamlandı",
            _ => "—"
        };

        private async Task FilterSiyahilariniDoldur(int cIl, int? rubu, int? deptId)
        {
            ViewBag.Iller = Enumerable.Range(DateTime.Now.Year - 2, 4)
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), x == cIl))
                .ToList();

            ViewBag.Rublar = Enumerable.Range(1, 4)
                .Select(x => new SelectListItem($"Rüb {x}", x.ToString(), x == rubu))
                .ToList();

            var deptler = await _unitOfWork.Repository<Departament>()
                .Query()
                .Where(x => !x.Silinib)
                .OrderBy(x => x.Ad)
                .ToListAsync();

            ViewBag.Departamentler = deptler
                .Select(x => new SelectListItem(x.Ad, x.Id.ToString(), x.Id == deptId))
                .ToList();
        }

        private async Task YaratmaFormSiyahilariDoldur()
        {
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib)
                .OrderBy(x => x.Soyad)
                .ToListAsync();

            ViewBag.Isciler = isciler
                .Select(x => new SelectListItem($"{x.Soyad} {x.Ad}", x.Id.ToString()))
                .ToList();

            ViewBag.Iller = Enumerable.Range(DateTime.Now.Year - 1, 3)
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), x == DateTime.Now.Year))
                .ToList();
        }
    }
}
