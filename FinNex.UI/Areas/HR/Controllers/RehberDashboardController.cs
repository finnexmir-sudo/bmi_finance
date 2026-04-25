using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Maas_If;
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
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<RehberDashboardController> _logger;

        public RehberDashboardController(
            IUnitOfWork uow,
            IDavamiyyetService davamiyyetService,
            IMezuniyyetService mezuniyyetService,
            IIcazeService icazeService,
            UserManager<AppUser> userManager,
            ILogger<RehberDashboardController> logger)
        {
            _uow = uow;
            _davamiyyetService = davamiyyetService;
            _mezuniyyetService = mezuniyyetService;
            _icazeService = icazeService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
          try
          {
            var bugun = DateTime.Today;
            var buAyBaslangic = new DateTime(bugun.Year, bugun.Month, 1);
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
            // 2. DAVAMIYYƏT
            // ═══════════════════════════════════════════════════
            var bugunkuDav = await _davamiyyetService.TarixUzreAsync(bugun);
            var davlar = bugunkuDav?.ToList() ?? new();

            vm.BugunQeydVar = davlar.Any();
            vm.BugunIshde = davlar.Count(x => x.Status == DavamiyyetStatus.Isde);
            vm.BugunGeciken = davlar.Count(x => x.Status == DavamiyyetStatus.Gecikme);
            vm.BugunQayib = davlar.Count(x => x.Status == DavamiyyetStatus.Qayib);
            vm.BugunIcazeli = davlar.Count(x => x.Status == DavamiyyetStatus.Icazeli
                                              || x.Status == DavamiyyetStatus.Xestelik
                                              || x.Status == DavamiyyetStatus.Ezamiyyet);

            // Adlı siyahılar — rəhbərə "kim?" sualına dərhal cavab vermək üçün
            vm.BugunGecikenIsciler = davlar
                .Where(x => x.Status == DavamiyyetStatus.Gecikme)
                .Select(x => new DavamiyyetIsciDto
                {
                    AdSoyad = x.IsciTamAd ?? "—",
                    Departament = x.DepartamentAd ?? "—",
                    GirisVaxti = x.GirisVaxti?.ToString("HH:mm")
                })
                .ToList();

            vm.BugunQayibIsciler = davlar
                .Where(x => x.Status == DavamiyyetStatus.Qayib)
                .Select(x => new DavamiyyetIsciDto
                {
                    AdSoyad = x.IsciTamAd ?? "—",
                    Departament = x.DepartamentAd ?? "—",
                    GirisVaxti = null
                })
                .ToList();

            // Davamiyyət faizi (bu ay)
            var isGunleriSayi = 0;
            for (var d = buAyBaslangic; d <= bugun; d = d.AddDays(1))
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    isGunleriSayi++;

            var buAyDav = await _davamiyyetService.AraliqUzreAsync(buAyBaslangic, bugun);
            var buAyDavlar = buAyDav?.ToList() ?? new();
            var gelmelerSayi = buAyDavlar.Count(x =>
                x.Status == DavamiyyetStatus.Isde || x.Status == DavamiyyetStatus.Gecikme);
            var maxGelmeMumkun = vm.AktivIsciSayi * isGunleriSayi;
            vm.DavamiyyetFaizi = maxGelmeMumkun > 0
                ? (int)((double)gelmelerSayi / maxGelmeMumkun * 100)
                : 0;

            // Son girişlər
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
            var mezResult = await _mezuniyyetService.GetListAsync();
            var mezler = mezResult.Success && mezResult.Data != null
                ? mezResult.Data.ToList()
                : new();

            // Hazırda məzuniyyətdə olanlar
            var hazirdaMez = mezler.Where(x =>
                x.Status == MezuniyyetStatus.Tesdiqlenib &&
                x.BaslamaTarixi.Date <= bugun &&
                x.BitmeTarixi.Date >= bugun).ToList();

            vm.HazirdaMezuniyyetde = hazirdaMez.Count;
            vm.BugunMezuniyyetde = hazirdaMez.Count;

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

            vm.BuHefteBitenMezuniyyet = mezler.Count(x =>
                x.Status == MezuniyyetStatus.Tesdiqlenib &&
                x.BitmeTarixi.Date > bugun &&
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
    }
}
