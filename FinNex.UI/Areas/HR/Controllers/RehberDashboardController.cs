using ClosedXML.Excel;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using FinNex.Application.DTOs.HR.Icaze;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Application.Services.HR;
using FinNex.Application.Interfaces.Structur;
using FinNex.UI.Areas.User.ViewModels.Icaze;
using FinNex.UI.Areas.HR.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.Rehber + "," + RoleNames.Admin)]
    public class RehberDashboardController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IDavamiyyetService _davamiyyetService;
        private readonly IMezuniyyetService _mezuniyyetService;
        private readonly IIcazeService _icazeService;
        private readonly IIsciService _isciService;
        private readonly IDepartmentService _departamentService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<RehberDashboardController> _logger;

        public RehberDashboardController(
            IUnitOfWork uow,
            IDavamiyyetService davamiyyetService,
            IMezuniyyetService mezuniyyetService,
            IIcazeService icazeService,
            IIsciService isciService,
            IDepartmentService departamentService,
            UserManager<AppUser> userManager,
            ILogger<RehberDashboardController> logger)
        {
            _uow = uow;
            _davamiyyetService = davamiyyetService;
            _mezuniyyetService = mezuniyyetService;
            _icazeService = icazeService;
            _isciService = isciService;
            _departamentService = departamentService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
          try
          {
            var bugun = DateTime.Today;
            var vm = new RehberDashboardVM();

            // ═══════════════════════════════════════════════════
            // 1. İŞÇİLƏR — Ümumi Statistika
            // ═══════════════════════════════════════════════════
            var isciler = await _uow.Repository<Isci>()
                .Query()
                .Where(x => !x.Silinib)
                .Select(x => new
                {
                    x.Id,
                    x.Ad,
                    x.Soyad,
                    x.Cins,
                    x.Status,
                    x.DogumTarixi,
                    x.IsheQebulTarixi,
                    x.IsdenAyrilmaTarixi
                })
                .ToListAsync();

            vm.UmumiIsciSayi = isciler.Count;
            vm.AktivIsciSayi = isciler.Count(x => x.Status == IsciStatus.Aktiv);
            vm.MezuniyyetdeIsciSayi = isciler.Count(x => x.Status == IsciStatus.Mezuniyyetde);
            vm.IshdenCixanSayi = isciler.Count(x => x.Status == IsciStatus.IshtenCixib);

            vm.KisiSayi = isciler.Count(x => x.Cins == Cins.Kisi);
            vm.QadinSayi = isciler.Count(x => x.Cins == Cins.Qadin);

            vm.BuAyIsheQebulSayi = isciler.Count(x =>
                x.IsheQebulTarixi.Year == bugun.Year && x.IsheQebulTarixi.Month == bugun.Month);
            vm.BuAyIshdenAyrilanSayi = isciler.Count(x =>
                x.IsdenAyrilmaTarixi.HasValue &&
                x.IsdenAyrilmaTarixi.Value.Year == bugun.Year &&
                x.IsdenAyrilmaTarixi.Value.Month == bugun.Month);

            // ── Yaş qrupları ──
            foreach (var isci in isciler.Where(x => x.Status == IsciStatus.Aktiv))
            {
                var yas = bugun.Year - isci.DogumTarixi.Year;
                if (isci.DogumTarixi.Date > bugun.AddYears(-yas)) yas--;

                if (yas < 30) vm.Yas20_30++;
                else if (yas < 40) vm.Yas30_40++;
                else if (yas < 50) vm.Yas40_50++;
                else vm.Yas50Plus++;

                // Staj
                var staj = bugun.Year - isci.IsheQebulTarixi.Year;
                if (isci.IsheQebulTarixi.Date > bugun.AddYears(-staj)) staj--;

                if (staj < 1) vm.Staj0_1++;
                else if (staj < 3) vm.Staj1_3++;
                else if (staj < 5) vm.Staj3_5++;
                else vm.Staj5Plus++;
            }

            // ═══════════════════════════════════════════════════
            // 2. DAVAMIYYƏT — Son girişlər (qısa siyahı)
            // Tam davamiyyət hesabatı HR/Davamiyyet səhifəsindədir.
            // ═══════════════════════════════════════════════════
            var bugunkuDav = await _davamiyyetService.TarixUzreAsync(bugun);
            var davlar = bugunkuDav?.ToList() ?? new();

            vm.SonGirisler = davlar
                .Where(x => x.GirisVaxti != null)
                .OrderByDescending(x => x.GirisVaxti)
                .Take(10)
                .Select(x => new SonGirisDto
                {
                    Ad = x.IsciTamAd ?? "—",
                    Departament = x.DepartamentAd ?? "—",
                    GirisVaxti = x.GirisVaxti?.ToString("HH:mm"),
                    CixisVaxti = x.CixisVaxti?.ToString("HH:mm"),
                    Status = (int)x.Status
                })
                .ToList();

            // ═══════════════════════════════════════════════════
            // 3. MƏZUNİYYƏT & İCAZƏ
            // ═══════════════════════════════════════════════════
            // Mezuniyyet siyahısını direct sorğu ilə alırıq — Departament/Vezife
            // Include-ları açıq olsun deyə (servisin GetListAsync-i bunları
            // yükləmir, ona görə dashboard-da ad/şöbə "—" çıxırdı).
            var mezEntities = await _uow.Repository<Mezuniyyet>()
                .Query()
                .Where(x => !x.Silinib)
                .Include(m => m.Isci).ThenInclude(i => i.IsciTeyinatlari).ThenInclude(t => t.Departament)
                .ToListAsync();

            string SobeAd(Mezuniyyet m) => m.Isci?.IsciTeyinatlari?
                .Where(t => t.Aktivdir && !t.Silinib)
                .Select(t => t.Departament?.Ad)
                .FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? "—";

            string NovText(MezuniyyetNovu n) => n switch
            {
                MezuniyyetNovu.Illik => "Əmək məzuniyyəti",
                MezuniyyetNovu.Xestelik => "Xəstəlik məzuniyyəti",
                MezuniyyetNovu.Ezamiyyet => "Ezamiyyət",
                _ => n.ToString()
            };

            var mezler = mezEntities.Select(m => new
            {
                m.Id,
                m.Status,
                m.Nov,
                m.BaslamaTarixi,
                m.BitmeTarixi,
                m.IsGunlerininSayi,
                IsciAdSoyad = m.Isci?.TamAd ?? "—",
                SobeAdi = SobeAd(m),
                NovText = NovText(m.Nov)
            }).ToList();

            // Bu gün məzuniyyətdə olanlar
            var hazirdaMez = mezler.Where(x =>
                x.Status == MezuniyyetStatus.Tesdiqlenib &&
                x.BaslamaTarixi.Date <= bugun &&
                x.BitmeTarixi.Date >= bugun).ToList();

            vm.HazirdaMezuniyyetde = hazirdaMez.Count;

            vm.MezuniyyetdeOlanlar = hazirdaMez
                .Take(10)
                .Select(x => new MezuniyyetdeOlanDto
                {
                    AdSoyad = x.IsciAdSoyad ?? "—",
                    Departament = x.SobeAdi ?? "—",
                    Nov = x.NovText ?? "—",
                    BitmeTarixi = x.BitmeTarixi
                })
                .ToList();

            // Hal-hazırda məzuniyyətdə olanlar içindən bu həftə qayıdacaqlar
            vm.BuHefteBitenMezuniyyet = hazirdaMez.Count(x =>
                x.BitmeTarixi.Date <= bugun.AddDays(7));

            // Rəhbər təsdiqi gözləyən məzuniyyətlər
            var rehberGozleyen = mezler
                .Where(x => x.Status == MezuniyyetStatus.RehberTesdiqinde)
                .ToList();
            vm.GozleyenMezuniyyetTesdiqi = rehberGozleyen.Count;
            vm.GozleyenMezuniyyetler = rehberGozleyen
                .Take(10)
                .Select(x => new GozleyenTesdiqDto
                {
                    Id = x.Id,
                    AdSoyad = x.IsciAdSoyad ?? "—",
                    Departament = x.SobeAdi ?? "—",
                    Tarix = $"{x.BaslamaTarixi:dd.MM} - {x.BitmeTarixi:dd.MM.yyyy}"
                })
                .ToList();

            // Məzuniyyət növləri
            var aktivMez = mezler.Where(x => x.Status == MezuniyyetStatus.Tesdiqlenib);
            vm.IllikMezuniyyetSayi = aktivMez.Count(x => x.Nov == MezuniyyetNovu.Illik);
            vm.XestelikMezuniyyetSayi = aktivMez.Count(x => x.Nov == MezuniyyetNovu.Xestelik);
            vm.EzamiyyetSayi = aktivMez.Count(x => x.Nov == MezuniyyetNovu.Ezamiyyet);

            // İcazə — Rəhbər təsdiqi gözləyən
            var icazeResult = await _icazeService.GetRehberTesdiqindeAsync();
            if (icazeResult.Success && icazeResult.Data != null)
            {
                var icazeler = icazeResult.Data.ToList();
                vm.GozleyenIcazeTesdiqi = icazeler.Count;
                vm.GozleyenIcazeler = icazeler
                    .Take(10)
                    .Select(x => new GozleyenTesdiqDto
                    {
                        Id = x.Id,
                        AdSoyad = x.IsciAdSoyad ?? "—",
                        Departament = x.SobeAdi ?? "—",
                        Tarix = $"{x.IcazeTarixi:dd.MM.yyyy} ({x.BaslamaSaati:hh\\:mm}-{x.BitisSaati:hh\\:mm})"
                    })
                    .ToList();
            }

            // ═══════════════════════════════════════════════════
            // 4. DEPARTAMENT ANALİTİKASI
            // ═══════════════════════════════════════════════════
            var teyinatlar = await _uow.Repository<IsciTeyinat>()
                .Query()
                .Where(x => !x.Silinib && x.Aktivdir)
                .Include(x => x.Departament)
                .Include(x => x.Isci)
                .Include(x => x.Vezife)
                .Where(x => x.Isci != null && !x.Isci.Silinib && x.Isci.Status == IsciStatus.Aktiv)
                .ToListAsync();

            vm.DepartamentStat = teyinatlar
                .Where(x => x.Departament != null)
                .GroupBy(x => x.Departament.Ad ?? "—")
                .Select(g => new DepartamentStatDto
                {
                    Ad = g.Key,
                    IsciSayi = g.Count(),
                    KisiSayi = g.Count(x => x.Isci != null && x.Isci.Cins == Cins.Kisi),
                    QadinSayi = g.Count(x => x.Isci != null && x.Isci.Cins == Cins.Qadin)
                })
                .OrderByDescending(x => x.IsciSayi)
                .ToList();

            // Vəzifə paylanması
            vm.VezifeStat = teyinatlar
                .GroupBy(x => x.Vezife?.Ad ?? "—")
                .Select(g => new VezifeStatDto
                {
                    Ad = g.Key,
                    IsciSayi = g.Count()
                })
                .OrderByDescending(x => x.IsciSayi)
                .Take(10)
                .ToList();

            // ═══════════════════════════════════════════════════
            // 5. MAAŞ / MALİYYƏ
            // ═══════════════════════════════════════════════════
            var buAyMaaslar = await _uow.Repository<Maas>()
                .Query()
                .Where(x => !x.Silinib && x.Il == bugun.Year && x.Ay == bugun.Month)
                .ToListAsync();

            vm.UmumiBrutMaas = buAyMaaslar.Sum(x => x.BrutMebleg);
            vm.UmumiNetMaas = buAyMaaslar.Sum(x => x.NetMebleg);
            vm.MaasLayihe = buAyMaaslar.Count(x => x.Status == MaasStatus.Layihe);
            vm.MaasTesdiqlendi = buAyMaaslar.Count(x => x.Status == MaasStatus.Tesdiqlendi);
            vm.MaasOdenildi = buAyMaaslar.Count(x => x.Status == MaasStatus.Odenildi);

            // Orta maaş — aktiv işçilərin cari maaşından
            var maliyeler = await _uow.Repository<IsciMaliye>()
                .Query()
                .Where(x => !x.Silinib && x.CariMaas > 0)
                .Select(x => x.CariMaas)
                .ToListAsync();
            vm.OrtaMaas = maliyeler.Any() ? maliyeler.Average() : 0;

            // Son maaş dəyişiklikləri
            var sonDeyisiklikler = await _uow.Repository<IsciMaasTarixcesi>()
                .Query()
                .Include(x => x.Isci)
                .Where(x => !x.Silinib)
                .OrderByDescending(x => x.DeyismeTarixi)
                .Take(8)
                .ToListAsync();

            vm.SonMaasDeyisiklikleri = sonDeyisiklikler
                .Select(x => new MaasDeyisiklikDto
                {
                    AdSoyad = x.Isci != null ? $"{x.Isci.Ad} {x.Isci.Soyad}" : "—",
                    KohneMaas = x.KohneMaas,
                    YeniMaas = x.YeniMaas,
                    Tarix = x.DeyismeTarixi
                })
                .ToList();

            // Maaş status sayları + bu ay maaş hesablanmamış aktiv işçilər
            vm.MaasLayiheSayi = vm.MaasLayihe;
            var hesablanmisIsciIdler = buAyMaaslar.Select(x => x.IsciId).ToHashSet();
            vm.MaasHesablanmamisSayi = isciler
                .Count(x => x.Status == IsciStatus.Aktiv && !hesablanmisIsciIdler.Contains(x.Id));

            // ═══════════════════════════════════════════════════
            // 6. AVANS
            // ═══════════════════════════════════════════════════
            var buAyAvanslar = await _uow.Repository<Avans>()
                .Query()
                .Where(x => !x.Silinib && x.Il == bugun.Year && x.Ay == bugun.Month)
                .ToListAsync();

            vm.GozleyenAvansSayi = buAyAvanslar.Count(x => x.Status == AvansStatus.Gozlemede);
            var tasdiqlenmisAvanslar = buAyAvanslar
                .Where(x => x.Status == AvansStatus.Tesdiqlenib || x.Status == AvansStatus.Odenilib)
                .ToList();
            vm.BuAyAvansSayi = tasdiqlenmisAvanslar.Count;
            vm.BuAyAvansCemi = tasdiqlenmisAvanslar.Sum(x => x.Mebleg);

            // ═══════════════════════════════════════════════════
            // 7. SƏNƏD DÖVRİYYƏSİ
            // ═══════════════════════════════════════════════════
            var senedler = await _uow.Repository<Sened>()
                .Query()
                .Where(x => !x.Silinib)
                .Select(x => new { x.Status, x.SenedTarixi })
                .ToListAsync();
            vm.GozleyenSenedSayi = senedler.Count(x => x.Status == SenedStatusu.Yoxlanilir);
            vm.AktivSenedSayi = senedler.Count(x => x.Status != SenedStatusu.Arxiv);
            vm.BuAySenedSayi = senedler.Count(x =>
                x.SenedTarixi.Year == bugun.Year && x.SenedTarixi.Month == bugun.Month);

            // ═══════════════════════════════════════════════════
            // 8. SWIFT / ÖDƏNİŞ TAPŞIRIQLARI
            // ═══════════════════════════════════════════════════
            var buAyOdenisler = await _uow.Repository<OdenisTapsirigi>()
                .Query()
                .Where(x => !x.Silinib && x.Tarix.Year == bugun.Year && x.Tarix.Month == bugun.Month)
                .Select(x => new { x.Mebleg })
                .ToListAsync();
            vm.BuAyOdenisTapsirigiSayi = buAyOdenisler.Count;
            vm.BuAyOdenisTapsirigiMebleg = buAyOdenisler.Sum(x => x.Mebleg);

            // ═══════════════════════════════════════════════════
            // 9. KRİTİK MƏZUNİYYƏT BALANSI (illik ≤ 3 gün qalan aktiv işçilər)
            // ═══════════════════════════════════════════════════
            const int KritikHedd = 3;
            var balanslar = await _uow.Repository<MezuniyyetBalans>()
                .Query()
                .Where(x => !x.Silinib && x.Il == bugun.Year && x.Nov == MezuniyyetNovu.Illik)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Departament)
                .ToListAsync();

            var kritikList = balanslar
                .Where(x => x.Isci != null && !x.Isci.Silinib && x.Isci.Status == IsciStatus.Aktiv)
                .Select(x => new
                {
                    AdSoyad = $"{x.Isci.Ad} {x.Isci.Soyad}",
                    Departament = x.Isci.IsciTeyinatlari
                        .Select(t => t.Departament != null ? t.Departament.Ad : null)
                        .FirstOrDefault() ?? "—",
                    Qalan = x.ToplamGun - x.IstifadeOlunanGun
                })
                .Where(x => x.Qalan <= KritikHedd)
                .OrderBy(x => x.Qalan)
                .ToList();

            vm.KritikMezBalansSayi = kritikList.Count;
            vm.KritikBalansIsciler = kritikList
                .Take(8)
                .Select(x => new KritikBalansDto
                {
                    AdSoyad = x.AdSoyad,
                    Departament = x.Departament,
                    QalanGun = x.Qalan
                })
                .ToList();

            ViewData["Title"] = "Rəhbər Dashboard";
            ViewData["UserRole"] = "Rəhbər";
            return View(vm);
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "RehberDashboard Index xətası");
            ViewData["Title"] = "Rəhbər Dashboard";
            ViewData["UserRole"] = "Rəhbər";
            ViewBag.ErrorMessage = "Dashboard yüklənmədi: " + ex.Message;
            return View("~/Areas/HR/Views/RehberDashboard/Index.cshtml", new RehberDashboardVM());
          }
        }

        // ═══════════════════════════════════════════════════════════════
        // Davamiyyət — rəhbər üçün ayrıca səhifə (HR Davamiyyət-in eynisi)
        // Belə ki, rəhbərin HR controller-ə icazəsi olmasa belə davamiyyəti
        // görə bilsin. JS eyni faylı işlədir; URL-ləri view-dakı data-*
        // atributlarından oxuyur.
        // ═══════════════════════════════════════════════════════════════
        public async Task<IActionResult> Davamiyyet()
        {
            var bugun = DateTime.Today;
            var list = await _davamiyyetService.TarixUzreAsync(bugun);

            list = list
                .OrderBy(x => x.GirisVaxti == null)
                .ThenBy(x => x.GirisVaxti)
                .ThenBy(x => x.IsciTamAd)
                .ToList();

            var aktivIsciSayi = await _uow.Repository<Isci>()
                .Query()
                .CountAsync(x => !x.Silinib && x.Status == IsciStatus.Aktiv);
            ViewBag.AktivIsciSayi = aktivIsciSayi;

            // Bugün aktiv məzuniyyətdə olan işçi sayı + IsciId siyahısı
            try
            {
                var mezuniyyetIsciIds = await _uow.Repository<Mezuniyyet>()
                    .Query().AsNoTracking()
                    .Where(x => !x.Silinib &&
                                x.Status == MezuniyyetStatus.Tesdiqlenib &&
                                x.BaslamaTarixi.Date <= bugun &&
                                x.BitmeTarixi.Date >= bugun)
                    .Select(x => x.IsciId)
                    .ToListAsync();
                ViewBag.MezuniyyetSayi = mezuniyyetIsciIds.Count;
                ViewBag.MezuniyyetIsciIds = mezuniyyetIsciIds.ToHashSet();
            }
            catch { ViewBag.MezuniyyetSayi = 0; ViewBag.MezuniyyetIsciIds = new HashSet<int>(); }

            ViewData["Title"] = "Davamiyyət";
            return View(list);
        }

        // ═══════════════════════════════════════════════════════════════
        // İcazə İzləmə — rəhbər üçün işçi icazə statistikası
        // ═══════════════════════════════════════════════════════════════
        public async Task<IActionResult> IcazeIzleme(
            DateTime? tarixFrom,
            DateTime? tarixTo,
            int? departamentId,
            string? axtaris,
            int? status,
            string? sirala)
        {
            var filtr = new IcazeIzlemeFiltrDto
            {
                TarixFrom = tarixFrom,
                TarixTo = tarixTo,
                DepartamentId = departamentId,
                Axtaris = axtaris,
                Status = status,
            };

            var result = await _icazeService.GetIsciIzlemeAsync(filtr);
            var depResult = await _departamentService.HamisiniGetirAsync();

            var departamentler = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new("Bütün şöbələr", "")
            };
            if (depResult.Success && depResult.Data != null)
            {
                departamentler.AddRange(depResult.Data
                    .OrderBy(d => d.Ad)
                    .Select(d => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(d.Ad, d.Id.ToString())));
            }

            var isciler = result.Success ? result.Data!.ToList() : new();

            isciler = sirala switch
            {
                "cemi"   => isciler.OrderByDescending(x => x.CemiMuraciet).ToList(),
                "saat"   => isciler.OrderByDescending(x => x.TesdiqSaat).ToList(),
                "imtina" => isciler.OrderByDescending(x => x.ImtinaEdildiSayi).ToList(),
                _        => isciler.OrderByDescending(x => x.CemiMuraciet).ToList(),
            };

            var vm = new IcazeIzlemeVM
            {
                IsciIstatistikler = isciler,
                Filtr = filtr,
                Departamentler = departamentler,
            };

            ViewData["Sirala"] = sirala ?? "cemi";
            ViewData["Title"] = "İcazə İzləmə";

            if (!result.Success)
                TempData["Error"] = result.Message;

            return View(vm);
        }

        // ── Məzuniyyətdə olan işçilər (Rehber versiya) ──────────────
        [HttpGet]
        public async Task<IActionResult> GetMezuniyyetler(DateTime? tarix)
        {
            var hedef = (tarix ?? DateTime.Today).Date;

            var mezuniyyetler = await _uow.Repository<Mezuniyyet>()
                .Query().AsNoTracking()
                .Where(x => !x.Silinib &&
                             x.Status == MezuniyyetStatus.Tesdiqlenib &&
                             x.BaslamaTarixi.Date <= hedef &&
                             x.BitmeTarixi.Date >= hedef)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                        .ThenInclude(t => t.Departament)
                .ToListAsync();

            var records = mezuniyyetler.Select(m =>
            {
                var esasTeyinat = m.Isci?.IsciTeyinatlari?
                    .Where(t => t.Esasdir && !t.Silinib).FirstOrDefault()
                    ?? m.Isci?.IsciTeyinatlari?.FirstOrDefault(t => !t.Silinib);
                return new
                {
                    id = m.Id,
                    isciTamAd = ((m.Isci?.Ad ?? "") + " " + (m.Isci?.Soyad ?? "")).Trim(),
                    departamentAd = esasTeyinat?.Departament?.Ad ?? "-",
                    baslamaTarixi = m.BaslamaTarixi,
                    bitmeTarixi = m.BitmeTarixi,
                    efektivGunSayi = m.EfektivGunSayi
                };
            }).OrderBy(x => x.isciTamAd).ToList();

            return Json(new { records, count = records.Count, tarix = hedef });
        }

        [HttpGet]
        public async Task<IActionResult> DavamiyyetGetByTarix(DateTime? tarix, DateTime? baslangic, DateTime? son, int? isciId, int? status)
        {
            try
            {
                var umumi = await GetDavamiyyetFilteredData(tarix, baslangic, son, isciId, null);

                // Məzuniyyətdəki işçiləri İcazəli-dən ayır
                var mzBaslangic = (baslangic ?? tarix ?? DateTime.Today).Date;
                var mzSon       = (son       ?? tarix ?? DateTime.Today).Date;
                HashSet<int> mezuniyyetIsciIds;
                try
                {
                    var mzIds = await _uow.Repository<Mezuniyyet>()
                        .Query().AsNoTracking()
                        .Where(x => !x.Silinib &&
                                    x.Status == MezuniyyetStatus.Tesdiqlenib &&
                                    x.BaslamaTarixi.Date <= mzSon &&
                                    x.BitmeTarixi.Date >= mzBaslangic)
                        .Select(x => x.IsciId)
                        .ToListAsync();
                    mezuniyyetIsciIds = new HashSet<int>(mzIds);
                }
                catch { mezuniyyetIsciIds = new HashSet<int>(); }

                var result = status.HasValue
                    ? umumi.Where(x => (int)x.Status == status.Value).ToList()
                    : umumi;

                // status=4 (İcazəli) filtri zamanı məzuniyyətdəkiləri çıxar
                if (status.HasValue && status.Value == (int)DavamiyyetStatus.Icazeli)
                    result = result.Where(x => !mezuniyyetIsciIds.Contains(x.IsciId)).ToList();

                // IsParametri — nahar və TezCixan hesablaması üçün
                IsParametri parametri;
                try { parametri = await _uow.Repository<IsParametri>().Query().AsNoTracking().Where(x => !x.Silinib).FirstOrDefaultAsync() ?? new IsParametri(); }
                catch { parametri = new IsParametri(); }

                var standartCixis = parametri.StandartCixisVaxti;
                var tezCixmaTolerans = TimeSpan.FromMinutes(parametri.TezCixmaToleransDeqiqe);
                var naharBaslama = parametri.NaharBaslamaSaati;
                var naharBitis = naharBaslama.Add(TimeSpan.FromMinutes(parametri.NaharMuddetDeqiqe));

                var elanBaslangic = (baslangic ?? tarix ?? DateTime.Today).Date;
                var elanSon = (son ?? tarix ?? DateTime.Today).Date;
                var elanDict = new Dictionary<DateTime, TimeSpan>();
                try
                {
                    var elanlar = await _uow.Repository<IsGunuBitdiElan>()
                        .Query().AsNoTracking()
                        .Where(x => !x.Silinib && x.Tarix.Date >= elanBaslangic && x.Tarix.Date <= elanSon)
                        .ToListAsync();
                    foreach (var e in elanlar)
                        elanDict[e.Tarix.Date] = e.BitisVaxti;
                }
                catch { }

                var hedefTarixler = result.Select(x => x.Tarix.Date).Distinct().ToList();
                var bayramDict = new Dictionary<DateTime, TimeSpan>();
                try
                {
                    var bayramlar = await _uow.Repository<BayramGunu>()
                        .Query().AsNoTracking()
                        .Where(x => !x.Silinib && hedefTarixler.Contains(x.Tarix.Date) && x.XususiBitisVaxti.HasValue)
                        .ToListAsync();
                    bayramDict = bayramlar.ToDictionary(x => x.Tarix.Date, x => x.XususiBitisVaxti!.Value);
                }
                catch { }

                // ── Əlil işçi qısaldılmış Cümə qaydası (Tabel ilə eyni) ──────────────
                // Əlil işçi TAM 5 günlük həftənin 5-ci iş günü (Cümə) net ≥4 saat işləyibsə tez çıxan sayılmır.
                var elilIsciIds = new HashSet<int>();
                var elilBayramSet = new HashSet<DateTime>();
                try
                {
                    var hedefIsciler = result.Select(x => x.IsciId).Distinct().ToList();
                    var minHedef = hedefTarixler.Count > 0 ? hedefTarixler.Min() : DateTime.Today;
                    var maxHedef = hedefTarixler.Count > 0 ? hedefTarixler.Max() : DateTime.Today;
                    var elilGuzestler = await _uow.Repository<IsciGuzest>()
                        .Query().AsNoTracking()
                        .Where(ig => !ig.Silinib && hedefIsciler.Contains(ig.IsciId) &&
                                     ig.Guzest.Ad.Contains("Əlil") &&
                                     ig.BaslamaTarixi.Date <= maxHedef &&
                                     (ig.BitmeTarixi == null || ig.BitmeTarixi.Value.Date >= minHedef))
                        .Select(ig => ig.IsciId)
                        .ToListAsync();
                    elilIsciIds = new HashSet<int>(elilGuzestler);

                    if (elilIsciIds.Count > 0)
                    {
                        var bayramBas = minHedef.AddDays(-7);
                        var elilBayramlar = await _uow.Repository<BayramGunu>()
                            .Query().AsNoTracking()
                            .Where(b => !b.Silinib && b.Tip == GunTipi.Bayram &&
                                        b.Tarix.Date >= bayramBas && b.Tarix.Date <= maxHedef)
                            .Select(b => b.Tarix)
                            .ToListAsync();
                        elilBayramSet = new HashSet<DateTime>(elilBayramlar.Select(d => d.Date));
                    }
                }
                catch { /* IsciGuzest/BayramGunu mövcud deyilsə əlil qaydası keçilir */ }

                var tezCixanSayi = 0;
                var data = result.Select(x =>
                {
                    var gunCixis = bayramDict.TryGetValue(x.Tarix.Date, out var bv) ? bv : standartCixis;
                    var gunHedd = elanDict.TryGetValue(x.Tarix.Date, out var elanVaxt)
                        ? elanVaxt
                        : gunCixis - tezCixmaTolerans;

                    int? islemeSaatiDeq = null;
                    bool naharCixildi = false;
                    if (x.GirisVaxti.HasValue && x.CixisVaxti.HasValue)
                    {
                        var diff = (int)(x.CixisVaxti.Value - x.GirisVaxti.Value).TotalMinutes;
                        if (x.CixisVaxti.Value.TimeOfDay > naharBitis && x.GirisVaxti.Value.TimeOfDay < naharBaslama)
                        {
                            diff -= parametri.NaharMuddetDeqiqe;
                            naharCixildi = true;
                        }
                        islemeSaatiDeq = Math.Max(0, diff);
                    }

                    // Əlil işçi qısaldılmış Cümə günü net ≥4 saat (240 dəq) işləyibsə tez çıxan sayılmır
                    bool elilCumeBagisla = elilIsciIds.Contains(x.IsciId)
                        && EmekRejimiHelper.ElilQisaldilmisGun(x.Tarix.Date, elilBayramSet)
                        && (islemeSaatiDeq ?? 0) >= 240;

                    var tezCixanFlag = x.CixisVaxti.HasValue && x.CixisVaxti.Value.TimeOfDay < gunHedd
                        && !elilCumeBagisla;

                    return new
                    {
                        id = x.Id,
                        isciTamAd = x.IsciTamAd ?? "-",
                        departamentAd = x.DepartamentAd ?? "-",
                        tarix = x.Tarix,
                        girisVaxti = x.GirisVaxti,
                        cixisVaxti = x.CixisVaxti,
                        status = (int)x.Status,
                        maasdanKes = x.MaasdanKes,
                        qayibSebebi = x.QayibSebebi ?? "",
                        tezCixan = tezCixanFlag,
                        islemeSaatiDeq,
                        naharCixildi
                    };
                }).OrderByDescending(x => x.tarix).ThenBy(x => x.isciTamAd).ToList();
                tezCixanSayi = data.Count(x => x.tezCixan);

                var gelib = umumi.Count(x => x.Status == DavamiyyetStatus.Isde || x.Status == DavamiyyetStatus.Gecikme);
                var gecikme = umumi.Count(x => x.Status == DavamiyyetStatus.Gecikme);
                var qayib = umumi.Count(x => x.Status == DavamiyyetStatus.Qayib);
                var icazeli = umumi.Count(x => x.Status == DavamiyyetStatus.Icazeli && !mezuniyyetIsciIds.Contains(x.IsciId));
                var xestelik = umumi.Count(x => x.Status == DavamiyyetStatus.Xestelik);
                var ezamiyyet = umumi.Count(x => x.Status == DavamiyyetStatus.Ezamiyyet);

                var iseSaatleri = umumi
                    .Where(x => x.GirisVaxti.HasValue && x.CixisVaxti.HasValue)
                    .Select(x =>
                    {
                        var saatlar = (x.CixisVaxti!.Value - x.GirisVaxti!.Value).TotalHours;
                        if (x.CixisVaxti.Value.TimeOfDay > naharBitis && x.GirisVaxti.Value.TimeOfDay < naharBaslama)
                            saatlar -= parametri.NaharMuddetDeqiqe / 60.0;
                        return Math.Max(0, saatlar);
                    })
                    .ToList();
                var ortaIsSaati = iseSaatleri.Any() ? Math.Round(iseSaatleri.Average(), 1) : 0;

                var enCoxGecikenDept = umumi
                    .Where(x => x.Status == DavamiyyetStatus.Gecikme)
                    .GroupBy(x => x.DepartamentAd ?? "-")
                    .OrderByDescending(g => g.Count())
                    .Select(g => new { ad = g.Key, say = g.Count() })
                    .FirstOrDefault();

                return Json(new
                {
                    records = data,
                    stats = new
                    {
                        gelib,
                        gecikme,
                        qayib,
                        icazeli,
                        xestelik,
                        ezamiyyet,
                        tezCixan = tezCixanSayi,
                        cemi = umumi.Count,
                        ortaIsSaati,
                        enCoxGecikenDept = enCoxGecikenDept?.ad ?? "-",
                        enCoxGecikenDeptSay = enCoxGecikenDept?.say ?? 0
                    },
                    isParametri = new
                    {
                        girisVaxti = parametri.StandartGirisVaxti.ToString(@"hh\:mm"),
                        cixisVaxti = parametri.StandartCixisVaxti.ToString(@"hh\:mm"),
                        gecikmeTolerans = parametri.GecikmeToleransDeqiqe,
                        tezCixmaTolerans = parametri.TezCixmaToleransDeqiqe,
                        naharBaslamaSaati = naharBaslama.ToString(@"hh\:mm"),
                        naharMuddetDeqiqe = parametri.NaharMuddetDeqiqe
                    },
                    isGunuBitdiElan = elanDict.TryGetValue(DateTime.Today, out var bugunElan)
                        ? bugunElan.ToString(@"hh\:mm")
                        : (string?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RehberDashboard.DavamiyyetGetByTarix xətası");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DavamiyyetGetGozlenilen(DateTime? tarix)
        {
            var hedef = (tarix ?? DateTime.Today).Date;

            var aktivIsciler = await _uow.Repository<Isci>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .Include(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .ToListAsync();

            var qeydiOlanlar = await _uow.Repository<Davamiyyet>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Tarix.Date == hedef)
                .Select(x => x.IsciId)
                .ToListAsync();

            // Həmin gün üçün təsdiqlənmiş icazəsi olan işçilər
            var icazeliIsciIds = new HashSet<int>(
                await _uow.Repository<Icaze>()
                    .Query().AsNoTracking()
                    .Where(x => !x.Silinib
                             && x.Status == IcazeStatus.Tesdiqlenib
                             && x.IcazeTarixi.Date == hedef)
                    .Select(x => x.IsciId)
                    .ToListAsync());

            var gozlenilenler = aktivIsciler
                .Where(i => !qeydiOlanlar.Contains(i.Id))
                .Select(i =>
                {
                    var esasTeyinat = i.IsciTeyinatlari
                        .Where(t => t.Esasdir && !t.Silinib)
                        .FirstOrDefault()
                        ?? i.IsciTeyinatlari.FirstOrDefault(t => !t.Silinib);
                    int st = icazeliIsciIds.Contains(i.Id)
                        ? 4                                          // İcazəli
                        : (hedef < DateTime.Today ? 3 : 0);         // Qayib / Gözlənilir
                    return new
                    {
                        id = 0,
                        isciId = i.Id,
                        isciTamAd = i.Ad + " " + i.Soyad,
                        departamentAd = esasTeyinat?.Departament?.Ad ?? "-",
                        tarix = hedef,
                        girisVaxti = (DateTime?)null,
                        cixisVaxti = (DateTime?)null,
                        status = st
                    };
                })
                .OrderBy(x => x.isciTamAd)
                .ToList();

            return Json(new
            {
                records = gozlenilenler,
                count = gozlenilenler.Count,
                tarix = hedef
            });
        }

        [HttpGet]
        public async Task<IActionResult> DavamiyyetIsciAxtar(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new List<object>());

            var isciler = await _isciService.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv &&
                     (x.Ad.StartsWith(q) || x.Soyad.StartsWith(q)),
                izlemeden: true);

            var result = isciler.Success
                ? isciler.Data!.Take(10).Select(x => new { id = x.Id, tamAd = x.TamAd, sobe = x.SobeAdi ?? "-" })
                : Enumerable.Empty<object>();

            return Json(result);
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> DavamiyyetMovcudTarixler()
        {
            var bugun = DateTime.Today;
            var tarixler = await _uow.Repository<Davamiyyet>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Tarix.Date <= bugun)
                .Select(x => x.Tarix.Date)
                .Distinct()
                .OrderByDescending(x => x)
                .Take(365)
                .ToListAsync();

            return Json(tarixler.Select(t => t.ToString("yyyy-MM-dd")).ToList());
        }

        public async Task<IActionResult> DavamiyyetExportExcel(DateTime? tarix, DateTime? baslangic, DateTime? son, int? isciId, int? status)
        {
            var result = await GetDavamiyyetFilteredData(tarix, baslangic, son, isciId, status);
            var sorted = result.OrderByDescending(x => x.Tarix).ThenBy(x => x.IsciTamAd).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Davamiyyət");

            ws.Cell(1, 1).Value = "İşçi";
            ws.Cell(1, 2).Value = "Departament";
            ws.Cell(1, 3).Value = "Tarix";
            ws.Cell(1, 4).Value = "Giriş";
            ws.Cell(1, 5).Value = "Çıxış";
            ws.Cell(1, 6).Value = "İş saatı";
            ws.Cell(1, 7).Value = "Status";

            var headerRange = ws.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e2a3b");
            headerRange.Style.Font.FontColor = XLColor.White;

            for (int i = 0; i < sorted.Count; i++)
            {
                var r = sorted[i];
                var row = i + 2;

                ws.Cell(row, 1).Value = r.IsciTamAd;
                ws.Cell(row, 2).Value = r.DepartamentAd ?? "-";
                ws.Cell(row, 3).Value = r.Tarix.ToString("dd.MM.yyyy");
                ws.Cell(row, 4).Value = r.GirisVaxti?.ToString("HH:mm") ?? "--:--";
                ws.Cell(row, 5).Value = r.CixisVaxti?.ToString("HH:mm") ?? "--:--";

                if (r.GirisVaxti.HasValue && r.CixisVaxti.HasValue)
                {
                    var dur = r.CixisVaxti.Value - r.GirisVaxti.Value;
                    ws.Cell(row, 6).Value = $"{dur.Hours} s {dur.Minutes} d";
                }
                else
                    ws.Cell(row, 6).Value = "---";

                ws.Cell(row, 7).Value = r.Status switch
                {
                    DavamiyyetStatus.Isde => "İşdə",
                    DavamiyyetStatus.Gecikme => "Gecikmə",
                    DavamiyyetStatus.Qayib => "Qayıb",
                    DavamiyyetStatus.Icazeli => "İcazəli",
                    DavamiyyetStatus.Xestelik => "Xəstəlik",
                    DavamiyyetStatus.Ezamiyyet => "Ezamiyyət",
                    _ => "-"
                };
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var fileName = $"Davamiyyet_{DateTime.Now:yyyy-MM-dd_HHmm}.xlsx";
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ── Köməkçi: filter logic (HR DavamiyyetController-dakı ilə eyni) ──
        private async Task<IList<Application.DTOs.HR.Davamiyyet.DavamiyyetListDto>> GetDavamiyyetFilteredData(
            DateTime? tarix, DateTime? baslangic, DateTime? son, int? isciId, int? status)
        {
            IList<Application.DTOs.HR.Davamiyyet.DavamiyyetListDto> result;

            if (isciId.HasValue)
            {
                result = await _davamiyyetService.IsciUzreAsync(isciId.Value);
                if (baslangic.HasValue && son.HasValue)
                {
                    result = result
                        .Where(x => x.Tarix.Date >= baslangic.Value.Date && x.Tarix.Date <= son.Value.Date)
                        .ToList();
                }
            }
            else if (baslangic.HasValue && son.HasValue)
            {
                result = await _davamiyyetService.AraliqUzreAsync(baslangic.Value, son.Value);
            }
            else if (tarix.HasValue)
            {
                result = await _davamiyyetService.TarixUzreAsync(tarix.Value);
            }
            else
            {
                result = await _davamiyyetService.TarixUzreAsync(DateTime.Today);
            }

            if (status.HasValue)
            {
                result = result.Where(x => (int)x.Status == status.Value).ToList();
            }

            return result;
        }
    }
}
