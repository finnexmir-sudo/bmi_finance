using System.Security.Claims;
using ClosedXML.Excel;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Services.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class DavamiyyetController : Controller
    {
        private readonly IDavamiyyetService _davamiyyetService;
        private readonly IIsciService _isciService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJetonTeklifleriService _teklifService;
        private readonly IBildirisRouter _bildirisRouter;

        private readonly ILogger<DavamiyyetController> _logger;

        public DavamiyyetController(
            IDavamiyyetService davamiyyetService,
            IIsciService isciService,
            IUnitOfWork unitOfWork,
            IJetonTeklifleriService teklifService,
            IBildirisRouter bildirisRouter,
            ILogger<DavamiyyetController> logger)
        {
            _davamiyyetService = davamiyyetService;
            _isciService = isciService;
            _unitOfWork = unitOfWork;
            _teklifService = teklifService;
            _bildirisRouter = bildirisRouter;
            _logger = logger;
        }

        // Cari istifadəçinin işçi qeydi (AppUser → Isci.AppUserId).
        // Tapılmasa null — admin hesabı işçiyə bağlı olmaya bilər və bu,
        // əməliyyatı BLOKLAMAMALIDIR (audit sahəsi 0 qalır).
        private async Task<int?> CariIsciIdAsync()
        {
            var xam = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(xam, out var appUserId)) return null;

            var isci = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.AppUserId == appUserId && !x.Silinib)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            return isci;
        }

        public async Task<IActionResult> Index()
        {
            var bugun = DateTime.Today;
            var list = await _davamiyyetService.TarixUzreAsync(bugun);

            // Giriş saatına görə sırala — qeydə alınanlar əvvəl, sonra digərləri
            list = list
                .OrderBy(x => x.GirisVaxti == null)
                .ThenBy(x => x.GirisVaxti)
                .ThenBy(x => x.IsciTamAd)
                .ToList();

            // Aktiv işçi sayı — gözlənilir hesablanması üçün
            var aktivIsciSayi = await _unitOfWork.Repository<Isci>()
                .Query()
                .CountAsync(x => !x.Silinib && x.Status == IsciStatus.Aktiv);
            ViewBag.AktivIsciSayi = aktivIsciSayi;

            // Bugün aktiv məzuniyyətdə olan işçi sayı + IsciId siyahısı
            try
            {
                var mezuniyyetIsciIds = await _unitOfWork.Repository<Mezuniyyet>()
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

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetByTarix(DateTime? tarix, DateTime? baslangic, DateTime? son, int? isciId, int? status)
        {
            try
            {
                var parametri = await GetIsParametriEntity();
                var standartCixis = parametri.StandartCixisVaxti;
                var tezCixmaTolerans = TimeSpan.FromMinutes(parametri.TezCixmaToleransDeqiqe);
                var naharBaslama = parametri.NaharBaslamaSaati;
                var naharBitis = naharBaslama.Add(TimeSpan.FromMinutes(parametri.NaharMuddetDeqiqe));

                // IsGunuBitdiElan — həmin tarix üçün düymə vurulubsa, o vaxt TezCixan həddi kimi götürülür
                var elanBaslangic = (baslangic ?? tarix ?? DateTime.Today).Date;
                var elanSon = (son ?? tarix ?? DateTime.Today).Date;
                var elanDict = new Dictionary<DateTime, TimeSpan>();
                try
                {
                    var elanlar = await _unitOfWork.Repository<IsGunuBitdiElan>()
                        .Query().AsNoTracking()
                        .Where(x => !x.Silinib && x.Tarix.Date >= elanBaslangic && x.Tarix.Date <= elanSon)
                        .ToListAsync();
                    foreach (var e in elanlar)
                        elanDict[e.Tarix.Date] = e.BitisVaxti;
                }
                catch { }

                // KPI-lar filter-dən ƏVVƏL hesablanır ki, status filtri cədvələ təsir
                // etsə də, yuxarıdakı statistika sabit qalsın.
                var umumi = await GetFilteredData(tarix, baslangic, son, isciId, null);

                // Məzuniyyətdəki işçiləri İcazəli-dən ayır
                var mzBaslangic = (baslangic ?? tarix ?? DateTime.Today).Date;
                var mzSon       = (son       ?? tarix ?? DateTime.Today).Date;
                HashSet<int> mezuniyyetIsciIds;
                try
                {
                    var mzIds = await _unitOfWork.Repository<Mezuniyyet>()
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

                // Erkən çıxış icazəsi olan işçilər (tarix+isciId cütü)
                var erkenIcazeSet = new HashSet<(int, DateTime)>();
                try
                {
                    var eiBaslangic = (baslangic ?? tarix ?? DateTime.Today).Date;
                    var eiSon       = (son       ?? tarix ?? DateTime.Today).Date;
                    var eiList = await _unitOfWork.Repository<ErkenCixisIcaze>()
                        .Query().AsNoTracking()
                        .Where(x => !x.Silinib &&
                                    x.Tarix.Date >= eiBaslangic &&
                                    x.Tarix.Date <= eiSon)
                        .Select(x => new { x.IsciId, x.Tarix })
                        .ToListAsync();
                    foreach (var ei in eiList)
                        erkenIcazeSet.Add((ei.IsciId, ei.Tarix.Date));
                }
                catch { }

                // Təsdiqlənmiş SAAT İCAZƏLƏRİ — günün sonunu örtən icazə erkən çıxışı bağışlayır
                var icazeList = new List<(int IsciId, DateTime Tarix, TimeSpan Bas, TimeSpan Bit)>();
                try
                {
                    var icB = (baslangic ?? tarix ?? DateTime.Today).Date;
                    var icS = (son ?? tarix ?? DateTime.Today).Date;
                    var ics = await _unitOfWork.Repository<Icaze>()
                        .Query().AsNoTracking()
                        .Where(x => !x.Silinib && x.Status == IcazeStatus.Tesdiqlenib &&
                                    x.IcazeTarixi.Date >= icB && x.IcazeTarixi.Date <= icS)
                        .Select(x => new { x.IsciId, x.IcazeTarixi, x.BaslamaSaati, x.BitisSaati })
                        .ToListAsync();
                    foreach (var i in ics)
                        icazeList.Add((i.IsciId, i.IcazeTarixi.Date, i.BaslamaSaati, i.BitisSaati));
                }
                catch { }

                // "İcazəli (indi)" — CANLI: işçi (1) təsdiqlənmiş icazəsi var, (2) FAKTİKİ çıxıb
                // (çıxış icazə başlanğıcından sonra) və (3) bu gündürsə indiki saat pəncərədədir
                // (Bas ≤ indi ≤ Bit). Keçmiş gün → həmin gün icazəni faktiki işlədib.
                var indiSaatIcaze = DateTime.Now.TimeOfDay;
                var icazeliIndiIds = new HashSet<int>(umumi
                    .Where(x => x.CixisVaxti.HasValue && icazeList.Any(i =>
                        i.IsciId == x.IsciId && i.Tarix == x.Tarix.Date &&
                        x.CixisVaxti.Value.TimeOfDay >= i.Bas - tezCixmaTolerans &&
                        (x.Tarix.Date != DateTime.Today || (indiSaatIcaze >= i.Bas && indiSaatIcaze <= i.Bit))))
                    .Select(x => x.IsciId));

                // Təsdiqlənmiş EZAMİYYƏTLƏR — tarix aralığını örtən
                var ezamiyyetList = new List<(int IsciId, DateTime Bas, DateTime Bit, TimeSpan? BasSaat)>();
                try
                {
                    var ezB = (baslangic ?? tarix ?? DateTime.Today).Date;
                    var ezS = (son ?? tarix ?? DateTime.Today).Date;
                    var ezs = await _unitOfWork.Repository<EzamiyyetMuraciet>()
                        .Query().AsNoTracking()
                        .Where(x => !x.Silinib && x.Status == EzamiyyetStatus.Tesdiqlendi &&
                                    x.BaslamaTarixi.Date <= ezS && x.BitmeTarixi.Date >= ezB)
                        .Select(x => new { x.IsciId, x.BaslamaTarixi, x.BitmeTarixi, x.BaslamaSaati })
                        .ToListAsync();
                    foreach (var e in ezs)
                        ezamiyyetList.Add((e.IsciId, e.BaslamaTarixi.Date, e.BitmeTarixi.Date, e.BaslamaSaati));
                }
                catch { }

                // Ezamiyyət günü "Gecikmə" GÖSTƏRİLMİR: təsdiqlənmiş ezamiyyəti həmin günü
                // örtən işçidə gec giriş ezamiyyətdən qayıdışdır → Ezamiyyət. Bu düzəliş
                // KPI-lardan və filtrlərdən ƏVVƏL tətbiq olunur ki, say = siyahı qalsın
                // (CLAUDE.md qaydası). Köhnə (ADMS düzəlişindən əvvəl yazılmış) qeydləri də
                // örtür — bazaya toxunmur, yalnız göstərmə səviyyəsindədir.
                foreach (var rGec in umumi.Where(x => x.Status == DavamiyyetStatus.Gecikme))
                {
                    if (ezamiyyetList.Any(e => e.IsciId == rGec.IsciId &&
                                               e.Bas <= rGec.Tarix.Date && e.Bit >= rGec.Tarix.Date))
                        rGec.Status = DavamiyyetStatus.Ezamiyyet;
                }

                // tezCixanSayi — per-record data hesablandıqdan sonra doldurulur
                var tezCixanSayi = 0;

                // ── Tədbir (offline görüş) bağışlanması — həm Gecikmə, həm erkən çıxış ──────
                // İşçi həmin gün offline tədbir iştirakçısıdırsa:
                //  • giriş ≤ tədbir bitmə + tolerans → Gecikmə deyil (İşdə);
                //  • çıxış tədbir pəncərəsindədirsə (Bas−tol .. Bit+tol) → erkən çıxış deyil.
                // Mövcud qeydlərə də tətbiq olunur (ADMS mənbə qaydası ilə eyni).
                var gorushDict = new Dictionary<(int, DateTime), (TimeSpan Bas, TimeSpan Bit)>();
                try
                {
                    var gGorushB = (baslangic ?? tarix ?? DateTime.Today).Date;
                    var gGorushS = (son ?? tarix ?? DateTime.Today).Date;
                    var gorushlar = await _unitOfWork.Repository<GorushIshtirakci>().Query().AsNoTracking()
                        .Where(x => !x.Silinib && x.Gorush.Nov == GorushNovu.Offline
                                 && x.Gorush.Status != GorushStatus.LegvEdildi
                                 && x.Gorush.Tarix.Date >= gGorushB && x.Gorush.Tarix.Date <= gGorushS
                                 && x.Status != IshtirakciStatus.Redd
                                 && x.Status != IshtirakciStatus.IshtiraketmeyecekBildirib
                                 && x.Gorush.BitisSaati != null)
                        .Select(x => new { x.IsciId, Tarix = x.Gorush.Tarix.Date, Bas = x.Gorush.BaslamaSaati, Bit = x.Gorush.BitisSaati!.Value })
                        .ToListAsync();
                    foreach (var g in gorushlar)
                    {
                        var k = (g.IsciId, g.Tarix);
                        if (!gorushDict.TryGetValue(k, out var cur) || g.Bit > cur.Bit)
                            gorushDict[k] = (g.Bas, g.Bit);
                    }
                }
                catch { /* Görüş cədvəli yoxdursa keç */ }
                var gorushGecikTolerans = TimeSpan.FromMinutes(parametri.GecikmeToleransDeqiqe);
                bool TedbirBagislanir(Application.DTOs.HR.Davamiyyet.DavamiyyetListDto rec) =>
                    rec.Status == DavamiyyetStatus.Gecikme
                    && rec.GirisVaxti.HasValue
                    && gorushDict.TryGetValue((rec.IsciId, rec.Tarix.Date), out var b)
                    && rec.GirisVaxti.Value.TimeOfDay <= b.Bit + gorushGecikTolerans;

                // ── İcazəli KPI ilə DRILL-DOWN siyahısı EYNİ prinsiplə getməlidir ───────────
                // (CLAUDE.md: aqreqat say ≠ siyahı tələsi). İcazəli KPI İKİ qrupdan ibarətdir:
                //   (1) icazeliIndi — cihazda faktiki çıxıb icazədə olanlar (umumi-də var),
                //   (2) icazeGozleyen — təsdiqlənmiş icazəsi olan, amma həmin gün cihaz qeydi
                //       OLMAYAN işçilər (məs. işə gəlməyib, adından icazə yazılıb təsdiqlənib).
                // İkinci qrup `umumi`-də olmadığı üçün əvvəllər siyahıya düşmürdü → kart 1,
                // siyahı boş idi. Ona görə həmin qrup üçün sintetik sətir hazırlanır.
                var hedefTarixKpi = (tarix ?? baslangic ?? DateTime.Today).Date;
                var qeydliIdsKpi = new HashSet<int>(umumi.Where(x => x.Tarix.Date == hedefTarixKpi).Select(x => x.IsciId));
                var icazeGozleyenIds = icazeList
                    .Where(i => i.Tarix == hedefTarixKpi && !qeydliIdsKpi.Contains(i.IsciId) && !mezuniyyetIsciIds.Contains(i.IsciId))
                    .Select(i => i.IsciId).Distinct().ToList();

                // Ezamiyyət — EYNİ prinsip (say = siyahı): təsdiqlənmiş ezamiyyəti hədəf tarixi
                // örtən, amma həmin gün cihaz qeydi OLMAYAN işçilər (tam günlük ezamiyyətdə
                // işçi cihaza vurmur). Əvvəllər onlar "Gözlənilir"ə düşürdü, Ezamiyyət KPI 0 idi.
                var ezamiyyetGozleyenIds = ezamiyyetList
                    .Where(e => e.Bas <= hedefTarixKpi && e.Bit >= hedefTarixKpi
                             && !qeydliIdsKpi.Contains(e.IsciId)
                             && !mezuniyyetIsciIds.Contains(e.IsciId)
                             && !icazeGozleyenIds.Contains(e.IsciId))
                    .Select(e => e.IsciId).Distinct().ToList();

                // İcazəli filtri seçiləndə icazeGozleyen işçilər üçün sintetik sətir hazırla
                var icazeGozleyenRows = new List<Application.DTOs.HR.Davamiyyet.DavamiyyetListDto>();
                if (status.HasValue && status.Value == (int)DavamiyyetStatus.Icazeli && icazeGozleyenIds.Count > 0)
                {
                    var icazeGozleyenIsciler = await _unitOfWork.Repository<Isci>()
                        .Query().AsNoTracking()
                        .Where(x => icazeGozleyenIds.Contains(x.Id))
                        .Include(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                            .ThenInclude(t => t.Departament)
                        .ToListAsync();
                    icazeGozleyenRows = icazeGozleyenIsciler.Select(i =>
                    {
                        var esasTeyinat = i.IsciTeyinatlari.FirstOrDefault(t => t.Esasdir && !t.Silinib)
                                          ?? i.IsciTeyinatlari.FirstOrDefault(t => !t.Silinib);
                        return new Application.DTOs.HR.Davamiyyet.DavamiyyetListDto
                        {
                            Id = 0,
                            IsciId = i.Id,
                            IsciTamAd = i.Ad + " " + i.Soyad,
                            Tarix = hedefTarixKpi,
                            GirisVaxti = null,
                            CixisVaxti = null,
                            Status = DavamiyyetStatus.Icazeli,
                            DepartamentAd = esasTeyinat?.Departament?.Ad ?? "-",
                            MaasdanKes = false,
                            QayibSebebi = null
                        };
                    }).ToList();
                }

                // Ezamiyyət filtri seçiləndə ezamiyyetGozleyen işçilər üçün sintetik sətir (İcazəli pattern-i)
                var ezamiyyetGozleyenRows = new List<Application.DTOs.HR.Davamiyyet.DavamiyyetListDto>();
                if (status.HasValue && status.Value == (int)DavamiyyetStatus.Ezamiyyet && ezamiyyetGozleyenIds.Count > 0)
                {
                    var ezamiyyetGozleyenIsciler = await _unitOfWork.Repository<Isci>()
                        .Query().AsNoTracking()
                        .Where(x => ezamiyyetGozleyenIds.Contains(x.Id))
                        .Include(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                            .ThenInclude(t => t.Departament)
                        .ToListAsync();
                    ezamiyyetGozleyenRows = ezamiyyetGozleyenIsciler.Select(i =>
                    {
                        var esasTeyinat = i.IsciTeyinatlari.FirstOrDefault(t => t.Esasdir && !t.Silinib)
                                          ?? i.IsciTeyinatlari.FirstOrDefault(t => !t.Silinib);
                        return new Application.DTOs.HR.Davamiyyet.DavamiyyetListDto
                        {
                            Id = 0,
                            IsciId = i.Id,
                            IsciTamAd = i.Ad + " " + i.Soyad,
                            Tarix = hedefTarixKpi,
                            GirisVaxti = null,
                            CixisVaxti = null,
                            Status = DavamiyyetStatus.Ezamiyyet,
                            DepartamentAd = esasTeyinat?.Departament?.Ad ?? "-",
                            MaasdanKes = false,
                            QayibSebebi = null
                        };
                    }).ToList();
                }

                // status=4 (İcazəli) → "indi icazədə" + "icazə gözləyən" (sintetik);
                // status=6 (Ezamiyyət) → qeydli ezamiyyət + cihaz qeydi olmayan ezamiyyətlilər (sintetik);
                // digər statuslar adi filtr
                var result = (status.HasValue && status.Value == (int)DavamiyyetStatus.Icazeli)
                    ? umumi.Where(x => icazeliIndiIds.Contains(x.IsciId) && !mezuniyyetIsciIds.Contains(x.IsciId))
                           .Concat(icazeGozleyenRows).ToList()
                    : (status.HasValue && status.Value == (int)DavamiyyetStatus.Ezamiyyet)
                        ? umumi.Where(x => x.Status == DavamiyyetStatus.Ezamiyyet)
                               .Concat(ezamiyyetGozleyenRows).ToList()
                        : status.HasValue
                            ? umumi.Where(x => (int)x.Status == status.Value && !TedbirBagislanir(x)).ToList()
                            : umumi;

                // Nəticədəki bütün tarixlər üçün BayramGunu xüsusi bitmə vaxtlarını toplu çək
                var hedefTarixler = result.Select(x => x.Tarix.Date).Distinct().ToList();
                var bayramDict = new Dictionary<DateTime, TimeSpan>();
                try
                {
                    var bayramlar = await _unitOfWork.Repository<BayramGunu>()
                        .Query().AsNoTracking()
                        .Where(x => !x.Silinib && hedefTarixler.Contains(x.Tarix.Date) && x.XususiBitisVaxti.HasValue)
                        .ToListAsync();
                    bayramDict = bayramlar.ToDictionary(x => x.Tarix.Date, x => x.XususiBitisVaxti!.Value);
                }
                catch { /* BayramGunu cədvəli mövcud deyilsə keç */ }

                // ── Əlil işçi qısaldılmış Cümə qaydası (Tabel ilə eyni) ──────────────
                // Əlil işçi TAM 5 günlük həftənin 5-ci iş günü (Cümə) net ≥4 saat işləyibsə
                // erkən çıxış sayılmır — tabeldə həmin gün onsuz da 4 saat yazılır.
                var elilIsciIds = new HashSet<int>();
                var elilBayramSet = new HashSet<DateTime>();
                bool elilDataOk = false;
                try
                {
                    var hedefIsciler = result.Select(x => x.IsciId).Distinct().ToList();
                    var minHedef = hedefTarixler.Count > 0 ? hedefTarixler.Min() : DateTime.Today;
                    var maxHedef = hedefTarixler.Count > 0 ? hedefTarixler.Max() : DateTime.Today;
                    // Mərkəzi mənbə (EmekRejimiHelper) — HR/Rəhbər/User eyni datadan
                    (elilIsciIds, elilBayramSet) = await EmekRejimiHelper.ElilQisaldilmisDataAsync(
                        _unitOfWork, hedefIsciler, minHedef, maxHedef);
                    elilDataOk = true;
                }
                catch { elilDataOk = false; /* təhlükəsiz: yüklənmə alınmasa bağışlama YOX, erkən çıxış tutulur */ }

                var data = result.Select(x =>
                {
                    var gunCixis = bayramDict.TryGetValue(x.Tarix.Date, out var bv) ? bv : standartCixis;
                    var gunHedd = elanDict.TryGetValue(x.Tarix.Date, out var elanVaxt)
                        ? elanVaxt
                        : gunCixis - tezCixmaTolerans;

                    // Təsdiqlənmiş saat icazəsi / ezamiyyət erkən çıxışı bağışlayır:
                    //  - İcazə: çıxış icazənin başlanğıcından sonradır (Rəhbər paneli ilə EYNİ məntiq —
                    //    yarım günlük icazə də erkən çıxışı örtür; gün-sonu/BitisSaati tələbi YOXDUR)
                    //  - Ezamiyyət: həmin tarixi əhatə edir (saat varsa çıxış başlanğıc ətrafında, yoxdursa tam gün)
                    var cixisTod = x.CixisVaxti?.TimeOfDay ?? TimeSpan.Zero;
                    bool icazeOrtuyur = x.CixisVaxti.HasValue && icazeList.Any(i =>
                        i.IsciId == x.IsciId && i.Tarix == x.Tarix.Date &&
                        cixisTod >= i.Bas - tezCixmaTolerans);
                    bool ezamiyyetOrtuyur = x.CixisVaxti.HasValue && ezamiyyetList.Any(e =>
                        e.IsciId == x.IsciId && e.Bas <= x.Tarix.Date && e.Bit >= x.Tarix.Date &&
                        (e.BasSaat == null || cixisTod >= e.BasSaat.Value - tezCixmaTolerans));
                    // Tədbir (offline görüş): çıxış tədbir pəncərəsindədirsə (Bas−tol .. Bit+tol)
                    // erkən çıxış deyil — işçi tədbirə gedib/gəlib (Pattern A).
                    bool gorushOrtuyur = x.CixisVaxti.HasValue
                        && gorushDict.TryGetValue((x.IsciId, x.Tarix.Date), out var gw)
                        && cixisTod >= gw.Bas - tezCixmaTolerans
                        && cixisTod <= gw.Bit + tezCixmaTolerans;

                    // Nahar çıxılması — işçi nahar başlamadan gəlib, nahar bitdikdən sonra çıxıbsa.
                    // (Əlil 4 saat qaydası NET işləmə saatına baxdığı üçün tezCixan-dan ƏVVƏL hesablanır.)
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

                    // Əlil işçi qısaldılmış Cümə günü net ≥4 saat (240 dəq) işləyibsə erkən çıxış bağışlanır.
                    // elilDataOk false-dursa (yüklənmə xətası) bağışlama tətbiq olunmur → təhlükəsiz tərəf.
                    bool elilCumeBagisla = elilDataOk
                        && elilIsciIds.Contains(x.IsciId)
                        && EmekRejimiHelper.ElilQisaldilmisGun(x.Tarix.Date, elilBayramSet)
                        && (islemeSaatiDeq ?? 0) >= 240;

                    var tezCixanFlag = x.CixisVaxti.HasValue
                        && x.CixisVaxti.Value.TimeOfDay < gunHedd
                        && x.Status != DavamiyyetStatus.Ezamiyyet
                        && x.Status != DavamiyyetStatus.Icazeli
                        && !erkenIcazeSet.Contains((x.IsciId, x.Tarix.Date))
                        && !icazeOrtuyur
                        && !ezamiyyetOrtuyur
                        && !gorushOrtuyur
                        && !elilCumeBagisla;

                    // ── 17.08.2026-da BƏRPA: JS-in oxuduğu, amma serverin GÖNDƏRMƏDİYİ sahələr ──
                    // `isciId`, `erkenIcaze`, `cixisQirmizi`, `isSaatiQirmizi`, `isSaatiSebeb`
                    // 29.07.2026-da (5fb0b698) Rəhbər Davamiyyət dublikat səhifəsi silinərkən
                    // `ErkenCixisIcazeVer` action-ı ilə BİRLİKDƏ getdi. `hr-davamiyyet.js` isə
                    // onları oxumağa davam etdi və hamısı `undefined` oldu:
                    //   • `data-isci-id` BOŞ qalırdı → "erkən çıxışa icazə ver" düyməsi POST-u
                    //     isciId=0 ilə göndərirdi, servis "İşçi seçilməyib" qaytarırdı. JS yalnız
                    //     düyməni bərpa edir, mesaj göstərmir → rəhbər "vurdum" deyir, bazada
                    //     ErkenCixisIcaze qeydi olmur (real hadisə: 17.08.2026, Rafael Quliyev).
                    //   • `data-erken-icaze` həmişə "0" → icazə verilsə də tooltip yenə düymə göstərir.
                    //   • `data-issaati-sebeb` boş → "niyə qırmızı" izahı heç vaxt görünmürdü.
                    // QAYDA (CLAUDE.md): controller action silinəndə onu çağıran JS-i də yoxla —
                    // burada action bərpa olundu, amma onu QİDALANDIRAN data sahələri unudulmuşdu.
                    bool erkenIcazeVar = erkenIcazeSet.Contains((x.IsciId, x.Tarix.Date));

                    // Cari `tezCixanFlag` onsuz da bütün örtükləri (icazə/ezamiyyət/tədbir/
                    // fərdi icazə/əlil) nəzərə alır → köhnə `cixisQirmizi` ilə eynidir.
                    bool cixisQirmizi = tezCixanFlag;
                    var normaDeq = Math.Max(0,
                        (int)(gunCixis - parametri.StandartGirisVaxti).TotalMinutes - parametri.NaharMuddetDeqiqe);
                    bool isSaatiAz = islemeSaatiDeq.HasValue && islemeSaatiDeq.Value < normaDeq;
                    bool isSaatiQirmizi = isSaatiAz && cixisQirmizi;

                    string isSaatiSebeb;
                    if (!x.CixisVaxti.HasValue)
                        isSaatiSebeb = "";
                    else if (x.CixisVaxti.Value.TimeOfDay >= gunHedd)
                    {
                        var elanMetn = elanDict.TryGetValue(x.Tarix.Date, out var elanTs)
                            ? elanTs.ToString(@"hh\:mm")
                            : null;
                        isSaatiSebeb = elanMetn != null
                            ? "İş günü erkən bitirilib (" + elanMetn + ") — vaxtında gedib, erkən çıxış sayılmır."
                            : "Standart çıxış vaxtında gedib — erkən çıxış sayılmır.";
                    }
                    else if (erkenIcazeVar)
                        isSaatiSebeb = "Rəhbər fərdi erkən çıxış icazəsi verib — erkən çıxış sayılmır.";
                    else if (ezamiyyetOrtuyur || x.Status == DavamiyyetStatus.Ezamiyyet)
                        isSaatiSebeb = "Təsdiqlənmiş ezamiyyət var — erkən çıxış sayılmır.";
                    else if (icazeOrtuyur || x.Status == DavamiyyetStatus.Icazeli)
                        isSaatiSebeb = "Təsdiqlənmiş icazə var — çıxış icazə daxilindədir.";
                    else if (gorushOrtuyur)
                        isSaatiSebeb = "Tədbirdə (offline görüş) olub — çıxış tədbir pəncərəsindədir.";
                    else if (elilCumeBagisla)
                        isSaatiSebeb = "Əlil işçi qısaldılmış gündə normanı (4 saat) doldurub — erkən çıxış sayılmır.";
                    else
                    {
                        var cixisMetn = x.CixisVaxti.Value.TimeOfDay.ToString(@"hh\:mm");
                        var isaatPart = islemeSaatiDeq.HasValue
                            ? " İşlənmə: " + (islemeSaatiDeq.Value / 60) + " s " + (islemeSaatiDeq.Value % 60) + " d"
                              + (isSaatiAz ? " (norma " + (normaDeq / 60) + " s)." : ".")
                            : "";
                        // Örtməyən icazə də ola bilər: icazə çıxışdan SONRA başlayır.
                        var icazeBas = icazeList
                            .Where(i => i.IsciId == x.IsciId && i.Tarix == x.Tarix.Date)
                            .Select(i => (TimeSpan?)i.Bas)
                            .FirstOrDefault();
                        var icazeBasMetn = icazeBas.HasValue ? icazeBas.Value.ToString(@"hh\:mm") : null;
                        isSaatiSebeb = icazeBasMetn != null
                            ? "İcazə " + icazeBasMetn + "-dan başlayır, amma işçi ondan ƏVVƏL (" + cixisMetn + ") çıxıb." + isaatPart
                            : "İş günü bitməyib, erkən çıxıb (" + cixisMetn + ") — icazə/ezamiyyət/fərdi icazə yoxdur." + isaatPart;
                    }

                    return new
                    {
                        id = x.Id,
                        // KRİTİK: JS `data-isci-id` bundan doldurur — bu olmadan erkən çıxış
                        // icazəsi, "Qayıb yaz" və admin düzəlişi düymələri işləmir.
                        isciId = x.IsciId,
                        isciTamAd = x.IsciTamAd ?? "-",
                        departamentAd = x.DepartamentAd ?? "-",
                        tarix = x.Tarix,
                        girisVaxti = x.GirisVaxti,
                        // Tədbir pəncərəsindəki çıxış = tədbirdən qayıdış, əsl çıxış deyil → açıq göstər
                        cixisVaxti = gorushOrtuyur ? (DateTime?)null : x.CixisVaxti,
                        status = TedbirBagislanir(x) ? (int)DavamiyyetStatus.Isde : (int)x.Status,
                        maasdanKes = x.MaasdanKes,
                        qayibSebebi = x.QayibSebebi ?? "",
                        tezCixan = tezCixanFlag,
                        islemeSaatiDeq,
                        naharCixildi,
                        erkenIcaze = erkenIcazeVar,
                        cixisQirmizi,
                        isSaatiQirmizi,
                        isSaatiSebeb
                    };
                }).OrderByDescending(x => x.tarix).ThenBy(x => x.isciTamAd).ToList();

                // tezCixanSayi artıq per-record hesablandığı üçün buradan da götürürük
                tezCixanSayi = data.Count(x => x.tezCixan);

                // Stats — umumi üzərindən (məzuniyyətdəkilər İcazəli-dən çıxarılır)
                var gelib = umumi.Count(x => x.Status == DavamiyyetStatus.Isde || x.Status == DavamiyyetStatus.Gecikme);
                var gecikme = umumi.Count(x => x.Status == DavamiyyetStatus.Gecikme && !TedbirBagislanir(x));
                var qayib = umumi.Count(x => x.Status == DavamiyyetStatus.Qayib);
                // İcazəli = (indi fiziki icazədə olanlar) + (təsdiqlənmiş icazəsi olan, amma hələ
                // qeydə düşməyənlər — səhər/gözləyən icazə). Belə, kart gözlənilən siyahısındakı
                // "İcazəli" ilə uyğun olur (əvvəl bu ikinci qrup İcazəli sayılmırdı → kart 0 idi).
                // icazeGozleyenIds yuxarıda (result-dan əvvəl) hesablanıb — say ilə siyahı EYNİ mənbədən.
                var icazeGozleyenSayi = icazeGozleyenIds.Count;
                var icazeli = umumi.Count(x => icazeliIndiIds.Contains(x.IsciId) && !mezuniyyetIsciIds.Contains(x.IsciId)) + icazeGozleyenSayi;
                var xestelik = umumi.Count(x => x.Status == DavamiyyetStatus.Xestelik);
                // Ezamiyyət = qeydli ezamiyyət + cihaz qeydi olmayan ezamiyyətlilər (say = siyahı,
                // ezamiyyetGozleyenIds ilə EYNİ mənbədən — İcazəli KPI qaydasının eynisi).
                var ezamiyyet = umumi.Count(x => x.Status == DavamiyyetStatus.Ezamiyyet) + ezamiyyetGozleyenIds.Count;

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
                    .Where(x => x.Status == DavamiyyetStatus.Gecikme && !TedbirBagislanir(x))
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
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Rehber + "," + RoleNames.Admin)]
        public async Task<IActionResult> IsGunuBit()
        {
            try
            {
                var bugun = DateTime.Today;
                var bitisVaxti = DateTime.Now.TimeOfDay;

                var movcud = await _unitOfWork.Repository<IsGunuBitdiElan>()
                    .Query()
                    .Where(x => x.Tarix.Date == bugun && !x.Silinib)
                    .FirstOrDefaultAsync();

                if (movcud != null)
                {
                    movcud.BitisVaxti = bitisVaxti;
                    await _unitOfWork.Repository<IsGunuBitdiElan>().YenileAsync(movcud);
                }
                else
                {
                    await _unitOfWork.Repository<IsGunuBitdiElan>().YaratAsync(new IsGunuBitdiElan
                    {
                        Tarix = bugun,
                        BitisVaxti = bitisVaxti
                    });
                }
                await _unitOfWork.YaddaSaxlaAsync();

                var isciIds = await _unitOfWork.Repository<Isci>()
                    .Query().AsNoTracking()
                    .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                    .Select(x => x.Id)
                    .ToListAsync();

                foreach (var isciId in isciIds)
                {
                    await _bildirisRouter.NotifyIsciAsync(
                        isciId,
                        BildirisNovu.IsGunuBitdi,
                        "İş günü başa çatdı",
                        "İşini bitirən şəxslər gedə bilərlər.");
                }

                return Ok(new { ok = true, bitisVaxti = bitisVaxti.ToString(@"hh\:mm") });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, xeta = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetIsParametri()
        {
            try
            {
                var p = await GetIsParametriEntity();
                return Json(new
                {
                    id = p.Id,
                    girisVaxti = p.StandartGirisVaxti.ToString(@"hh\:mm"),
                    cixisVaxti = p.StandartCixisVaxti.ToString(@"hh\:mm"),
                    gecikmeTolerans = p.GecikmeToleransDeqiqe,
                    tezCixmaTolerans = p.TezCixmaToleransDeqiqe,
                    naharBaslamaSaati = p.NaharBaslamaSaati.ToString(@"hh\:mm"),
                    naharMuddetDeqiqe = p.NaharMuddetDeqiqe
                });
            }
            catch
            {
                return Json(new { id = 0, girisVaxti = "09:00", cixisVaxti = "17:45", gecikmeTolerans = 5, tezCixmaTolerans = 15, naharBaslamaSaati = "13:00", naharMuddetDeqiqe = 45 });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveIsParametri([FromBody] IsParametriDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { error = "Məlumat natamamdır." });

                if (!TimeSpan.TryParse(dto.GirisVaxti, out var giris) ||
                    !TimeSpan.TryParse(dto.CixisVaxti, out var cixis))
                    return BadRequest(new { error = "Vaxt formatı düzgün deyil (HH:mm)." });

                if (dto.GecikmeTolerans < 0 || dto.GecikmeTolerans > 60)
                    return BadRequest(new { error = "Gecikme toleransı 0-60 dəqiqə arasında olmalıdır." });

                if (dto.TezCixmaTolerans < 0 || dto.TezCixmaTolerans > 60)
                    return BadRequest(new { error = "Tez çıxma toleransı 0-60 dəqiqə arasında olmalıdır." });

                if (!TimeSpan.TryParse(dto.NaharBaslamaSaati, out var naharBaslama))
                    naharBaslama = new TimeSpan(13, 0, 0);

                if (dto.NaharMuddetDeqiqe < 0 || dto.NaharMuddetDeqiqe > 120)
                    return BadRequest(new { error = "Nahar müddəti 0-120 dəqiqə arasında olmalıdır." });

                var entity = await _unitOfWork.Repository<IsParametri>()
                    .Query()
                    .Where(x => !x.Silinib)
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    entity = new IsParametri
                    {
                        StandartGirisVaxti = giris,
                        StandartCixisVaxti = cixis,
                        GecikmeToleransDeqiqe = dto.GecikmeTolerans,
                        TezCixmaToleransDeqiqe = dto.TezCixmaTolerans,
                        NaharBaslamaSaati = naharBaslama,
                        NaharMuddetDeqiqe = dto.NaharMuddetDeqiqe
                    };
                    await _unitOfWork.Repository<IsParametri>().YaratAsync(entity);
                }
                else
                {
                    entity.StandartGirisVaxti = giris;
                    entity.StandartCixisVaxti = cixis;
                    entity.GecikmeToleransDeqiqe = dto.GecikmeTolerans;
                    entity.TezCixmaToleransDeqiqe = dto.TezCixmaTolerans;
                    entity.NaharBaslamaSaati = naharBaslama;
                    entity.NaharMuddetDeqiqe = dto.NaharMuddetDeqiqe;
                    entity.YenilenmeTarixi = DateTime.Now;
                }

                await _unitOfWork.YaddaSaxlaAsync();
                return Ok(new { message = "İş parametrləri yadda saxlandı." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Yadda saxlama xətası: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(DateTime? tarix, DateTime? baslangic, DateTime? son, int? isciId, int? status)
        {
            var result = await GetFilteredData(tarix, baslangic, son, isciId, status);
            var sorted = result.OrderByDescending(x => x.Tarix).ThenBy(x => x.IsciTamAd).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Davamiyyət");

            // Başlıq
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

        // ── Gözlənilən işçilər ──────────────────────────────────
        // Aktiv olan, amma göstərilən tarix üçün davamiyyət qeydi olmayan
        // işçilər. Bu gün üçün — hələ gəlməyənlər. Keçmiş tarix üçün — real
        // qayıblar (icazə/xəstəlik yoxdursa).
        [HttpGet]
        public async Task<IActionResult> GetGozlenilen(DateTime? tarix)
        {
            var hedef = (tarix ?? DateTime.Today).Date;

            // Aktiv işçilər
            var aktivIsciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .Include(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .ToListAsync();

            // Hədəf tarixdə qeydi olanların ID-ləri
            var qeydiOlanlar = await _unitOfWork.Repository<Davamiyyet>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Tarix.Date == hedef)
                .Select(x => x.IsciId)
                .ToListAsync();

            // Həmin gün üçün təsdiqlənmiş icazəsi olan işçilər
            var icazeliIsciIds = new HashSet<int>(
                await _unitOfWork.Repository<Icaze>()
                    .Query().AsNoTracking()
                    .Where(x => !x.Silinib
                             && x.Status == IcazeStatus.Tesdiqlenib
                             && x.IcazeTarixi.Date == hedef)
                    .Select(x => x.IsciId)
                    .ToListAsync());

            // Həmin günü örtən təsdiqlənmiş ezamiyyəti olan işçilər — onlar "Gözlənilir"/
            // "Qayıb" deyil, Ezamiyyət statusunda göstərilir (cihaz qeydi olmaması normaldır).
            var ezamiyyetdeIsciIds = new HashSet<int>(
                await _unitOfWork.Repository<EzamiyyetMuraciet>()
                    .Query().AsNoTracking()
                    .Where(x => !x.Silinib
                             && x.Status == EzamiyyetStatus.Tesdiqlendi
                             && x.BaslamaTarixi.Date <= hedef
                             && x.BitmeTarixi.Date >= hedef)
                    .Select(x => x.IsciId)
                    .ToListAsync());

            // Həmin gün offline tədbirdə (görüşdə) olan işçilər → "Tədbirdə" göstərilir.
            // (QayibMarkerBackgroundService ilə eyni məntiq: offline, ləğv olunmamış,
            //  iştirakçı Redd/İştiraketməyəcək deyil.) Yalnız görüntü — heç bir yazı yoxdur.
            var tedbirDict = new Dictionary<int, (string Ad, TimeSpan Saat)>();
            try
            {
                var tedbirler = await _unitOfWork.Repository<GorushIshtirakci>()
                    .Query().AsNoTracking()
                    .Where(x => !x.Silinib
                             && x.Gorush.Nov == GorushNovu.Offline
                             && x.Gorush.Status != GorushStatus.LegvEdildi
                             && x.Gorush.Tarix.Date == hedef
                             && x.Status != IshtirakciStatus.Redd
                             && x.Status != IshtirakciStatus.IshtiraketmeyecekBildirib)
                    .Select(x => new { x.IsciId, x.Gorush.Bashliq, x.Gorush.BaslamaSaati })
                    .ToListAsync();
                tedbirDict = tedbirler
                    .GroupBy(x => x.IsciId)
                    .ToDictionary(g => g.Key, g =>
                    {
                        var f = g.OrderBy(x => x.BaslamaSaati).First();
                        return (f.Bashliq, f.BaslamaSaati);
                    });
            }
            catch { /* Görüş cədvəli yoxdursa keç */ }

            var gozlenilenler = aktivIsciler
                .Where(i => !qeydiOlanlar.Contains(i.Id))
                .Select(i =>
                {
                    var esasTeyinat = i.IsciTeyinatlari
                        .Where(t => t.Esasdir && !t.Silinib)
                        .FirstOrDefault()
                        ?? i.IsciTeyinatlari.FirstOrDefault(t => !t.Silinib);
                    // İcazəli > Ezamiyyət > Tədbirdə > (Qayib/Gözlənilir). Tədbirdə = sintetik status 100.
                    int st;
                    string? tedAd = null, tedSaat = null;
                    if (icazeliIsciIds.Contains(i.Id)) st = 4;                    // İcazəli
                    else if (ezamiyyetdeIsciIds.Contains(i.Id)) st = 6;           // Ezamiyyət
                    else if (tedbirDict.TryGetValue(i.Id, out var ted))           // Tədbirdə
                    {
                        st = 100;
                        tedAd = ted.Ad;
                        tedSaat = ted.Saat.ToString(@"hh\:mm");
                    }
                    else st = (hedef < DateTime.Today ? 3 : 0);                   // Qayib / Gözlənilir
                    return new
                    {
                        id = 0,
                        isciId = i.Id,
                        isciTamAd = i.Ad + " " + i.Soyad,
                        departamentAd = esasTeyinat?.Departament?.Ad ?? "-",
                        tarix = hedef,
                        girisVaxti = (DateTime?)null,
                        cixisVaxti = (DateTime?)null,
                        status = st,
                        tedbirAd = tedAd,
                        tedbirSaat = tedSaat
                    };
                })
                .OrderBy(x => x.isciTamAd)
                .ToList();

            // İcazəli (4) və Ezamiyyət (6) "gözlənilir" sayılmır — öz KPI-lərində sayılırlar.
            var yalnizGozleyen = gozlenilenler.Where(x => x.status != 4 && x.status != 6).ToList();

            return Json(new
            {
                records = yalnizGozleyen,
                count = yalnizGozleyen.Count,
                tedbirdeCount = yalnizGozleyen.Count(x => x.status == 100),
                tarix = hedef
            });
        }

        // ── Məzuniyyətdə olan işçilər ─────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetMezuniyyetler(DateTime? tarix)
        {
            var hedef = (tarix ?? DateTime.Today).Date;

            var mezuniyyetler = await _unitOfWork.Repository<Mezuniyyet>()
                .Query()
                .AsNoTracking()
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
                    .Where(t => t.Esasdir && !t.Silinib)
                    .FirstOrDefault()
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

        [HttpPost]
        public async Task<IActionResult> QayibDuzelt([FromBody] QayibDuzeltRequest req)
        {
            if (req == null || req.Id <= 0)
                return BadRequest(new { error = "Məlumat natamamdır." });

            var entity = await _unitOfWork.Repository<Davamiyyet>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == req.Id && !x.Silinib && x.Status == DavamiyyetStatus.Qayib);

            if (entity == null)
                return BadRequest(new { error = "Qayıb qeydi tapılmadı." });

            var maasOdenilib = await _unitOfWork.Repository<Maas>()
                .Query()
                .AnyAsync(m => !m.Silinib && m.IsciId == entity.IsciId
                    && m.Il == entity.Tarix.Year && m.Ay == entity.Tarix.Month
                    && m.Status == MaasStatus.Odenildi);

            if (maasOdenilib)
                return BadRequest(new { error = "Bu ayın maaşı artıq ödənildiyi üçün dəyişiklik edilə bilməz." });

            entity.MaasdanKes = req.MaasdanKes;
            entity.QayibSebebi = req.QayibSebebi?.Trim();
            await _unitOfWork.YaddaSaxlaAsync();

            return Ok(new { message = "Yeniləndi." });
        }

        [HttpPost]
        public async Task<IActionResult> QayibYaz([FromBody] QayibYazRequest req)
        {
            if (req == null || req.IsciId <= 0)
                return BadRequest(new { error = "Məlumat natamamdır." });

            var tarix = req.Tarix.Date;

            var maasOdenilib = await _unitOfWork.Repository<Maas>()
                .Query()
                .AnyAsync(m => !m.Silinib && m.IsciId == req.IsciId
                    && m.Il == tarix.Year && m.Ay == tarix.Month
                    && m.Status == MaasStatus.Odenildi);

            if (maasOdenilib)
                return BadRequest(new { error = "Bu ayın maaşı artıq ödənildiyi üçün dəyişiklik edilə bilməz." });

            var movcut = await _davamiyyetService.BuGunMovcuddurmuAsync(req.IsciId, tarix);
            if (movcut)
                return BadRequest(new { error = "Bu tarix üçün davamiyyət qeydi artıq mövcuddur." });

            var dto = new Application.DTOs.HR.Davamiyyet.DavamiyyetCreateDto
            {
                IsciId = req.IsciId,
                Tarix = tarix,
                Status = DavamiyyetStatus.Qayib,
                MaasdanKes = req.MaasdanKes,
                QayibSebebi = req.QayibSebebi?.Trim()
            };

            var result = await _davamiyyetService.YaratAsync(dto);
            if (!result.Success)
                return BadRequest(new { error = result.Message });

            if (result.Data != null)
                await _teklifService.DavamiyyetYoxlaAsync(result.Data.Id);

            return Ok(new { message = "Qayıb uğurla qeyd edildi." });
        }

        // ── POST: Erkən çıxış icazəsi ver (YALNIZ bugün üçün) ──────────────
        // İşçiyə bugünə icazə verilir → ErkenCixisIcaze qeydi yaranır və davamiyyət
        // həmin günü "tez çıxdı" saymır (HR/Davamiyyet:414, ADMS:398, User:283).
        //
        // TARİXÇƏ: bu action əvvəl RehberDashboardController-də idi. 29.07.2026-da
        // (5fb0b698) Rəhbər Davamiyyət dublikat səhifəsi silinərkən action da onunla
        // birlikdə getdi, amma düymə `hr-davamiyyet.js`-də qaldı — nəticədə düymə
        // görünürdü, POST isə boş ünvana gedirdi və heç nə yazılmırdı. Rəhbər
        // əvvəllər verə bilirdi, sonra heç kim verə bilmədi. 14.08.2026-da bərpa
        // olundu, indi vahid səhifədədir.
        //
        // Rol: controller səviyyəsindəki [Authorize] — HR + Admin + Rəhbər.
        // Ayrıca məhdudiyyət qoyulmadı, çünki səhifənin özü onsuz da bu üç rola açıqdır
        // və icazəni praktikada rəhbər/HR verir.
        //
        // YALNIZ BUGÜN: keçmiş günə icazə vermək intizam qeydini geriyə dəyişmək
        // deməkdir — orijinal davranış saxlanılır (kommit c75bbd2b).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ErkenCixisIcazeVer(int isciId)
        {
            if (isciId <= 0)
                return Json(new { success = false, message = "İşçi seçilməyib." });

            try
            {
                var bugun = DateTime.Today;

                // İdempotent — bu gün üçün artıq icazə varsa təkrar yaratma
                var movcud = await _unitOfWork.Repository<ErkenCixisIcaze>()
                    .Query()
                    .AnyAsync(x => !x.Silinib && x.IsciId == isciId && x.Tarix.Date == bugun);
                if (movcud)
                    return Json(new { success = true, message = "Artıq verilib." });

                // İcazəni verən — cari istifadəçinin işçisi. Tapılmasa 0 qalır
                // (məs. admin hesabı işçiyə bağlı deyil) və bu, icazəni BLOKLAMIR.
                var verenIsciId = await CariIsciIdAsync() ?? 0;

                await _unitOfWork.Repository<ErkenCixisIcaze>().YaratAsync(new ErkenCixisIcaze
                {
                    IsciId = isciId,
                    Tarix = bugun,
                    IcazeVerenIsciId = verenIsciId
                });
                await _unitOfWork.YaddaSaxlaAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ErkenCixisIcazeVer xətası — isciId={IsciId}", isciId);
                return Json(new { success = false, message = "Xəta baş verdi." });
            }
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> AdminDavamiyyetDuzelt([FromBody] AdminDuzeltRequest req)
        {
            if (req == null || req.Id <= 0)
                return BadRequest(new { error = "Məlumat natamamdır." });

            var entity = await _unitOfWork.Repository<Davamiyyet>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == req.Id && !x.Silinib);

            if (entity == null)
                return BadRequest(new { error = "Davamiyyət qeydi tapılmadı." });

            if (!string.IsNullOrWhiteSpace(req.GirisVaxti) && TimeSpan.TryParse(req.GirisVaxti, out var girisTs))
                entity.GirisVaxti = entity.Tarix.Date.Add(girisTs);
            else if (string.IsNullOrWhiteSpace(req.GirisVaxti))
                entity.GirisVaxti = null;

            if (!string.IsNullOrWhiteSpace(req.CixisVaxti) && TimeSpan.TryParse(req.CixisVaxti, out var cixisTs))
                entity.CixisVaxti = entity.Tarix.Date.Add(cixisTs);
            else if (string.IsNullOrWhiteSpace(req.CixisVaxti))
                entity.CixisVaxti = null;

            if (Enum.IsDefined(typeof(DavamiyyetStatus), req.Status))
                entity.Status = (DavamiyyetStatus)req.Status;

            entity.MaasdanKes = req.MaasdanKes;
            entity.QayibSebebi = req.QayibSebebi?.Trim();
            entity.YenilenmeTarixi = DateTime.Now;

            await _unitOfWork.YaddaSaxlaAsync();
            return Ok(new { message = "Davamiyyət qeydi yeniləndi." });
        }

        [HttpPost]
        public async Task<IActionResult> QayibSil([FromBody] QayibSilRequest req)
        {
            if (req == null || req.Id <= 0)
                return BadRequest(new { error = "Məlumat natamamdır." });

            var entity = await _unitOfWork.Repository<Davamiyyet>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == req.Id && !x.Silinib && x.Status == DavamiyyetStatus.Qayib);

            if (entity == null)
                return BadRequest(new { error = "Qayıb qeydi tapılmadı." });

            var maasOdenilib = await _unitOfWork.Repository<Maas>()
                .Query()
                .AnyAsync(m => !m.Silinib && m.IsciId == entity.IsciId
                    && m.Il == entity.Tarix.Year && m.Ay == entity.Tarix.Month
                    && m.Status == MaasStatus.Odenildi);

            if (maasOdenilib)
                return BadRequest(new { error = "Bu ayın maaşı artıq ödənildiyi üçün silinmə edilə bilməz." });

            entity.Silinib = true;
            await _unitOfWork.YaddaSaxlaAsync();

            return Ok(new { message = "Qayıb qeydi silindi." });
        }

        [HttpGet]
        public async Task<IActionResult> IsciAxtar(string q)
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

        // ── Helper: İsParametri — mövcuddursa yüklə, yoxdursa (cədvəl olmasa belə) default qaytır ──
        private async Task<IsParametri> GetIsParametriEntity()
        {
            try
            {
                var entity = await _unitOfWork.Repository<IsParametri>()
                    .Query().AsNoTracking()
                    .Where(x => !x.Silinib)
                    .FirstOrDefaultAsync();
                return entity ?? new IsParametri();
            }
            catch
            {
                return new IsParametri();
            }
        }

        // ── Helper: shared filtering logic ──
        private async Task<IList<Application.DTOs.HR.Davamiyyet.DavamiyyetListDto>> GetFilteredData(
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

    public class AdminDuzeltRequest
    {
        public int Id { get; set; }
        public string? GirisVaxti { get; set; }
        public string? CixisVaxti { get; set; }
        public int Status { get; set; }
        public bool MaasdanKes { get; set; }
        public string? QayibSebebi { get; set; }
    }

    public class QayibDuzeltRequest
    {
        public int Id { get; set; }
        public bool MaasdanKes { get; set; }
        public string? QayibSebebi { get; set; }
    }

    public class QayibYazRequest
    {
        public int IsciId { get; set; }
        public DateTime Tarix { get; set; }
        public bool MaasdanKes { get; set; }
        public string? QayibSebebi { get; set; }
    }

    public class QayibSilRequest
    {
        public int Id { get; set; }
    }

    public class IsParametriDto
    {
        public string GirisVaxti { get; set; } = "09:00";
        public string CixisVaxti { get; set; } = "17:45";
        public int GecikmeTolerans { get; set; } = 5;
        public int TezCixmaTolerans { get; set; } = 15;
        public string NaharBaslamaSaati { get; set; } = "13:00";
        public int NaharMuddetDeqiqe { get; set; } = 45;
    }
}
