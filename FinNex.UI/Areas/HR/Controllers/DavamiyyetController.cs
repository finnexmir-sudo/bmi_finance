using ClosedXML.Excel;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
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

        public DavamiyyetController(
            IDavamiyyetService davamiyyetService,
            IIsciService isciService,
            IUnitOfWork unitOfWork,
            IJetonTeklifleriService teklifService,
            IBildirisRouter bildirisRouter)
        {
            _davamiyyetService = davamiyyetService;
            _isciService = isciService;
            _unitOfWork = unitOfWork;
            _teklifService = teklifService;
            _bildirisRouter = bildirisRouter;
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

                // tezCixanSayi — per-record data hesablandıqdan sonra doldurulur
                var tezCixanSayi = 0;

                var result = status.HasValue
                    ? umumi.Where(x => (int)x.Status == status.Value).ToList()
                    : umumi;

                // status=4 (İcazəli) filtri zamanı məzuniyyətdəkiləri çıxar
                if (status.HasValue && status.Value == (int)DavamiyyetStatus.Icazeli)
                    result = result.Where(x => !mezuniyyetIsciIds.Contains(x.IsciId)).ToList();

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

                var data = result.Select(x =>
                {
                    var gunCixis = bayramDict.TryGetValue(x.Tarix.Date, out var bv) ? bv : standartCixis;
                    var gunHedd = elanDict.TryGetValue(x.Tarix.Date, out var elanVaxt)
                        ? elanVaxt
                        : gunCixis - tezCixmaTolerans;
                    var tezCixanFlag = x.CixisVaxti.HasValue && x.CixisVaxti.Value.TimeOfDay < gunHedd;

                    // Nahar çıxılması — işçi nahar başlamadan gəlib, nahar bitdikdən sonra çıxıbsa
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

                // tezCixanSayi artıq per-record hesablandığı üçün buradan da götürürük
                tezCixanSayi = data.Count(x => x.tezCixan);

                // Stats — umumi üzərindən (məzuniyyətdəkilər İcazəli-dən çıxarılır)
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
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Rehber)]
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
                await _unitOfWork.SaveChangesAsync();

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

            var gozlenilenler = aktivIsciler
                .Where(i => !qeydiOlanlar.Contains(i.Id))
                .Select(i =>
                {
                    var esasTeyinat = i.IsciTeyinatlari
                        .Where(t => t.Esasdir && !t.Silinib)
                        .FirstOrDefault()
                        ?? i.IsciTeyinatlari.FirstOrDefault(t => !t.Silinib);
                    return new
                    {
                        id = 0,
                        isciId = i.Id,
                        isciTamAd = i.Ad + " " + i.Soyad,
                        departamentAd = esasTeyinat?.Departament?.Ad ?? "-",
                        tarix = hedef,
                        girisVaxti = (DateTime?)null,
                        cixisVaxti = (DateTime?)null,
                        // Keçmiş gün üçün = Qayıb (3), bu gün üçün = Gözlənilir (0 virtual)
                        status = hedef < DateTime.Today ? 3 : 0
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
