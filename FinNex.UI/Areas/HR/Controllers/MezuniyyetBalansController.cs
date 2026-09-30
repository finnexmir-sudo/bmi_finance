using ClosedXML.Excel;
using FinNex.Application.Common.Extensions;
using FinNex.Application.Services.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class MezuniyyetBalansController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMezuniyyetHuquqService _huquqService;

        public MezuniyyetBalansController(IUnitOfWork unitOfWork, IMezuniyyetHuquqService huquqService)
        {
            _unitOfWork = unitOfWork;
            _huquqService = huquqService;
        }

        // GET /HR/MezuniyyetBalans/Yoxlama?tarix=2026-07-03
        // Sistemin hesabladığı illik hüquq (əsas + staj + uşaq) — YALNIZ OXUMA.
        // Bazaya yazmır; HR rəqəmləri real data ilə tutuşdurur (Addım 2 yoxlaması).
        public async Task<IActionResult> Yoxlama(DateTime? tarix)
        {
            var refTarix = (tarix ?? DateTime.Today).Date;
            ViewBag.Tarix = refTarix;
            var data = await _huquqService.HesablaAsync(refTarix);
            ViewData["Title"] = "Məzuniyyət hüququ — yoxlama";
            return View(data);
        }

        // GET /HR/MezuniyyetBalans?il=2026
        public async Task<IActionResult> Index(int? il)
        {
            var cariIl = il ?? DateTime.Now.Year;
            ViewBag.SecilmisIl = cariIl;

            // İllərin siyahısı (dropdown üçün)
            var illerSiyahisi = await _unitOfWork.Repository<MezuniyyetBalans>()
                .Query()
                .Where(b => !b.Silinib)
                .Select(b => b.Il)
                .Distinct()
                .OrderByDescending(i => i)
                .ToListAsync();

            if (!illerSiyahisi.Contains(cariIl))
                illerSiyahisi.Insert(0, cariIl);

            ViewBag.Iller = illerSiyahisi;

            // Aktiv işçilər — cari il balansları + əvvəlki illərin İllik qalıqları
            // (əvvəlki illərdən qalan günlərin tarixçəsini göstərmək üçün)
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .Include(x => x.MezuniyyetBalanslari.Where(b => !b.Silinib && b.Il == cariIl))
                // «Cari təyinat» = Aktivdir, `BitmeTarixi == null` YOX.
                // BitmeTarixi PLANLAŞDIRILMIŞ bitmə tarixidir; redaktədə yazılır və
                // sətir `Aktivdir=1` VƏ `BitmeTarixi=<tarix>` qalır (bazada 29-dan 22-si
                // belədir). Köhnə şərtlə işçilərin çoxunda Departament «—» görünürdü.
                .Include(x => x.IsciTeyinatlari.Where(t => t.Aktivdir && !t.Silinib))
                    .ThenInclude(t => t.Departament)
                // Sıralama qaydası: HR-ın «İşçi Sıralaması» səhifəsində verdiyi `Sira`
                // əsasdır; ad/soyad əlifbası yalnız eyni `Sira` daxilində işləyir.
                .OrderBy(x => x.Sira)
                .ThenBy(x => x.Ad)
                .ThenBy(x => x.Soyad)
                .ToListAsync();

            // Bütün illərin balansı (həm keçmiş, həm cari) — yan sütunda
            // illər üzrə qalıq göstərmək üçün.
            // DİQQƏT: AsNoTracking() VACİBDİR. Bu sorğu tracking ilə işləsəydi,
            // EF Core "relationship fixup" bütün illərin balanslarını yuxarıdakı
            // isciler-in MezuniyyetBalanslari naviqasiyasına əlavə edər və
            // filtered Include (b.Il == cariIl) effektiv olmazdı — nəticədə view
            // əvvəlki ilin balansını cari il kimi göstərərdi.
            var isciIds = isciler.Select(i => i.Id).ToList();
            var butunBalanslar = await _unitOfWork.Repository<MezuniyyetBalans>()
                .Query()
                .AsNoTracking()
                .Where(b => !b.Silinib
                         && b.Nov == MezuniyyetNovu.Illik
                         && isciIds.Contains(b.IsciId))
                .OrderByDescending(b => b.Il)
                .ToListAsync();

            // { isciId: [ (il, qaliq), ... ] } — həm cari həm keçmiş illər daxildir
            // (yalnız qaliq > 0 olanlar, amma cari il həmişə göstərilir — "hələ
            //  verilməyib" informasiyası üçün).
            ViewBag.IllerUzreQaliq = butunBalanslar
                .Where(b => (b.ToplamGun - b.IstifadeOlunanGun) > 0 || b.Il == cariIl)
                .GroupBy(b => b.IsciId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(b => (
                        Il: b.Il,
                        Qaliq: b.ToplamGun - b.IstifadeOlunanGun,
                        Cari: b.Il == cariIl
                    )).OrderByDescending(x => x.Il).ToList()
                );

            // ── «Limit 14 gün» (Ə.M. md.137) ────────────────────────────────
            // Əsas məzuniyyətin hissələrindən BİRİ ən azı 14 gün olmalıdır. Ona
            // görə hər iş ilində birdəfəlik ≥14 günlük məzuniyyət GÖTÜRÜLMƏYİBSƏ,
            // həmin ilin qalığından 14 gün «ehtiyat» sayılır və sərbəst hissə
            // (qalıq − 14) göstərilir.
            //
            // ⚠️ Bu YALNIZ GÖSTƏRİŞDİR — balansdan heç nə çıxılmır, heç bir sorğu
            //    bu rəqəmə görə bloklanmır (istifadəçi qərarı 09.09.2026).
            // ⚠️ Excel («Cari qalıq» vərəqi, L sütunu) 14-ü HƏMİŞƏ çıxırdı — ≥14
            //    günlük məzuniyyət artıq götürülübsə də. Bu SƏHVDİR və qəsdən
            //    təkrarlanmır (istifadəçi təsdiqi: «mənim qaydam, excel səhvdir»).
            var uzunMezuniyyetler = await _unitOfWork.Repository<Mezuniyyet>()
                .Query()
                .AsNoTracking()
                .Where(m => !m.Silinib
                         && m.Nov == MezuniyyetNovu.Illik
                         && m.Status == MezuniyyetStatus.Tesdiqlenib
                         && isciIds.Contains(m.IsciId))
                // EfektivGunSayi [NotMapped]-dir — SQL-ə tərcümə olunmur, ona görə
                // xam sahələr gətirilir və gün sayı yaddaşda hesablanır.
                .Select(m => new
                {
                    m.IsciId,
                    m.BaslamaTarixi,
                    m.IsGunlerininSayi,
                    m.IsGunlerininSayiManual
                })
                .ToListAsync();

            var iseQebulMap = isciler.ToDictionary(i => i.Id, i => i.IsheQebulTarixi.Date);

            // { isciId: { iş ili, ... } } — həmin iş ilində ≥14 günlük birdəfəlik
            // məzuniyyət götürülüb.
            var onDordGunVar = new Dictionary<int, HashSet<int>>();
            foreach (var m in uzunMezuniyyetler)
            {
                int gun = m.IsGunlerininSayiManual ?? m.IsGunlerininSayi;
                if (gun < MinBirdefelikGun) continue;
                if (!iseQebulMap.TryGetValue(m.IsciId, out var qebul)) continue;

                // Məzuniyyət iki iş ilinə də düşə bilər (balans FIFO kəsir), amma
                // «birdəfəlik 14 gün» tələbi fasiləsizlik haqqındadır — qeyd
                // BAŞLADIĞI iş ilinə yazılır.
                int isIli = IsIliniTap(qebul, m.BaslamaTarixi);
                if (!onDordGunVar.TryGetValue(m.IsciId, out var set))
                    onDordGunVar[m.IsciId] = set = new HashSet<int>();
                set.Add(isIli);
            }

            ViewBag.OnDordGunVar = onDordGunVar;
            ViewBag.MinBirdefelikGun = MinBirdefelikGun;

            return View(isciler);
        }

        /// <summary>Ə.M. md.137 — əsas məzuniyyətin bir hissəsinin minimum müddəti.</summary>
        private const int MinBirdefelikGun = 14;

        /// <summary>
        /// Verilmiş tarixin hansı İŞ İLİNƏ düşdüyünü qaytarır. İş ili təqvim ili
        /// deyil — işə qəbul ildönümündən başlayır (2026-02-02 işə qəbul → 2026 iş ili
        /// 02.02.2026-dan 01.02.2027-yə qədərdir).
        ///
        /// ⚠️ Index.cshtml-dəki `SonIldonum` ilə EYNİ qaydadır (29 fevral kimi hüdud
        /// hallarında ayın son gününə clamp). Birini dəyişəndə o birini də dəyiş —
        /// yoxsa sütundakı il ilə limitin ili sürüşər və heç bir xəta çıxmaz.
        /// </summary>
        private static int IsIliniTap(DateTime iseQebul, DateTime tarix)
        {
            var t = tarix.Date;
            int ay = iseQebul.Month;
            int gun = Math.Min(iseQebul.Day, DateTime.DaysInMonth(t.Year, ay));
            var buIlDonum = new DateTime(t.Year, ay, gun);
            return t >= buIlDonum ? t.Year : t.Year - 1;
        }

        // POST /HR/MezuniyyetBalans/Update
        [HttpPost]
        public async Task<IActionResult> Update(int id, int toplamGun)
        {
            try
            {
                var balans = await _unitOfWork.Repository<MezuniyyetBalans>()
                    .IdIleGetirAsync(id);

                if (balans == null)
                    return Json(new { success = false, message = "Balans tapılmadı." });

                if (toplamGun < 0)
                    return Json(new { success = false, message = "Toplam gün mənfi ola bilməz." });

                if (toplamGun < balans.IstifadeOlunanGun)
                    return Json(new { success = false, message = "Toplam gün istifadə olunan gündən az ola bilməz." });

                balans.ToplamGun = toplamGun;
                balans.YenilenmeTarixi = DateTime.Now;

                await _unitOfWork.Repository<MezuniyyetBalans>().YenileAsync(balans);
                await _unitOfWork.YaddaSaxlaAsync();

                return Json(new
                {
                    success = true,
                    message = "Balans yeniləndi.",
                    qaliqGun = balans.QaliqGun
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Xəta: {ex.Message}" });
            }
        }

        // POST /HR/MezuniyyetBalans/CreateOrUpdate — fərdi işçi üçün balans yarat/yenilə
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(int isciId, int il, int illikGun, int xestelikGun, int ezamiyyetGun)
        {
            try
            {
                var repo = _unitOfWork.Repository<MezuniyyetBalans>();
                var novler = new[]
                {
                    (nov: MezuniyyetNovu.Illik, gun: illikGun),
                    (nov: MezuniyyetNovu.Xestelik, gun: xestelikGun),
                    (nov: MezuniyyetNovu.Ezamiyyet, gun: ezamiyyetGun)
                };

                foreach (var (nov, gun) in novler)
                {
                    if (gun < 0) continue;

                    var movcud = await repo.GetirAsync(x =>
                        x.IsciId == isciId && x.Il == il && x.Nov == nov && !x.Silinib);

                    if (movcud != null)
                    {
                        if (gun < movcud.IstifadeOlunanGun)
                            return Json(new { success = false, message = $"{nov} üçün toplam gün istifadə olunandan az ola bilməz." });

                        movcud.ToplamGun = gun;
                        movcud.YenilenmeTarixi = DateTime.Now;
                        await repo.YenileAsync(movcud);
                    }
                    else
                    {
                        await repo.YaratAsync(new MezuniyyetBalans
                        {
                            IsciId = isciId,
                            Il = il,
                            Nov = nov,
                            ToplamGun = gun,
                            IstifadeOlunanGun = 0
                        });
                    }
                }

                await _unitOfWork.YaddaSaxlaAsync();
                return Json(new { success = true, message = "Balans yeniləndi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Xəta: {ex.Message}" });
            }
        }

        // GET /HR/MezuniyyetBalans/IsciBalanslari?isciId=5
        // Bir işçinin bütün illərin balansını qaytarır — modal redaktə üçün.
        [HttpGet]
        public async Task<IActionResult> IsciBalanslari(int isciId)
        {
            try
            {
                var isci = await _unitOfWork.Repository<Isci>().IdIleGetirAsync(isciId);
                if (isci == null) return Json(new { success = false, message = "İşçi tapılmadı." });

                var balanslar = await _unitOfWork.Repository<MezuniyyetBalans>()
                    .Query()
                    .Where(b => !b.Silinib && b.IsciId == isciId)
                    .OrderByDescending(b => b.Il)
                    .ThenBy(b => b.Nov)
                    .ToListAsync();

                var data = balanslar.Select(b => new
                {
                    id = b.Id,
                    il = b.Il,
                    nov = (int)b.Nov,
                    novAd = b.Nov.ToString(),
                    toplamGun = b.ToplamGun,
                    istifade = b.IstifadeOlunanGun,
                    qaliq = b.ToplamGun - b.IstifadeOlunanGun
                }).ToList();

                return Json(new
                {
                    success = true,
                    isciAd = $"{isci.Ad} {isci.Soyad}",
                    balanslar = data
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Xəta: {ex.Message}" });
            }
        }

        // POST /HR/MezuniyyetBalans/AddIllikBalans
        // Müəyyən il üçün Əmək məzuniyyəti balansı yaradır (və ya artıq varsa
        // Toplam günü üstə gəlir). Bu HR-a keçmiş illərdə unudulmuş və ya
        // əl ilə əlavə edilmək istənilən günləri yazmağa imkan verir.
        [HttpPost]
        public async Task<IActionResult> AddIllikBalans(int isciId, int il, int toplamGun)
        {
            try
            {
                if (toplamGun <= 0)
                    return Json(new { success = false, message = "Toplam gün 0-dan böyük olmalıdır." });
                if (il < 2000 || il > DateTime.Now.Year + 5)
                    return Json(new { success = false, message = "İl düzgün deyil." });

                var repo = _unitOfWork.Repository<MezuniyyetBalans>();
                var movcud = await repo.GetirAsync(x =>
                    x.IsciId == isciId && x.Il == il && x.Nov == MezuniyyetNovu.Illik && !x.Silinib);

                if (movcud != null)
                {
                    // Artıq var — üstə gəl
                    movcud.ToplamGun += toplamGun;
                    movcud.YenilenmeTarixi = DateTime.Now;
                    await repo.YenileAsync(movcud);
                    await _unitOfWork.YaddaSaxlaAsync();
                    return Json(new { success = true, message = $"{il}-ci il balansına {toplamGun} gün əlavə edildi. Cəmi: {movcud.ToplamGun} gün." });
                }

                // Yenisi
                await repo.YaratAsync(new MezuniyyetBalans
                {
                    IsciId = isciId,
                    Il = il,
                    Nov = MezuniyyetNovu.Illik,
                    ToplamGun = toplamGun,
                    IstifadeOlunanGun = 0
                });
                await _unitOfWork.YaddaSaxlaAsync();
                return Json(new { success = true, message = $"{il}-ci il üçün {toplamGun} günlük balans yaradıldı." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Xəta: {ex.Message}" });
            }
        }

        // POST /HR/MezuniyyetBalans/YeniIlBalansYarat
        [HttpPost]
        public async Task<IActionResult> YeniIlBalansYarat(int il)
        {
            try
            {
                // Artıq həmin il üçün balans yaradılıbmı?
                var movcud = await _unitOfWork.Repository<MezuniyyetBalans>()
                    .AnyAsync(b => b.Il == il && !b.Silinib);

                if (movcud)
                    return Json(new { success = false, message = $"{il}-ci il üçün balans artıq mövcuddur." });

                // Bütün aktiv işçiləri gətir
                var aktivIsciler = await _unitOfWork.Repository<Isci>()
                    .Query()
                    .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                    .ToListAsync();

                if (!aktivIsciler.Any())
                    return Json(new { success = false, message = "Aktiv işçi tapılmadı." });

                var repo = _unitOfWork.Repository<MezuniyyetBalans>();

                // Əvvəlki ilin İllik balanslarını gətir (carry-over üçün)
                var evvelkiIlBalanslari = await repo.Query()
                    .Where(b => b.Il == il - 1 && !b.Silinib && b.Nov == MezuniyyetNovu.Illik)
                    .ToListAsync();
                var evvelkiBalansDict = evvelkiIlBalanslari.ToDictionary(b => b.IsciId, b => b);

                foreach (var isci in aktivIsciler)
                {
                    // Əmək məzuniyyəti (əvvəlki ildən maks 5 gün köçürmə ilə)
                    int kecirilecekGun = 0;
                    if (evvelkiBalansDict.TryGetValue(isci.Id, out var evvelkiBalans))
                    {
                        int qaliq = evvelkiBalans.ToplamGun - evvelkiBalans.IstifadeOlunanGun;
                        kecirilecekGun = Math.Min(Math.Max(qaliq, 0), 5);
                    }

                    await repo.YaratAsync(new MezuniyyetBalans
                    {
                        IsciId = isci.Id,
                        Il = il,
                        Nov = MezuniyyetNovu.Illik,
                        ToplamGun = 21 + kecirilecekGun,
                        IstifadeOlunanGun = 0
                    });

                    // Xəstəlik (limitsiz)
                    await repo.YaratAsync(new MezuniyyetBalans
                    {
                        IsciId = isci.Id,
                        Il = il,
                        Nov = MezuniyyetNovu.Xestelik,
                        ToplamGun = 0,
                        IstifadeOlunanGun = 0
                    });

                    // Ezamiyyət (limitsiz)
                    await repo.YaratAsync(new MezuniyyetBalans
                    {
                        IsciId = isci.Id,
                        Il = il,
                        Nov = MezuniyyetNovu.Ezamiyyet,
                        ToplamGun = 0,
                        IstifadeOlunanGun = 0
                    });
                }

                await _unitOfWork.YaddaSaxlaAsync();

                return Json(new
                {
                    success = true,
                    message = $"{il}-ci il üçün {aktivIsciler.Count} işçiyə balans yaradıldı."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Xəta: {ex.Message}" });
            }
        }

        // GET /HR/MezuniyyetBalans/IsciExcel?isciId=5
        // İşçinin öz məzuniyyət tarixçəsi — mühasibin əl ilə saxladığı şəxsi
        // vərəqin (əmr-əmr tarixçə + illər üzrə qalıq) FinNex qarşılığı.
        // Yalnız SİSTEMDƏ olan qeydləri göstərir — köhnə (sistemə qədərki)
        // əmrlər idxal edilməyibsə burada görünməyəcək (ayrıca məsələdir).
        public async Task<IActionResult> IsciExcel(int isciId)
        {
            var isci = await _unitOfWork.Repository<Isci>()
                .Query()
                .AsNoTracking()
                .Include(x => x.IsciTeyinatlari.Where(t => t.Aktivdir && !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .FirstOrDefaultAsync(x => x.Id == isciId);

            if (isci == null) return NotFound("İşçi tapılmadı.");

            var deptAd = isci.IsciTeyinatlari.FirstOrDefault()?.Departament?.Ad ?? "—";

            var mezuniyyetler = await _unitOfWork.Repository<Mezuniyyet>()
                .Query()
                .AsNoTracking()
                .Where(m => !m.Silinib && m.IsciId == isciId)
                .OrderBy(m => m.BaslamaTarixi)
                .Select(m => new
                {
                    m.EmrRegem,
                    m.EmrSuffiks,
                    m.EmrIl,
                    m.Nov,
                    m.Status,
                    m.BaslamaTarixi,
                    m.BitmeTarixi,
                    m.IsGunlerininSayi,
                    m.IsGunlerininSayiManual,
                    m.JetonIleOdendi,
                    m.OdenisTipi
                })
                .ToListAsync();

            var balanslar = await _unitOfWork.Repository<MezuniyyetBalans>()
                .Query()
                .AsNoTracking()
                .Where(b => !b.Silinib && b.IsciId == isciId)
                .OrderByDescending(b => b.Il)
                .ThenBy(b => b.Nov)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Tarixçə");

            ws.Cell(1, 1).Value = "İşçi:";
            ws.Cell(1, 2).Value = $"{isci.Ad} {isci.Soyad}";
            ws.Cell(1, 4).Value = "FİN:";
            ws.Cell(1, 5).Value = isci.FIN ?? "";
            ws.Cell(2, 1).Value = "Departament:";
            ws.Cell(2, 2).Value = deptAd;
            ws.Cell(2, 4).Value = "İşə qəbul:";
            ws.Cell(2, 5).Value = isci.IsheQebulTarixi;
            ws.Cell(2, 5).Style.DateFormat.Format = "dd.MM.yyyy";
            ws.Range(1, 1, 2, 1).Style.Font.Bold = true;
            ws.Range(1, 4, 2, 4).Style.Font.Bold = true;

            const int emrBaslikSetri = 4;
            var emrBasliqlari = new[]
            {
                "№", "Əmr №-si", "Növü", "Başlama", "Bitmə", "Gün sayı",
                "Status", "Ödəniş tipi", "Jeton ilə"
            };
            for (int i = 0; i < emrBasliqlari.Length; i++)
                ws.Cell(emrBaslikSetri, i + 1).Value = emrBasliqlari[i];

            var emrBaslikRange = ws.Range(emrBaslikSetri, 1, emrBaslikSetri, emrBasliqlari.Length);
            emrBaslikRange.Style.Font.Bold = true;
            emrBaslikRange.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
            emrBaslikRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            int setir = emrBaslikSetri + 1;
            foreach (var m in mezuniyyetler)
            {
                var emrNo = m.EmrRegem.HasValue ? $"K/M {m.EmrRegem}{m.EmrSuffiks ?? ""}" : "—";
                var gun = m.IsGunlerininSayiManual ?? m.IsGunlerininSayi;

                int c = 1;
                ws.Cell(setir, c++).Value = setir - emrBaslikSetri;
                ws.Cell(setir, c++).Value = emrNo;
                ws.Cell(setir, c++).Value = m.Nov.Adi();
                ws.Cell(setir, c).Value = m.BaslamaTarixi;
                ws.Cell(setir, c++).Style.DateFormat.Format = "dd.MM.yyyy";
                ws.Cell(setir, c).Value = m.BitmeTarixi;
                ws.Cell(setir, c++).Style.DateFormat.Format = "dd.MM.yyyy";
                ws.Cell(setir, c++).Value = gun;
                ws.Cell(setir, c++).Value = StatusAdi(m.Status);
                ws.Cell(setir, c++).Value = m.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis ? "Qabaqcadan" : "Ay sonu";
                ws.Cell(setir, c++).Value = m.JetonIleOdendi ? "Bəli" : "";
                setir++;
            }

            if (mezuniyyetler.Count == 0)
            {
                ws.Cell(setir, 1).Value = "Sistemdə bu işçi üçün qeyd tapılmadı.";
                setir++;
            }

            // ── İllər üzrə balans xülasəsi ───────────────────────────────
            setir += 2;
            int balansBaslikSetri = setir;
            var balansBasliqlari = new[] { "İş ili", "Növ", "Toplam gün", "İstifadə", "Qalıq" };
            for (int i = 0; i < balansBasliqlari.Length; i++)
                ws.Cell(balansBaslikSetri, i + 1).Value = balansBasliqlari[i];

            var balansBaslikRange = ws.Range(balansBaslikSetri, 1, balansBaslikSetri, balansBasliqlari.Length);
            balansBaslikRange.Style.Font.Bold = true;
            balansBaslikRange.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
            balansBaslikRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            setir = balansBaslikSetri + 1;
            foreach (var b in balanslar)
            {
                ws.Cell(setir, 1).Value = IlAralik(b.Il);
                ws.Cell(setir, 2).Value = b.Nov.Adi();
                ws.Cell(setir, 3).Value = b.ToplamGun;
                ws.Cell(setir, 4).Value = b.IstifadeOlunanGun;
                ws.Cell(setir, 5).Value = b.ToplamGun - b.IstifadeOlunanGun;
                setir++;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(emrBaslikSetri);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);

            var temizAd = $"{isci.Ad}_{isci.Soyad}".Replace(" ", "_");
            var fileName = $"Mezuniyyet_Tarixce_{temizAd}_{DateTime.Today:yyyy-MM-dd}.xlsx";
            return File(
                ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>Məzuniyyət statusunun Azərbaycan adı — bu export-a məxsus, yerli istifadə.</summary>
        private static string StatusAdi(MezuniyyetStatus status) => status switch
        {
            MezuniyyetStatus.Gozlemede => "Gözləmədə",
            MezuniyyetStatus.SobeReisiTesdiqinde => "Şöbə rəisi təsdiqində",
            MezuniyyetStatus.RehberTesdiqinde => "Rəhbər təsdiqində",
            MezuniyyetStatus.HrTesdiqinde => "HR təsdiqində",
            MezuniyyetStatus.Tesdiqlenib => "Təsdiqlənib",
            MezuniyyetStatus.ImtinaEdildi => "İmtina edildi",
            MezuniyyetStatus.LegvEdildi => "Ləğv edildi",
            _ => status.ToString()
        };

        /// <summary>İş ilini "2026–2027" formatında göstərir — Index.cshtml-dəki `IlAralik` ilə EYNİ qayda.</summary>
        private static string IlAralik(int il) => $"{il}–{il + 1}";
    }
}
