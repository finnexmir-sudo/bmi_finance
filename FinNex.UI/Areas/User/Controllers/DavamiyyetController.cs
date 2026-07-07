using FinNex.Application.DTOs.HR.Davamiyyet;
using FinNex.Application.Services.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class DavamiyyetController : Controller
    {
        private readonly IDavamiyyetService _davamiyyetService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public DavamiyyetController(
            IDavamiyyetService davamiyyetService,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _davamiyyetService = davamiyyetService;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var cariIl = DateTime.Today.Year;
            var all = await _davamiyyetService.IsciUzreAsync(isciId.Value);
            var list = all.Where(x => x.Tarix.Year == cariIl).ToList();

            // Statistika HR ilə eyni məntiqlə hesablanır (erkən çıxış üçün ErkenCixisIcaze +
            // BayramGunu + IsGunuBitdiElan + status istisnaları; İcazəli sayından məzuniyyət
            // günləri çıxılır). View yalnız hazır rəqəmləri oxuyur.
            var hesablama = await HesablaAsync(isciId.Value, list);
            ViewBag.Stats = hesablama.Stats;
            ViewBag.TezCixanIds = hesablama.TezCixanIds;

            var ip = await GetIsParametriEntity();
            ViewBag.GirisVaxti = ip.StandartGirisVaxti.ToString(@"hh\:mm");
            ViewBag.CixisVaxti = ip.StandartCixisVaxti.ToString(@"hh\:mm");
            ViewBag.GecikmeTolerans = ip.GecikmeToleransDeqiqe;
            ViewBag.TezCixmaTolerans = ip.TezCixmaToleransDeqiqe;

            try
            {
                var mezuniyyetler = await _unitOfWork.Repository<Mezuniyyet>()
                    .HamisiniGetirAsync(
                        x => x.IsciId == isciId.Value &&
                             x.BaslamaTarixi.Year == cariIl &&
                             x.Status == MezuniyyetStatus.Tesdiqlenib,
                        izlemeden: true);
                ViewBag.MezuniyyetGun = mezuniyyetler.Sum(x => x.EfektivGunSayi);
            }
            catch
            {
                ViewBag.MezuniyyetGun = 0;
            }

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyRecords(DateTime? baslangic, DateTime? son, int? status)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null)
                return Unauthorized();

            var allRecords = await _davamiyyetService.IsciUzreAsync(isciId.Value);

            // umumi — status filtrindən ƏVVƏL: KPI-lar HR-dakı kimi sabit qalsın deyə
            IList<DavamiyyetListDto> umumi;
            if (baslangic.HasValue && son.HasValue)
            {
                umumi = allRecords
                    .Where(x => x.Tarix.Date >= baslangic.Value.Date && x.Tarix.Date <= son.Value.Date)
                    .ToList();
            }
            else
            {
                var cariIl = DateTime.Today.Year;
                umumi = allRecords.Where(x => x.Tarix.Year == cariIl).ToList();
            }

            var hesablama = await HesablaAsync(isciId.Value, umumi);

            // Status filtri yalnız cədvələ tətbiq olunur, statistikaya yox
            IEnumerable<DavamiyyetListDto> gosteris = umumi;
            if (status.HasValue)
            {
                gosteris = gosteris.Where(x => (int)x.Status == status.Value && !hesablama.TedbirGecikmeIds.Contains(x.Id));
                // İcazəli süzgəcində məzuniyyət günləri çıxarılır ki, sayğacla uyğun olsun
                if (status.Value == (int)DavamiyyetStatus.Icazeli)
                    gosteris = gosteris.Where(x => !hesablama.MezuniyyetIcazeliIds.Contains(x.Id));
            }

            var records = gosteris.Select(x => new
            {
                id = x.Id,
                tarix = x.Tarix,
                girisVaxti = x.GirisVaxti,
                cixisVaxti = hesablama.GorushCixisReturnIds.Contains(x.Id) ? (DateTime?)null : x.CixisVaxti,
                status = hesablama.TedbirGecikmeIds.Contains(x.Id) ? (int)DavamiyyetStatus.Isde : (int)x.Status,
                tezCixan = hesablama.TezCixanIds.Contains(x.Id)
            }).OrderByDescending(x => x.tarix).ToList();

            return Json(new { records, stats = hesablama.Stats });
        }

        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private async Task<IsParametri> GetIsParametriEntity()
        {
            try
            {
                var entity = await _unitOfWork.Repository<IsParametri>()
                    .Query().AsNoTracking().Where(x => !x.Silinib).FirstOrDefaultAsync();
                return entity ?? new IsParametri();
            }
            catch { return new IsParametri(); }
        }

        // ── HR (GetByTarix) ilə eyni intizam məntiqi ─────────────────────────
        // Erkən çıxış həddi: BayramGunu (qısa gün) → IsGunuBitdiElan ("İş Bitdi")
        // → standart çıxış − tolerans. İcazəli/Ezamiyyət statusları və ErkenCixisIcaze
        // (icazə günləri) erkən çıxış sayılmır. İcazəli sayğacından isə təsdiqlənmiş
        // məzuniyyət günləri çıxılır (HR davranışı).
        private async Task<DavamiyyetHesablamaNeticesi> HesablaAsync(int isciId, IList<DavamiyyetListDto> records)
        {
            var netice = new DavamiyyetHesablamaNeticesi();
            var ip = await GetIsParametriEntity();
            var standartCixis = ip.StandartCixisVaxti;
            var tezCixmaTolerans = TimeSpan.FromMinutes(ip.TezCixmaToleransDeqiqe);
            var naharBaslama = ip.NaharBaslamaSaati;
            var naharBitis = naharBaslama.Add(TimeSpan.FromMinutes(ip.NaharMuddetDeqiqe));

            // Erkən çıxış / çıxış yoxdur statistikası yalnız 01.06.2026-dan (sistemin canlı başlama tarixi)
            var intizamBaslangic = new DateTime(2026, 6, 1);
            var intizamResult = records.Where(x => x.Tarix.Date >= intizamBaslangic).ToList();
            var tarixlerSet = intizamResult.Select(x => x.Tarix.Date).Distinct().ToList();

            // IsGunuBitdiElan — "İş Bitdi" elan olunan günlərdə elan vaxtı həddir
            var elanDict = new Dictionary<DateTime, TimeSpan>();
            try
            {
                var elanlar = await _unitOfWork.Repository<IsGunuBitdiElan>()
                    .Query().AsNoTracking()
                    .Where(x => !x.Silinib && tarixlerSet.Contains(x.Tarix.Date))
                    .ToListAsync();
                foreach (var e in elanlar) elanDict[e.Tarix.Date] = e.BitisVaxti;
            }
            catch { }

            // BayramGunu — qısa günlərin xüsusi bitmə vaxtı
            var bayramDict = new Dictionary<DateTime, TimeSpan>();
            try
            {
                var bayramlar = await _unitOfWork.Repository<BayramGunu>()
                    .Query().AsNoTracking()
                    .Where(x => !x.Silinib && tarixlerSet.Contains(x.Tarix.Date) && x.XususiBitisVaxti.HasValue)
                    .ToListAsync();
                foreach (var b in bayramlar) bayramDict[b.Tarix.Date] = b.XususiBitisVaxti!.Value;
            }
            catch { }

            // ── Əlil işçi qısaldılmış Cümə qaydası (Tabel ilə eyni) ──────────────
            // Əlil işçi TAM 5 günlük həftənin 5-ci iş günü (Cümə) net ≥4 saat işləyibsə
            // erkən çıxış sayılmır — tabeldə həmin gün onsuz da 4 saat yazılır.
            bool isElil = false;
            var elilBayramSet = new HashSet<DateTime>();
            try
            {
                if (tarixlerSet.Count > 0)
                {
                    var minTarix = tarixlerSet.Min();
                    var maxTarix = tarixlerSet.Max();
                    // Mərkəzi mənbə (EmekRejimiHelper) — HR/Rəhbər/User eyni datadan
                    var (elilIds, bSet) = await EmekRejimiHelper.ElilQisaldilmisDataAsync(
                        _unitOfWork, new[] { isciId }, minTarix, maxTarix);
                    isElil = elilIds.Contains(isciId);
                    elilBayramSet = bSet;
                }
            }
            catch { isElil = false; /* təhlükəsiz: yüklənmə alınmasa bağışlama YOX, erkən çıxış tutulur */ }

            // ErkenCixisIcaze — HR ilə eyni mənbə: erkən çıxış icazəsi olan günlər
            var erkenIcazeDates = new HashSet<DateTime>();
            try
            {
                var eiList = await _unitOfWork.Repository<ErkenCixisIcaze>()
                    .Query().AsNoTracking()
                    .Where(x => !x.Silinib && x.IsciId == isciId && tarixlerSet.Contains(x.Tarix.Date))
                    .Select(x => x.Tarix)
                    .ToListAsync();
                foreach (var d in eiList) erkenIcazeDates.Add(d.Date);
            }
            catch { }

            // Təsdiqlənmiş SAAT İCAZƏLƏRİ (günün sonunu örtən) — erkən çıxışı bağışlayır
            var icazeList = new List<(DateTime Tarix, TimeSpan Bas, TimeSpan Bit)>();
            try
            {
                var ics = await _unitOfWork.Repository<Icaze>()
                    .Query().AsNoTracking()
                    .Where(x => !x.Silinib && x.IsciId == isciId &&
                                x.Status == IcazeStatus.Tesdiqlenib &&
                                tarixlerSet.Contains(x.IcazeTarixi.Date))
                    .Select(x => new { x.IcazeTarixi, x.BaslamaSaati, x.BitisSaati })
                    .ToListAsync();
                foreach (var i in ics)
                    icazeList.Add((i.IcazeTarixi.Date, i.BaslamaSaati, i.BitisSaati));
            }
            catch { }

            // Təsdiqlənmiş EZAMİYYƏTLƏR — tarixi əhatə edən
            var ezamiyyetList = new List<(DateTime Bas, DateTime Bit, TimeSpan? BasSaat)>();
            try
            {
                var ezs = await _unitOfWork.Repository<EzamiyyetMuraciet>()
                    .Query().AsNoTracking()
                    .Where(x => !x.Silinib && x.IsciId == isciId &&
                                x.Status == EzamiyyetStatus.Tesdiqlendi &&
                                x.BitmeTarixi.Date >= intizamBaslangic)
                    .Select(x => new { x.BaslamaTarixi, x.BitmeTarixi, x.BaslamaSaati })
                    .ToListAsync();
                foreach (var e in ezs)
                    ezamiyyetList.Add((e.BaslamaTarixi.Date, e.BitmeTarixi.Date, e.BaslamaSaati));
            }
            catch { }

            // Tədbir (offline görüş) — həm gecikmə, həm erkən çıxış bağışlanması (HR/ADMS ilə eyni)
            var gorushDict = new Dictionary<DateTime, (TimeSpan Bas, TimeSpan Bit)>();
            try
            {
                var grs = await _unitOfWork.Repository<GorushIshtirakci>()
                    .Query().AsNoTracking()
                    .Where(x => !x.Silinib && x.IsciId == isciId
                             && x.Gorush.Nov == GorushNovu.Offline
                             && x.Gorush.Status != GorushStatus.LegvEdildi
                             && x.Status != IshtirakciStatus.Redd
                             && x.Status != IshtirakciStatus.IshtiraketmeyecekBildirib
                             && x.Gorush.BitisSaati != null
                             && x.Gorush.Tarix.Date >= intizamBaslangic)
                    .Select(x => new { Tarix = x.Gorush.Tarix.Date, Bas = x.Gorush.BaslamaSaati, Bit = x.Gorush.BitisSaati!.Value })
                    .ToListAsync();
                foreach (var g in grs)
                    if (!gorushDict.TryGetValue(g.Tarix, out var cur) || g.Bit > cur.Bit)
                        gorushDict[g.Tarix] = (g.Bas, g.Bit);
            }
            catch { }
            var gecikTolerans = TimeSpan.FromMinutes(ip.GecikmeToleransDeqiqe);

            // Per-record erkən çıxış (HR GetByTarix ilə eyni düstur)
            foreach (var x in intizamResult)
            {
                if (!x.CixisVaxti.HasValue) continue;
                if (x.Status == DavamiyyetStatus.Icazeli) continue;
                if (x.Status == DavamiyyetStatus.Ezamiyyet) continue;
                if (erkenIcazeDates.Contains(x.Tarix.Date)) continue;
                var gunCixis = bayramDict.TryGetValue(x.Tarix.Date, out var bv) ? bv : standartCixis;
                var hedd = elanDict.TryGetValue(x.Tarix.Date, out var ev) ? ev : gunCixis - tezCixmaTolerans;
                var cixisTod = x.CixisVaxti.Value.TimeOfDay;
                if (cixisTod >= hedd) continue;   // erkən deyil

                // Təsdiqlənmiş saat icazəsi (günün sonunu örtən) və ya ezamiyyət bağışlayır
                bool icazeOrtuyur = icazeList.Any(i => i.Tarix == x.Tarix.Date &&
                    i.Bit >= hedd && cixisTod >= i.Bas - tezCixmaTolerans);
                bool ezamiyyetOrtuyur = ezamiyyetList.Any(e => e.Bas <= x.Tarix.Date && e.Bit >= x.Tarix.Date &&
                    (e.BasSaat == null || cixisTod >= e.BasSaat.Value - tezCixmaTolerans));
                // Tədbir: çıxış tədbir pəncərəsindədirsə erkən çıxış deyil
                bool gorushOrtuyur = gorushDict.TryGetValue(x.Tarix.Date, out var gw)
                    && cixisTod >= gw.Bas - tezCixmaTolerans && cixisTod <= gw.Bit + tezCixmaTolerans;
                if (icazeOrtuyur || ezamiyyetOrtuyur || gorushOrtuyur) continue;

                // Əlil işçi qısaldılmış Cümə günü net ≥4 saat (240 dəq) işləyibsə erkən çıxış bağışlanır
                if (isElil && EmekRejimiHelper.ElilQisaldilmisGun(x.Tarix.Date, elilBayramSet))
                {
                    int netDeq = 0;
                    if (x.GirisVaxti.HasValue && x.CixisVaxti.HasValue)
                    {
                        netDeq = (int)(x.CixisVaxti.Value - x.GirisVaxti.Value).TotalMinutes;
                        if (x.CixisVaxti.Value.TimeOfDay > naharBitis && x.GirisVaxti.Value.TimeOfDay < naharBaslama)
                            netDeq -= ip.NaharMuddetDeqiqe;
                        netDeq = Math.Max(0, netDeq);
                    }
                    if (netDeq >= 240) continue;
                }

                netice.TezCixanIds.Add(x.Id);
            }

            // Təsdiqlənmiş məzuniyyət günləri — İcazəli sayğacından çıxılır (HR məntiqi)
            if (records.Any())
            {
                try
                {
                    var minTarix = records.Min(x => x.Tarix.Date);
                    var maxTarix = records.Max(x => x.Tarix.Date);
                    var mezuniyyetler = await _unitOfWork.Repository<Mezuniyyet>()
                        .Query().AsNoTracking()
                        .Where(x => !x.Silinib && x.IsciId == isciId &&
                                    x.Status == MezuniyyetStatus.Tesdiqlenib &&
                                    x.BaslamaTarixi.Date <= maxTarix &&
                                    x.BitmeTarixi.Date >= minTarix)
                        .Select(x => new { x.BaslamaTarixi, x.BitmeTarixi })
                        .ToListAsync();

                    foreach (var x in records)
                    {
                        if (x.Status != DavamiyyetStatus.Icazeli) continue;
                        var d = x.Tarix.Date;
                        if (mezuniyyetler.Any(m => d >= m.BaslamaTarixi.Date && d <= m.BitmeTarixi.Date))
                            netice.MezuniyyetIcazeliIds.Add(x.Id);
                    }
                }
                catch { }
            }

            // Tədbir pəncərəsi: giriş → gecikmə bağışlanır; çıxış → tədbirdən qayıdış (əsl çıxış deyil)
            foreach (var x in records)
            {
                if (!gorushDict.TryGetValue(x.Tarix.Date, out var gw)) continue;
                if (x.Status == DavamiyyetStatus.Gecikme && x.GirisVaxti.HasValue
                    && x.GirisVaxti.Value.TimeOfDay <= gw.Bit + gecikTolerans)
                    netice.TedbirGecikmeIds.Add(x.Id);
                if (x.CixisVaxti.HasValue
                    && x.CixisVaxti.Value.TimeOfDay >= gw.Bas - tezCixmaTolerans
                    && x.CixisVaxti.Value.TimeOfDay <= gw.Bit + tezCixmaTolerans)
                    netice.GorushCixisReturnIds.Add(x.Id);
            }

            netice.Stats = new DavamiyyetStatlar
            {
                Isde      = records.Count(x => x.Status == DavamiyyetStatus.Isde),
                Gecikme   = records.Count(x => x.Status == DavamiyyetStatus.Gecikme && !netice.TedbirGecikmeIds.Contains(x.Id)),
                Qayib     = records.Count(x => x.Status == DavamiyyetStatus.Qayib),
                Icazeli   = records.Count(x => x.Status == DavamiyyetStatus.Icazeli && !netice.MezuniyyetIcazeliIds.Contains(x.Id)),
                Xestelik  = records.Count(x => x.Status == DavamiyyetStatus.Xestelik),
                Ezamiyyet = records.Count(x => x.Status == DavamiyyetStatus.Ezamiyyet),
                TezCixan  = netice.TezCixanIds.Count,
                // Bugün hələ bitməyib — çıxış vurulmaması normaldır; yalnız KEÇMİŞ günləri say.
                CixisYox  = intizamResult.Count(x => x.Tarix.Date < DateTime.Today && x.GirisVaxti.HasValue && !x.CixisVaxti.HasValue),
                Cemi      = records.Count
            };

            return netice;
        }

        private class DavamiyyetHesablamaNeticesi
        {
            public DavamiyyetStatlar Stats { get; set; } = new();
            public HashSet<int> TezCixanIds { get; set; } = new();
            public HashSet<int> MezuniyyetIcazeliIds { get; set; } = new();
            public HashSet<int> TedbirGecikmeIds { get; set; } = new();
            public HashSet<int> GorushCixisReturnIds { get; set; } = new();
        }
    }

    // İlk yükləmə (ViewBag) və GetMyRecords (JSON) üçün ümumi statistika konteyneri
    public class DavamiyyetStatlar
    {
        public int Isde { get; set; }
        public int Gecikme { get; set; }
        public int Qayib { get; set; }
        public int Icazeli { get; set; }
        public int Xestelik { get; set; }
        public int Ezamiyyet { get; set; }
        public int TezCixan { get; set; }
        public int CixisYox { get; set; }
        public int Cemi { get; set; }
    }
}
