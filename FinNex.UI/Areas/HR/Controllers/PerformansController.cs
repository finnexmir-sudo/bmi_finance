using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
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
        private readonly IBildirisRouter _bildirisRouter;

        public PerformansController(IUnitOfWork unitOfWork, IBildirisRouter bildirisRouter)
        {
            _unitOfWork = unitOfWork;
            _bildirisRouter = bildirisRouter;
        }

        // ── Siyahı ──────────────────────────────────────────────
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
                .Include(x => x.Rehber2)
                .Include(x => x.SobeReisi)
                .Include(x => x.Kriteriyalar)
                .AsQueryable();

            if (rubu.HasValue) query = query.Where(x => x.Rubu == rubu.Value);

            var list = await query.OrderByDescending(x => x.YaradilmaTarixi).ToListAsync();

            if (departamentId.HasValue)
                list = list.Where(x => x.Isci.IsciTeyinatlari.Any(t => t.DepartamentId == departamentId.Value)).ToList();

            return Json(list.Select(x =>
            {
                var teyinat = x.Isci.IsciTeyinatlari.FirstOrDefault();
                return new
                {
                    x.Id,
                    IsciAd = $"{x.Isci.Ad} {x.Isci.Soyad}",
                    Departament = teyinat?.Departament?.Ad ?? "—",
                    Vezife = teyinat?.Vezife?.Ad ?? "—",
                    DovrTipi = x.DovrTipi == PerformansDovrTipi.Rublik ? "Rüblük" : "İllik",
                    x.Il, x.Rubu,
                    Status = (int)x.Status,
                    StatusAd = StatusAdi(x.Status),
                    x.YekunQiymet,
                    x.IsciOrtalamaQiymet,
                    x.SobeReisiOrtalamaQiymet,
                    x.MudirOrtalamaQiymet,
                    x.Rehber2OrtalamaQiymet,
                    KriteriyaSayi = x.Kriteriyalar.Count,
                    SobeReisiAd = x.SobeReisi != null ? $"{x.SobeReisi.Ad} {x.SobeReisi.Soyad}" : null,
                    RehberAd = $"{x.QiymetlendirenIsci.Ad} {x.QiymetlendirenIsci.Soyad}",
                    Rehber2Ad = x.Rehber2 != null ? $"{x.Rehber2.Ad} {x.Rehber2.Soyad}" : null,
                    x.MenecerKriteriyalari
                };
            }));
        }

        // ── Tək Yaratma ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await YaratmaFormSiyahilariDoldur();
            ViewData["Title"] = "Yeni Performans Qiymətləndirmə";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMudir(int isciId)
        {
            var teyinat = await _unitOfWork.Repository<IsciTeyinat>()
                .Query().Where(x => x.IsciId == isciId && x.BitmeTarixi == null && !x.Silinib)
                .FirstOrDefaultAsync();

            if (teyinat == null)
                return Json(new { sobeReisiId = (int?)null, sobeReisiAd = (string?)null, rehberId = (int?)null, rehberAd = (string?)null, isMenecer = false });

            var sobeReisi = await _unitOfWork.Repository<IsciStrukturRolu>()
                .Query().Where(x => x.DepartamentId == teyinat.DepartamentId && x.RolTipi == StrukturRolTipi.SobeReisi && x.Aktivdir && !x.Silinib)
                .Include(x => x.Isci).FirstOrDefaultAsync();

            var rehber = await _unitOfWork.Repository<IsciStrukturRolu>()
                .Query().Where(x => x.DepartamentId == teyinat.DepartamentId && x.RolTipi == StrukturRolTipi.Rehber && x.Aktivdir && !x.Silinib)
                .Include(x => x.Isci).FirstOrDefaultAsync();

            // Bu işçi özü şöbə rəisidirsə menecer modelini aktiv et
            var isciSobeReisiRolu = await _unitOfWork.Repository<IsciStrukturRolu>()
                .Query().Where(x => x.IsciId == isciId && x.RolTipi == StrukturRolTipi.SobeReisi && x.Aktivdir && !x.Silinib)
                .AnyAsync();

            return Json(new
            {
                sobeReisiId = sobeReisi?.IsciId,
                sobeReisiAd = sobeReisi?.Isci?.TamAd,
                rehberId = rehber?.IsciId,
                rehberAd = rehber?.Isci?.TamAd,
                isMenecer = isciSobeReisiRolu
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int isciId, int qiymetlendirenIsciId, int? rehber2Id,
            int? sobeReisiId, int dovrTipi, int il, int? rubu, bool menecerKriteriyalari,
            List<string> kriteriyaAdlari, List<int> kriteriyaCekileri)
        {
            if (isciId <= 0 || qiymetlendirenIsciId <= 0)
            {
                TempData["Error"] = "İşçi və Rəhbər 1 seçilməlidir.";
                await YaratmaFormSiyahilariDoldur();
                return View();
            }

            var performans = new PerformansQiymetlendirme
            {
                IsciId = isciId,
                QiymetlendirenIsciId = qiymetlendirenIsciId,
                Rehber2Id = rehber2Id > 0 ? rehber2Id : null,
                SobeReisiId = sobeReisiId > 0 ? sobeReisiId : null,
                DovrTipi = (PerformansDovrTipi)dovrTipi,
                Il = il,
                Rubu = rubu,
                MenecerKriteriyalari = menecerKriteriyalari,
                Status = PerformansStatus.Gozlemede
            };

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YaratAsync(performans);
            await _unitOfWork.YaddaSaxlaAsync();

            await KriteriyalarYarat(performans.Id, kriteriyaAdlari, kriteriyaCekileri);

            // İşçiyə bildiriş
            var detailUrl = Url.Action("Detal", "Performans", new { area = "User", id = performans.Id });
            await _bildirisRouter.NotifyIsciAsync(isciId, BildirisNovu.PerformansBasladi,
                "Performans qiymətləndirməsi başladı",
                $"{il} ili üzrə performans qiymətləndirməniz başladı. Zəhmət olmasa özünüzü qiymətləndirin.",
                redirectUrl: detailUrl);

            TempData["Success"] = "Performans qiymətləndirmə uğurla yaradıldı.";
            return RedirectToAction(nameof(Detal), new { id = performans.Id });
        }

        // ── Toplu Kampaniya ─────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> BulkKampaniya(int? tipi, int? il, int? dovrTipi, int? rubu)
        {
            if (!tipi.HasValue)
                return View("BulkKampaniyaTipi");

            var kampaniyaTipi = (KampaniyaTipi)tipi.Value;
            ViewBag.KampaniyaTipi = (int)kampaniyaTipi;

            // Dövr seçilməyibsə — dövr seçim səhifəsini göstər
            if (!il.HasValue || !dovrTipi.HasValue)
                return View("BulkKampaniyaDovre");

            var dovrTipiEnum = (PerformansDovrTipi)dovrTipi.Value;
            var rubuDeger = dovrTipiEnum == PerformansDovrTipi.Rublik ? (rubu ?? 1) : (int?)null;

            // Artıq bu dövr üçün qiymətləndirilmiş işçilər
            var mevcudQuery = _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query().Where(x => !x.Silinib && x.Il == il && x.KampaniyaTipi == kampaniyaTipi);
            if (dovrTipiEnum == PerformansDovrTipi.Rublik && rubuDeger.HasValue)
                mevcudQuery = mevcudQuery.Where(x => x.Rubu == rubuDeger);
            var mevcudIsciIds = (await mevcudQuery.Select(x => x.IsciId).ToListAsync()).ToHashSet();

            var strukturRollar = await _unitOfWork.Repository<IsciStrukturRolu>()
                .Query().Where(x => x.Aktivdir && !x.Silinib)
                .Include(x => x.Isci)
                .ToListAsync();

            // Yalnız Rəhbər rolundakılar (Rəhbər 1 / Rəhbər 2 üçün)
            var rehberler = strukturRollar
                .Where(r => r.RolTipi == StrukturRolTipi.Rehber)
                .Select(r => new SelectListItem($"{r.Isci.Soyad} {r.Isci.Ad}", r.IsciId.ToString()))
                .DistinctBy(x => x.Value)
                .ToList();
            ViewBag.Rehberler = rehberler;

            // Şöbə Rəisi siyahısı (inline seçim üçün)
            var sobeReisiRollar = strukturRollar
                .Where(r => r.RolTipi == StrukturRolTipi.SobeReisi)
                .ToList();
            var sobeReisiIds = sobeReisiRollar.Select(r => r.IsciId).ToHashSet();
            ViewBag.SobeReisleri = sobeReisiRollar
                .Select(r => new SelectListItem($"{r.Isci.Soyad} {r.Isci.Ad}", r.IsciId.ToString()))
                .DistinctBy(x => x.Value)
                .ToList();

            var rehberIds = strukturRollar
                .Where(r => r.RolTipi == StrukturRolTipi.Rehber)
                .Select(r => r.IsciId).ToHashSet();

            // İşçi siyahısı — tipə + mövcud qiymətləndirməyə görə filtrlə
            IEnumerable<Isci> isciler;
            if (kampaniyaTipi == KampaniyaTipi.SobeReisiQiymetlendirme)
            {
                isciler = await _unitOfWork.Repository<Isci>()
                    .Query().Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib
                        && sobeReisiIds.Contains(x.Id)
                        && !mevcudIsciIds.Contains(x.Id))
                    .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null)).ThenInclude(t => t.Departament)
                    .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null)).ThenInclude(t => t.Vezife)
                    .OrderBy(x => x.Soyad).ThenBy(x => x.Ad)
                    .ToListAsync();
            }
            else
            {
                var xaricEdilecekIds = sobeReisiIds.Union(rehberIds).Union(mevcudIsciIds).ToHashSet();
                isciler = await _unitOfWork.Repository<Isci>()
                    .Query().Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib
                        && !xaricEdilecekIds.Contains(x.Id))
                    .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null)).ThenInclude(t => t.Departament)
                    .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null)).ThenInclude(t => t.Vezife)
                    .OrderBy(x => x.Soyad).ThenBy(x => x.Ad)
                    .ToListAsync();
            }

            var isciVmList = isciler.Select(i =>
            {
                var teyinat = i.IsciTeyinatlari.FirstOrDefault();
                var deptId  = teyinat?.DepartamentId;
                var sobeR   = deptId.HasValue
                    ? sobeReisiRollar.FirstOrDefault(r => r.DepartamentId == deptId && r.IsciId != i.Id)
                    : null;
                return new
                {
                    i.Id, i.Ad, i.Soyad,
                    Departament  = teyinat?.Departament?.Ad ?? "—",
                    Vezife       = teyinat?.Vezife?.Ad ?? "—",
                    SobeReisiVar = sobeR != null,
                    SobeReisiId  = sobeR?.IsciId,
                    SobeReisiAd  = sobeR?.Isci?.TamAd
                };
            }).ToList();

            ViewBag.IsciVmList  = isciVmList;
            ViewBag.DovrTipi    = (int)dovrTipiEnum;
            ViewBag.SecilmisIl  = il.Value;
            ViewBag.SecilmisRubu = rubuDeger;
            ViewData["Title"] = kampaniyaTipi == KampaniyaTipi.SobeReisiQiymetlendirme
                ? "Şöbə Rəisi Qiymətləndirməsi Kampaniyası"
                : "İşçi Qiymətləndirməsi Kampaniyası";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkKampaniyaBaslat(
            List<int> secilmisIsciIds, int qiymetlendirenIsciId, int? rehber2Id,
            int dovrTipi, int il, int? rubu, int kampaniyaTipi)
        {
            if (!secilmisIsciIds.Any())
            { TempData["Error"] = "Ən azı bir işçi seçilməlidir."; return RedirectToAction(nameof(BulkKampaniya), new { tipi = kampaniyaTipi, il, dovrTipi, rubu }); }
            if (qiymetlendirenIsciId <= 0)
            { TempData["Error"] = "Rəhbər seçilməlidir."; return RedirectToAction(nameof(BulkKampaniya), new { tipi = kampaniyaTipi, il, dovrTipi, rubu }); }

            var tipi = (KampaniyaTipi)kampaniyaTipi;

            // İnline şöbə rəisi override-ları (sobe_ISCIID = sobeReisiId)
            var inlineSobe = new Dictionary<int, int>();
            foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("sobe_")))
            {
                if (int.TryParse(key[5..], out int iId) &&
                    int.TryParse(Request.Form[key], out int sId) && sId > 0)
                    inlineSobe[iId] = sId;
            }

            // DB-dən kriteriya şablonlarını yüklə
            var sablonlar = await _unitOfWork.Repository<PerformansKriteriyaSablon>()
                .Query().Where(x => x.KampaniyaTipi == tipi && x.Aktiv)
                .OrderBy(x => x.Sira).ToListAsync();

            var strukturRollar = await _unitOfWork.Repository<IsciStrukturRolu>()
                .Query().Where(x => x.Aktivdir && !x.Silinib).ToListAsync();

            var isciler = await _unitOfWork.Repository<Isci>()
                .Query().Where(x => secilmisIsciIds.Contains(x.Id) && !x.Silinib)
                .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                .ToListAsync();

            int yaradildi = 0;
            var listUrl = Url.Action("Index", "Performans", new { area = "User" });
            var xetalar = new List<string>();

            foreach (var isci in isciler)
            {
                var teyinat = isci.IsciTeyinatlari.FirstOrDefault();
                var deptId  = teyinat?.DepartamentId;

                // Şöbə rəisi qiymətləndirməsindədir — SobeReisiId yoxdur
                int? sobeReisiId = null;
                if (tipi == KampaniyaTipi.IsciQiymetlendirme)
                {
                    if (inlineSobe.TryGetValue(isci.Id, out int overrideSobe))
                        sobeReisiId = overrideSobe;
                    else if (deptId.HasValue)
                    {
                        var sr = strukturRollar.FirstOrDefault(r =>
                            r.DepartamentId == deptId && r.RolTipi == StrukturRolTipi.SobeReisi && r.IsciId != isci.Id);
                        sobeReisiId = sr?.IsciId;
                    }

                    // Validation: işçi qiymətləndirməsində şöbə rəisi məcburidir
                    if (sobeReisiId == null)
                    {
                        xetalar.Add($"{isci.Soyad} {isci.Ad} üçün şöbə rəisi seçilməyib.");
                        continue;
                    }
                }

                var performans = new PerformansQiymetlendirme
                {
                    IsciId = isci.Id,
                    QiymetlendirenIsciId = qiymetlendirenIsciId,
                    Rehber2Id  = rehber2Id > 0 ? rehber2Id : null,
                    SobeReisiId = sobeReisiId,
                    DovrTipi   = (PerformansDovrTipi)dovrTipi,
                    Il = il, Rubu = rubu,
                    KampaniyaTipi      = tipi,
                    MenecerKriteriyalari = tipi == KampaniyaTipi.SobeReisiQiymetlendirme,
                    Status = PerformansStatus.Gozlemede
                };

                await _unitOfWork.Repository<PerformansQiymetlendirme>().YaratAsync(performans);
                await _unitOfWork.YaddaSaxlaAsync();

                foreach (var s in sablonlar)
                    await _unitOfWork.Repository<PerformansKriteriya>().YaratAsync(
                        new PerformansKriteriya { PerformansId = performans.Id, KriteriyaAdi = s.Ad, Ceki = s.Ceki });

                await _unitOfWork.YaddaSaxlaAsync();

                await _bildirisRouter.NotifyIsciAsync(isci.Id, BildirisNovu.PerformansBasladi,
                    "Performans qiymətləndirməsi başladı",
                    $"{il} ili üzrə performans qiymətləndirməniz başladı.",
                    redirectUrl: listUrl);

                yaradildi++;
            }

            if (xetalar.Any())
                TempData["Warning"] = string.Join(" | ", xetalar);

            if (yaradildi == 0)
            {
                TempData["Error"] = "Heç bir qiymətləndirmə yaradılmadı. Bütün seçilmiş işçilər üçün şöbə rəisi seçilməlidir.";
                return RedirectToAction(nameof(BulkKampaniya), new { tipi = kampaniyaTipi, il, dovrTipi, rubu });
            }

            TempData["Success"] = $"{yaradildi} işçi üçün performans kampaniyası uğurla başladıldı.";
            return RedirectToAction(nameof(Index));
        }

        // ── Detal ────────────────────────────────────────────────
        public async Task<IActionResult> Detal(int id)
        {
            var performans = await LoadPerformans(id);
            if (performans == null) { TempData["Error"] = "Tapılmadı."; return RedirectToAction(nameof(Index)); }
            ViewData["Title"] = $"Performans — {performans.Isci.Ad} {performans.Isci.Soyad}";
            return View(performans);
        }

        // ── İşçi Qiymətləndirmə ─────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> IsciQiymetlendir(int performansId,
            string? isciSherhi, string? inkisafPlani,
            List<int> kriteriyaIds, List<decimal> isciQiymetleri, List<string?> isciSherhler)
        {
            var p = await LoadWithKriteriyalar(performansId);
            if (p == null) { TempData["Error"] = "Tapılmadı."; return RedirectToAction(nameof(Index)); }
            if (p.Status != PerformansStatus.Gozlemede) { TempData["Error"] = "Bu mərhələ artıq tamamlanıb."; return RedirectToAction(nameof(Detal), new { id = performansId }); }

            await KriteriyaQiymetYenile(p.Kriteriyalar, kriteriyaIds, isciQiymetleri, isciSherhler, "isci");

            decimal toplamCeki = p.Kriteriyalar.Sum(k => k.Ceki);
            p.IsciOrtalamaQiymet = toplamCeki > 0
                ? Math.Round(p.Kriteriyalar.Where(k => k.IsciQiymeti.HasValue).Sum(k => k.IsciQiymeti!.Value * k.Ceki) / toplamCeki, 2) : 0;
            p.IsciSherhi = isciSherhi;
            p.InkisafPlani = inkisafPlani;
            p.IsciQiymetlendirmeTarixi = DateTime.Now;
            p.Status = PerformansStatus.SobeReisiGozleyir;
            HesablaYekunQiymet(p);

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(p);
            await _unitOfWork.YaddaSaxlaAsync();

            // Şöbə rəisinə bildiriş
            if (p.SobeReisiId.HasValue)
            {
                var url = Url.Action("Detal", "Performans", new { area = "User", id = p.Id });
                await _bildirisRouter.NotifyIsciAsync(p.SobeReisiId.Value, BildirisNovu.PerformansNovbeSobe,
                    "Performans: Növbəniz",
                    $"{p.Isci.Ad} {p.Isci.Soyad} özünü qiymətləndirdi. Şöbə rəisi qiymətləndirməsi sizdən gözlənilir.",
                    redirectUrl: url);
            }
            else
            {
                // Şöbə rəisi yoxdursa bilavasitə rəhbərə bildiriş
                var url = Url.Action("Detal", "Performans", new { area = "User", id = p.Id });
                await _bildirisRouter.NotifyIsciAsync(p.QiymetlendirenIsciId, BildirisNovu.PerformansNovbeRehber,
                    "Performans: Növbəniz",
                    $"{p.Isci.Ad} {p.Isci.Soyad} özünü qiymətləndirdi. Rəhbər qiymətləndirməsi sizdən gözlənilir.",
                    redirectUrl: url);
            }

            TempData["Success"] = "İşçi qiymətləndirməsi qeydə alındı.";
            return RedirectToAction(nameof(Detal), new { id = performansId });
        }

        // ── Şöbə Rəisi Qiymətləndirmə ───────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SobeReisiQiymetlendir(int performansId,
            string? sobeReisiSherhi,
            List<int> kriteriyaIds, List<decimal> sobeReisiQiymetleri, List<string?> sobeReisiSherhler)
        {
            var p = await LoadWithKriteriyalar(performansId);
            if (p == null) { TempData["Error"] = "Tapılmadı."; return RedirectToAction(nameof(Index)); }
            if (p.Status != PerformansStatus.SobeReisiGozleyir) { TempData["Error"] = "Bu mərhələ mövcud deyil."; return RedirectToAction(nameof(Detal), new { id = performansId }); }

            await KriteriyaQiymetYenile(p.Kriteriyalar, kriteriyaIds, sobeReisiQiymetleri, sobeReisiSherhler, "sobe");

            decimal toplamCeki = p.Kriteriyalar.Sum(k => k.Ceki);
            p.SobeReisiOrtalamaQiymet = toplamCeki > 0
                ? Math.Round(p.Kriteriyalar.Where(k => k.SobeReisiQiymeti.HasValue).Sum(k => k.SobeReisiQiymeti!.Value * k.Ceki) / toplamCeki, 2) : 0;
            p.SobeReisiSherhi = sobeReisiSherhi;
            p.SobeReisiQiymetlendirmeTarixi = DateTime.Now;
            p.Status = PerformansStatus.RehberGozleyir;
            HesablaYekunQiymet(p);

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(p);
            await _unitOfWork.YaddaSaxlaAsync();

            var url = Url.Action("Detal", "Performans", new { area = "User", id = p.Id });
            await _bildirisRouter.NotifyIsciAsync(p.QiymetlendirenIsciId, BildirisNovu.PerformansNovbeRehber,
                "Performans: Növbəniz",
                $"{p.Isci.Ad} {p.Isci.Soyad} üçün şöbə rəisi qiymətləndirməsi tamamlandı. Rəhbər qiymətləndirməsi sizdən gözlənilir.",
                redirectUrl: url);

            TempData["Success"] = "Şöbə rəisi qiymətləndirməsi qeydə alındı.";
            return RedirectToAction(nameof(Detal), new { id = performansId });
        }

        // ── Rəhbər 1 Qiymətləndirmə ─────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RehberQiymetlendir(int performansId,
            string? mudirSherhi, string? inkisafPlani,
            List<int> kriteriyaIds, List<decimal> mudirQiymetleri, List<string?> mudirSherhler)
        {
            var p = await LoadWithKriteriyalar(performansId);
            if (p == null) { TempData["Error"] = "Tapılmadı."; return RedirectToAction(nameof(Index)); }

            var validStatuses = new[] { PerformansStatus.SobeReisiGozleyir, PerformansStatus.RehberGozleyir };
            if (!validStatuses.Contains(p.Status)) { TempData["Error"] = "Bu mərhələ mövcud deyil."; return RedirectToAction(nameof(Detal), new { id = performansId }); }

            await KriteriyaQiymetYenile(p.Kriteriyalar, kriteriyaIds, mudirQiymetleri, mudirSherhler, "rehber1");

            decimal toplamCeki = p.Kriteriyalar.Sum(k => k.Ceki);
            p.MudirOrtalamaQiymet = toplamCeki > 0
                ? Math.Round(p.Kriteriyalar.Where(k => k.MudirQiymeti.HasValue).Sum(k => k.MudirQiymeti!.Value * k.Ceki) / toplamCeki, 2) : 0;
            p.MudirSherhi = mudirSherhi;
            if (!string.IsNullOrEmpty(inkisafPlani)) p.InkisafPlani = inkisafPlani;
            p.MudirQiymetlendirmeTarixi = DateTime.Now;

            if (p.Rehber2Id.HasValue)
            {
                // Rehber2 var — onu gözlə
                p.Status = PerformansStatus.Rehber2Gozleyir;
                HesablaYekunQiymet(p);
                await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(p);
                await _unitOfWork.YaddaSaxlaAsync();

                var url = Url.Action("Detal", "Performans", new { area = "User", id = p.Id });
                await _bildirisRouter.NotifyIsciAsync(p.Rehber2Id.Value, BildirisNovu.PerformansNovbeRehber,
                    "Performans: Yekun Qiymətləndirmə",
                    $"{p.Isci.Ad} {p.Isci.Soyad} üçün yekun qiymətləndirmə sizdən gözlənilir.",
                    redirectUrl: url);

                TempData["Success"] = "Rəhbər 1 qiymətləndirməsi qeydə alındı. Rəhbər 2 gözlənilir.";
            }
            else
            {
                // Rehber2 yoxdur — tamamla
                p.Status = PerformansStatus.Tamamlandi;
                HesablaYekunQiymet(p);
                await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(p);
                await _unitOfWork.YaddaSaxlaAsync();

                var url = Url.Action("Detal", "Performans", new { area = "User", id = p.Id });
                await _bildirisRouter.NotifyIsciAsync(p.IsciId, BildirisNovu.PerformansTamamlandi,
                    "Performans qiymətləndirməniz tamamlandı",
                    $"Yekun qiymətiniz: {p.YekunQiymet:N2} / 5",
                    redirectUrl: url);

                TempData["Success"] = "Rəhbər qiymətləndirməsi qeydə alındı. Proses tamamlandı.";
            }

            return RedirectToAction(nameof(Detal), new { id = performansId });
        }

        // ── Rəhbər 2 Qiymətləndirmə ─────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Rehber2Qiymetlendir(int performansId,
            string? rehber2Sherhi, string? inkisafPlani,
            List<int> kriteriyaIds, List<decimal> rehber2Qiymetleri, List<string?> rehber2Sherhler)
        {
            var p = await LoadWithKriteriyalar(performansId);
            if (p == null) { TempData["Error"] = "Tapılmadı."; return RedirectToAction(nameof(Index)); }
            if (p.Status != PerformansStatus.Rehber2Gozleyir) { TempData["Error"] = "Bu mərhələ mövcud deyil."; return RedirectToAction(nameof(Detal), new { id = performansId }); }

            await KriteriyaQiymetYenile(p.Kriteriyalar, kriteriyaIds, rehber2Qiymetleri, rehber2Sherhler, "rehber2");

            decimal toplamCeki = p.Kriteriyalar.Sum(k => k.Ceki);
            p.Rehber2OrtalamaQiymet = toplamCeki > 0
                ? Math.Round(p.Kriteriyalar.Where(k => k.Rehber2Qiymeti.HasValue).Sum(k => k.Rehber2Qiymeti!.Value * k.Ceki) / toplamCeki, 2) : 0;
            p.Rehber2Sherhi = rehber2Sherhi;
            if (!string.IsNullOrEmpty(inkisafPlani)) p.InkisafPlani = inkisafPlani;
            p.Rehber2QiymetlendirmeTarixi = DateTime.Now;
            p.Status = PerformansStatus.Tamamlandi;
            HesablaYekunQiymet(p);

            await _unitOfWork.Repository<PerformansQiymetlendirme>().YenileAsync(p);
            await _unitOfWork.YaddaSaxlaAsync();

            var detailUrl = Url.Action("Detal", "Performans", new { area = "User", id = p.Id });
            await _bildirisRouter.NotifyIsciAsync(p.IsciId, BildirisNovu.PerformansTamamlandi,
                "Performans qiymətləndirməniz tamamlandı",
                $"Yekun qiymətiniz: {p.YekunQiymet:N2} / 5",
                redirectUrl: detailUrl);

            TempData["Success"] = "Yekun qiymətləndirmə qeydə alındı. Proses tamamlandı.";
            return RedirectToAction(nameof(Detal), new { id = performansId });
        }

        // ── Köməkçi metodlar ─────────────────────────────────────
        private void HesablaYekunQiymet(PerformansQiymetlendirme p)
        {
            bool hasSobe = p.SobeReisiId.HasValue && p.SobeReisiOrtalamaQiymet > 0;
            bool hasRehber2 = p.Rehber2Id.HasValue && p.Rehber2OrtalamaQiymet > 0;
            bool hasRehber1 = p.MudirOrtalamaQiymet > 0;
            bool hasIsci = p.IsciOrtalamaQiymet > 0;

            if (!hasIsci && !hasRehber1) return;

            decimal sum = 0; int count = 0;
            if (hasIsci) { sum += p.IsciOrtalamaQiymet; count++; }
            if (hasSobe) { sum += p.SobeReisiOrtalamaQiymet; count++; }
            if (hasRehber1) { sum += p.MudirOrtalamaQiymet; count++; }
            if (hasRehber2) { sum += p.Rehber2OrtalamaQiymet; count++; }

            p.YekunQiymet = count > 0 ? Math.Round(sum / count, 2) : 0;
        }

        private async Task KriteriyaQiymetYenile(ICollection<PerformansKriteriya> kriteriyalar,
            List<int> ids, List<decimal> qiymetler, List<string?> sherhler, string tip)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                var krit = kriteriyalar.FirstOrDefault(k => k.Id == ids[i]);
                if (krit == null) continue;
                string? sherh = sherhler.Count > i ? sherhler[i] : null;
                switch (tip)
                {
                    case "isci": krit.IsciQiymeti = qiymetler[i]; krit.IsciSherhi = sherh; break;
                    case "sobe": krit.SobeReisiQiymeti = qiymetler[i]; krit.SobeReisiSherhi = sherh; break;
                    case "rehber1": krit.MudirQiymeti = qiymetler[i]; krit.MudirSherhi = sherh; break;
                    case "rehber2": krit.Rehber2Qiymeti = qiymetler[i]; krit.Rehber2Sherhi = sherh; break;
                }
                await _unitOfWork.Repository<PerformansKriteriya>().YenileAsync(krit);
            }
        }

        private async Task KriteriyalarYarat(int performansId, List<string> adlar, List<int> cekiler)
        {
            for (int i = 0; i < adlar.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(adlar[i])) continue;
                await _unitOfWork.Repository<PerformansKriteriya>().YaratAsync(new PerformansKriteriya
                {
                    PerformansId = performansId,
                    KriteriyaAdi = adlar[i].Trim(),
                    Ceki = i < cekiler.Count ? cekiler[i] : 0
                });
            }
            await _unitOfWork.YaddaSaxlaAsync();
        }

        private static List<(string, int)> StandartKriteriyalar() => new()
        {
            ("İş keyfiyyəti", 30), ("Vaxtında icra", 20), ("Komanda işi", 20),
            ("Təşəbbüskarlıq", 15), ("Peşəkar inkişaf", 15)
        };

        private static List<(string, int)> MenecerKriteriyalari() => new()
        {
            ("Liderlik", 30), ("Resurs idarəetməsi", 25), ("Komanda inkişafı", 20),
            ("Strateji düşüncə", 15), ("Məsuliyyət", 10)
        };

        private string StatusAdi(PerformansStatus s) => s switch
        {
            PerformansStatus.Gozlemede => "Gözləmədə",
            PerformansStatus.SobeReisiGozleyir => "Şöbə rəisi gözlənilir",
            PerformansStatus.RehberGozleyir => "Rəhbər gözlənilir",
            PerformansStatus.Rehber2Gozleyir => "Rəhbər 2 gözlənilir",
            PerformansStatus.Tamamlandi => "Tamamlandı",
            _ => "—"
        };

        private async Task<PerformansQiymetlendirme?> LoadPerformans(int id)
        {
            return await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query().Where(x => x.Id == id && !x.Silinib)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Departament)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Vezife)
                .Include(x => x.QiymetlendirenIsci)
                .Include(x => x.Rehber2)
                .Include(x => x.SobeReisi)
                .Include(x => x.Kriteriyalar)
                .FirstOrDefaultAsync();
        }

        private async Task<PerformansQiymetlendirme?> LoadWithKriteriyalar(int id)
        {
            return await _unitOfWork.Repository<PerformansQiymetlendirme>()
                .Query().Where(x => x.Id == id && !x.Silinib)
                .Include(x => x.Isci)
                .Include(x => x.Kriteriyalar)
                .FirstOrDefaultAsync();
        }

        private async Task FilterSiyahilariniDoldur(int cIl, int? rubu, int? deptId)
        {
            ViewBag.Iller = Enumerable.Range(DateTime.Now.Year - 2, 4)
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), x == cIl)).ToList();
            ViewBag.Rublar = Enumerable.Range(1, 4)
                .Select(x => new SelectListItem($"Rüb {x}", x.ToString(), x == rubu)).ToList();
            var deptler = await _unitOfWork.Repository<Departament>()
                .Query().Where(x => !x.Silinib).OrderBy(x => x.Ad).ToListAsync();
            ViewBag.Departamentler = deptler
                .Select(x => new SelectListItem(x.Ad, x.Id.ToString(), x.Id == deptId)).ToList();
        }

        // ── Kriteriya İdarəetməsi ────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Kriteriyalar()
        {
            var sablonlar = await _unitOfWork.Repository<PerformansKriteriyaSablon>()
                .Query().Where(x => !x.Silinib).OrderBy(x => x.KampaniyaTipi).ThenBy(x => x.Sira)
                .ToListAsync();
            ViewData["Title"] = "Performans Kriteriyaları";
            return View(sablonlar);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> KriteriyaYarat(string ad, decimal ceki, int kampaniyaTipi)
        {
            if (string.IsNullOrWhiteSpace(ad) || ceki <= 0)
            { TempData["Error"] = "Ad və çəki daxil edilməlidir."; return RedirectToAction(nameof(Kriteriyalar)); }

            var tipi = (KampaniyaTipi)kampaniyaTipi;
            var sonSira = (await _unitOfWork.Repository<PerformansKriteriyaSablon>()
                .Query().Where(x => x.KampaniyaTipi == tipi && !x.Silinib)
                .OrderByDescending(x => x.Sira).FirstOrDefaultAsync())?.Sira ?? 0;

            await _unitOfWork.Repository<PerformansKriteriyaSablon>().YaratAsync(new PerformansKriteriyaSablon
            {
                Ad = ad.Trim(), Ceki = ceki, KampaniyaTipi = tipi, Aktiv = true, Sira = sonSira + 1
            });
            await _unitOfWork.YaddaSaxlaAsync();
            TempData["Success"] = "Kriteriya əlavə edildi.";
            return RedirectToAction(nameof(Kriteriyalar));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> KriteriyaYenile(int id, string ad, decimal ceki, bool aktiv)
        {
            var s = await _unitOfWork.Repository<PerformansKriteriyaSablon>()
                .Query().FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
            if (s == null) { TempData["Error"] = "Tapılmadı."; return RedirectToAction(nameof(Kriteriyalar)); }

            s.Ad = ad.Trim(); s.Ceki = ceki; s.Aktiv = aktiv;
            await _unitOfWork.Repository<PerformansKriteriyaSablon>().YenileAsync(s);
            await _unitOfWork.YaddaSaxlaAsync();
            TempData["Success"] = "Kriteriya yeniləndi.";
            return RedirectToAction(nameof(Kriteriyalar));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> KriteriyaSil(int id)
        {
            var s = await _unitOfWork.Repository<PerformansKriteriyaSablon>()
                .Query().FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
            if (s != null) { s.Silinib = true; await _unitOfWork.Repository<PerformansKriteriyaSablon>().YenileAsync(s); await _unitOfWork.YaddaSaxlaAsync(); }
            TempData["Success"] = "Kriteriya silindi.";
            return RedirectToAction(nameof(Kriteriyalar));
        }

        private async Task YaratmaFormSiyahilariDoldur()
        {
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query().Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib)
                .OrderBy(x => x.Soyad).ToListAsync();
            ViewBag.Isciler = isciler
                .Select(x => new SelectListItem($"{x.Soyad} {x.Ad}", x.Id.ToString())).ToList();
            ViewBag.Iller = Enumerable.Range(DateTime.Now.Year - 1, 3)
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), x == DateTime.Now.Year)).ToList();
        }
    }
}
