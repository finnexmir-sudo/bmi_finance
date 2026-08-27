using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// Qabaqcadan ödəniş seçilmiş məzuniyyətlər üçün Mühasib səhifəsi.
    /// HR təsdiqindən sonra müraciət buraya düşür (OdenisStatus = Gozleyir).
    /// Mühasib hesablamanın məntiqini görür (addım-addım izah), məbləği
    /// yoxlayır (lazım olarsa dəyişir) və “Ödənildi” vurur.
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.Muhasib + "," + RoleNames.Admin)]
    public class MezuniyyetOdenisController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMaasHesablamaService _maasHesablamaService;
        private readonly IBildirisService _bildirisService;
        private readonly UserManager<AppUser> _userManager;

        public MezuniyyetOdenisController(
            IUnitOfWork unitOfWork,
            IMaasHesablamaService maasHesablamaService,
            IBildirisService bildirisService,
            UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _maasHesablamaService = maasHesablamaService;
            _bildirisService = bildirisService;
            _userManager = userManager;
        }

        // ── GET /HR/MezuniyyetOdenis ─────────────────────────────
        //  Filter: gozleyir (default) / planli / odenilib / hamisi
        public async Task<IActionResult> Index(string filter = "gozleyir")
        {
            var query = _unitOfWork.Repository<Mezuniyyet>()
                .Query()
                .Where(x => !x.Silinib && x.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis)
                .Include(x => x.Isci);

            IQueryable<Mezuniyyet> filtered = filter switch
            {
                "planli" => query.Where(x => x.OdenisStatus == MezuniyyetOdenisStatus.PlanliOdenis),
                "odenilib" => query.Where(x => x.OdenisStatus == MezuniyyetOdenisStatus.Odenilib),
                "hamisi" => query,
                _ => query.Where(x => x.OdenisStatus == MezuniyyetOdenisStatus.Gozleyir)
            };

            var list = await filtered
                .OrderByDescending(x => x.HrTesdiqTarixi)
                .ThenByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            ViewBag.Filter = filter;
            ViewBag.GozleyirSay = await _unitOfWork.Repository<Mezuniyyet>().Query()
                .CountAsync(x => !x.Silinib
                    && x.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis
                    && x.OdenisStatus == MezuniyyetOdenisStatus.Gozleyir);
            ViewBag.PlanliSay = await _unitOfWork.Repository<Mezuniyyet>().Query()
                .CountAsync(x => !x.Silinib
                    && x.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis
                    && x.OdenisStatus == MezuniyyetOdenisStatus.PlanliOdenis);

            return View(list);
        }

        // ── GET /HR/MezuniyyetOdenis/Detail/5 ────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            var mez = await _unitOfWork.Repository<Mezuniyyet>()
                .Query()
                .Where(x => x.Id == id && !x.Silinib)
                .Include(x => x.Isci)
                .Include(x => x.HrIsci)
                .Include(x => x.OdeyenMuhasib)
                .FirstOrDefaultAsync();

            if (mez == null) return NotFound();

            // Yenidən canlı hesablama aparırıq — Mühasib kodlara baxmadan
            // məntiqi görə bilsin və cari rəqəmlərin eyni olduğundan əmin olsun.
            var hesab = await _maasHesablamaService
                .MezuniyyetOdenisiDetalliHesablaAsync(mez.IsciId, mez.BaslamaTarixi, mez.BitmeTarixi);

            // ── Ay sonu maaş preview — mühasib üçün informativ ──
            // İşçi maaş günü nə qədər alacaq (qabaqcadan ödəniş çıxıldıqdan sonra)?
            var maliye = await _unitOfWork.Repository<IsciMaliye>()
                .GetirAsync(x => x.IsciId == mez.IsciId);
            decimal cariMaas = maliye?.CariMaas ?? 0;

            var mezAy = mez.BaslamaTarixi.Month;
            var mezIl = mez.BaslamaTarixi.Year;
            int ayIsGun = hesab.AySliceleri.FirstOrDefault()?.AyIsGun ?? 22;

            // HYS
            var hysAyBitis = new DateTime(mezIl, mezAy, 1).AddMonths(1).AddDays(-1);
            decimal hysMebleg = await _unitOfWork.Repository<IsciHYS>()
                .Query()
                .Where(x =>
                    !x.Silinib && x.IsciId == mez.IsciId &&
                    x.BaslamaTarixi <= hysAyBitis &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= new DateTime(mezIl, mezAy, 1)))
                .SumAsync(x => (decimal?)x.Mebleg) ?? 0m;

            // Avans
            var avanslar = await _unitOfWork.Repository<Avans>()
                .Query()
                .Where(x => !x.Silinib && x.IsciId == mez.IsciId &&
                    x.Il == mezIl && x.Ay == mezAy &&
                    (x.Status == AvansStatus.Tesdiqlenib || x.Status == AvansStatus.Odenilib))
                .ToListAsync();
            decimal avansMebleg = avanslar.Sum(x => x.Mebleg);

            // Aylıq NET cəmi: işlənmiş günlər + məzuniyyət pulu (birlikdə vergi hesablanır)
            // HaqiqiIsGun = faktiki iş günü (həftəsonu/bayram çıxılıb) — maaş üçün düzgün əsas
            decimal islenmisNetCemi = 0;
            decimal mezOdenisNetCemi = 0;
            decimal islenmisMaas = 0;
            int islenmisGun = 0;
            decimal gelirVergisi = 0, dsmfIsci = 0, issizlikIsci = 0, itssSum = 0, umumiTutulma = 0;
            foreach (var s in hesab.AySliceleri)
            {
                int ig = Math.Max(0, s.AyIsGun - s.HaqiqiIsGun);
                decimal im = (cariMaas > 0 && s.AyIsGun > 0)
                    ? Math.Round(cariMaas / s.AyIsGun * ig, 2) : 0;
                var itax = await _maasHesablamaService
                    .TutulmalariHesablaAsync(im, new DateTime(s.Il, s.Ay, 1), mez.IsciId);
                var ftax = await _maasHesablamaService
                    .TutulmalariHesablaAsync(im + s.Secilen, new DateTime(s.Il, s.Ay, 1), mez.IsciId);
                islenmisNetCemi += itax.Net;
                mezOdenisNetCemi += ftax.Net - itax.Net;

                // ── Mühasib cədvəli üçün əvəzləşmə (yalnız göstərmə) ─────────
                // YALNIZ qabaqcadan ödənişdə dolur — AySonu qeydində qabaqcadan
                // verilmiş pul yoxdur, «əvəzləşmə» anlayışı mənasızdır (view da
                // netlər boş olanda sütunları ümumiyyətlə göstərmir).
                //
                // Payın brütü = EH («cari maaş hesabı» sütunu) — mühasib aylıq
                // cədvəldə məzuniyyəti bu payla yazır (işlənmiş + EH = tam ay).
                // Neti MARJİNAL hesablanır: tax(işlənmiş+EH) − tax(işlənmiş) —
                // ayın güzəştlərini işlənmiş hissə udur, pay güzəştsiz vergilənir.
                // Düz faiz (15.5%) YAZILMIR — aşağı maaşda güzəşt sərhədinə
                // düşən hallar üçün real vergi funksiyası çağırılır.
                if (mez.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis)
                {
                    var etax = await _maasHesablamaService
                        .TutulmalariHesablaAsync(im + s.EH, new DateTime(s.Il, s.Ay, 1), mez.IsciId);
                    s.EvezlesmeNet = Math.Round(etax.Net - itax.Net, 2);
                    s.EvezlesmeTutulma = Math.Round(s.EH - s.EvezlesmeNet.Value, 2);
                }

                islenmisMaas += im;
                islenmisGun += ig;
                // Aylıq cəmi tutulma komponentləri (birləşdirilmiş brüt üzrə)
                gelirVergisi += ftax.GelirVergisi;
                dsmfIsci += ftax.DsmfIsci;
                issizlikIsci += ftax.IssizlikIsci;
                itssSum += ftax.Itss;
                umumiTutulma += ftax.UmumiTutulma;
            }
            decimal ayNetCemi = islenmisNetCemi + mezOdenisNetCemi;

            // Qabaqcadan ödəniş: məzuniyyət NET (vergidən sonra)
            decimal advanceNet = mez.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis
                ? mezOdenisNetCemi : 0;

            // Əvəzləşmə netləri ÖDƏNİLƏN NET-ə qəpiyinə bağlanmalıdır — yoxsa
            // mühasibin cədvəli bank köçürməsi ilə 1-2 qəpik fərqlənər və «niyə
            // tutuşmur» sualı təzələnər. Yuvarlaqlaşma fərqi SON AYIN payına
            // yazılır (son ay qalığı udur).
            if (advanceNet > 0 && hesab.AySliceleri.Count > 0)
            {
                var evvelkiler = hesab.AySliceleri.Take(hesab.AySliceleri.Count - 1)
                    .Sum(x => x.EvezlesmeNet ?? 0);
                var son = hesab.AySliceleri[^1];
                son.EvezlesmeNet = Math.Round(advanceNet - evvelkiler, 2);
                son.EvezlesmeTutulma = Math.Round(son.EH - son.EvezlesmeNet.Value, 2);
            }
            decimal advanceTutulma = advanceNet > 0 ? hesab.CemiOdenis - advanceNet : 0;

            // Maaş günü qalıq: aylıq NET - avans (artıq verilib) - HYS - avans kreditləri
            decimal maasGuniNet = mez.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis
                ? islenmisNetCemi - hysMebleg - avansMebleg
                : ayNetCemi - hysMebleg - avansMebleg - hesab.CemiOdenis;

            ViewBag.CariMaas = cariMaas;
            ViewBag.AyIsGun = ayIsGun;
            ViewBag.MezIsGun = hesab.AySliceleri.FirstOrDefault()?.HaqiqiIsGun ?? hesab.UmumiIsGun;
            ViewBag.IslenmisGun = islenmisGun;
            ViewBag.IslenmisMaas = islenmisMaas;
            ViewBag.IslenmisNet = islenmisNetCemi;
            ViewBag.HysMebleg = hysMebleg;
            ViewBag.AvansMebleg = avansMebleg;
            ViewBag.MezPuluBrut = hesab.CemiOdenis;
            ViewBag.MaasGuniNet = maasGuniNet;
            ViewBag.AdvanceNet = advanceNet;
            ViewBag.AdvanceTutulma = advanceTutulma;
            ViewBag.GelirVergisi = gelirVergisi;
            ViewBag.DsmfIsci = dsmfIsci;
            ViewBag.IssizlikIsci = issizlikIsci;
            ViewBag.Itss = itssSum;
            ViewBag.UmumiTutulma = umumiTutulma;

            // Avans hazırlıq yoxlaması — əvvəlki ay maaşı bağlanıbmı (12 aylıq pəncərə tam olsun)
            var (evvelkiAyBagli, evvelkiAyAdi) = await EvvelkiAyMaasiBaglidirAsync(mez);
            ViewBag.EvvelkiAyBaglidir = evvelkiAyBagli;
            ViewBag.EvvelkiAyAdi = evvelkiAyAdi;

            ViewBag.Mezuniyyet = mez;
            return View(hesab);
        }

        // ── POST /HR/MezuniyyetOdenis/Planla ─────────────────────
        // Mühasib məbləği təsdiqləyir. Faktiki bank köçürməsi məzuniyyətdən bir
        // iş günü əvvəl (PlanliOdenisTarixi) icra olunur. Həmin tarixdə fon
        // xidməti statusu avtomatik Odenilib-ə çevirir və işçiyə ödəniş
        // bildirişi göndərir. Müstəsna hallarda "IcraEt" düyməsi ilə bu tarixdən
        // asılı olmayaraq manual olaraq icra etmək mümkündür.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Planla(int id, string? redakteEdilmisMebleg, string? odenenMeblegBrut)
        {
            var mez = await _unitOfWork.Repository<Mezuniyyet>()
                .GetirAsync(x => x.Id == id);

            if (mez == null)
            {
                TempData["Error"] = "Müraciət tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            if (mez.OdenisTipi != MezuniyyetOdenisTipi.QabaqcadanOdenis)
            {
                TempData["Error"] = "Bu müraciət qabaqcadan ödənişə təyin olunmayıb.";
                return RedirectToAction(nameof(Index));
            }

            if (mez.OdenisStatus != MezuniyyetOdenisStatus.Gozleyir)
            {
                TempData["Error"] = "Yalnız gözləmədə olan ödənişi planlaşdırmaq olar.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            // Avans yalnız əvvəlki ay maaşı bağlandıqdan sonra — 12 aylıq orta düzgün olsun
            var (eaBagli, eaAdi) = await EvvelkiAyMaasiBaglidirAsync(mez);
            if (!eaBagli)
            {
                TempData["Error"] = $"Avans ödənişi üçün əvvəlki ayın ({eaAdi}) maaşı hələ hesablanmayıb. " +
                                    "Maaş bağlandıqdan sonra ödəyin — yoxsa 12 aylıq orta natamam olur.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            // HTML number input həmişə "456.52" formatında POST edir, amma Azərbaycan
            // culture-ı vergülü decimal separator kimi gözləyir → səhv parse olunub
            // rəqəm 100 qat böyüyürdü. InvariantCulture ilə (həm dot həm comma qəbul)
            // manual parse edirik.
            decimal? parsedMebleg = null;
            if (!string.IsNullOrWhiteSpace(redakteEdilmisMebleg))
            {
                var normalized = redakteEdilmisMebleg.Trim().Replace(',', '.');
                if (decimal.TryParse(normalized,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var parsed) && parsed > 0)
                {
                    parsedMebleg = Math.Round(parsed, 2);
                }
                else
                {
                    TempData["Error"] = "Ödəniş məbləği düzgün deyil.";
                    return RedirectToAction(nameof(Detail), new { id });
                }
            }

            // Default canlı məbləğ view-dan (redakteEdilmisMebleg) gəlir. Boşdursa
            // dondurulmuş OdenenMebleg-ə KEÇMİRİK — məbləğ açıq tələb olunur.
            decimal? yekunMebleg = parsedMebleg;

            if (yekunMebleg == null || yekunMebleg <= 0)
            {
                TempData["Error"] = "Ödəniş məbləği daxil edilməyib.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var appUser = await _userManager.GetUserAsync(User);
            var muhasibIsciId = appUser?.IsciId;

            // Planlı ödəniş tarixi — məzuniyyətdən bir iş günü əvvəl
            var planliTarix = await EvvelkiIsGunuAsync(mez.BaslamaTarixi);

            mez.OdenenMebleg = yekunMebleg;
            mez.OdenisStatus = MezuniyyetOdenisStatus.PlanliOdenis;
            if (!string.IsNullOrWhiteSpace(odenenMeblegBrut))
            {
                var normBrut = odenenMeblegBrut.Trim().Replace(',', '.');
                if (decimal.TryParse(normBrut, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsedBrut) && parsedBrut > 0)
                    mez.OdenenMeblegBrut = Math.Round(parsedBrut, 2);
            }
            mez.PlanliOdenisTarixi = planliTarix;
            mez.OdeyenMuhasibId = muhasibIsciId;
            mez.YenilenmeTarixi = DateTime.Now;

            await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(mez);
            await _unitOfWork.YaddaSaxlaAsync();

            try
            {
                await _bildirisService.YaratAsync(
                    isciId: mez.IsciId,
                    nov: BildirisNovu.MezuniyyetOdenisPlanlandi,
                    bashliq: "Məzuniyyət ödənişi planlaşdı",
                    metn: $"{mez.BaslamaTarixi:dd.MM.yyyy}–{mez.BitmeTarixi:dd.MM.yyyy} məzuniyyət " +
                          $"ödənişiniz ({yekunMebleg:N2} ₼) {planliTarix:dd.MM.yyyy} tarixində " +
                          "kartınıza köçürüləcək.",
                    redirectUrl: Url.Action("Detail", "Mezuniyyet", new { area = "User", id = mez.Id }),
                    mezuniyyetId: mez.Id
                );
            }
            catch { /* bildiriş xətası əsas işləməni pozmasın */ }

            TempData["Success"] = $"Ödəniş planlaşdı — {planliTarix:dd.MM.yyyy} ({yekunMebleg:N2} ₼).";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /HR/MezuniyyetOdenis/IcraEt ──────────────────────
        // Planlı tarixdən asılı olmayaraq ödənişi dərhal "Icra edildi"
        // kimi işarələ (məsələn, bank köçürməsi daha erkən edildi).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IcraEt(int id)
        {
            var mez = await _unitOfWork.Repository<Mezuniyyet>()
                .GetirAsync(x => x.Id == id);

            if (mez == null)
            {
                TempData["Error"] = "Müraciət tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            if (mez.OdenisTipi != MezuniyyetOdenisTipi.QabaqcadanOdenis)
            {
                TempData["Error"] = "Bu müraciət qabaqcadan ödənişə təyin olunmayıb.";
                return RedirectToAction(nameof(Index));
            }

            if (mez.OdenisStatus != MezuniyyetOdenisStatus.PlanliOdenis
                && mez.OdenisStatus != MezuniyyetOdenisStatus.Gozleyir)
            {
                TempData["Error"] = "Bu müraciət artıq icra edilib.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            // Avans yalnız əvvəlki ay maaşı bağlandıqdan sonra icra edilə bilər
            var (eaBagliIcra, eaAdiIcra) = await EvvelkiAyMaasiBaglidirAsync(mez);
            if (!eaBagliIcra)
            {
                TempData["Error"] = $"Avans ödənişi üçün əvvəlki ayın ({eaAdiIcra}) maaşı hələ hesablanmayıb. " +
                                    "Maaş bağlandıqdan sonra icra edin.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            if (mez.OdenenMebleg == null || mez.OdenenMebleg <= 0)
            {
                TempData["Error"] = "Ödəniş məbləği təsdiqlənməyib. Əvvəlcə 'Planla' addımını tamamlayın.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var appUser = await _userManager.GetUserAsync(User);
            var muhasibIsciId = appUser?.IsciId;

            mez.OdenisStatus = MezuniyyetOdenisStatus.Odenilib;
            mez.OdenilmeTarixi = DateTime.Now;
            mez.OdeyenMuhasibId = muhasibIsciId ?? mez.OdeyenMuhasibId;
            mez.YenilenmeTarixi = DateTime.Now;

            await _unitOfWork.Repository<Mezuniyyet>().YenileAsync(mez);
            await _unitOfWork.YaddaSaxlaAsync();

            try
            {
                await _bildirisService.YaratAsync(
                    isciId: mez.IsciId,
                    nov: BildirisNovu.MezuniyyetOdenisIcraEdildi,
                    bashliq: "Məzuniyyət ödənişi icra edildi",
                    metn: $"{mez.BaslamaTarixi:dd.MM.yyyy}–{mez.BitmeTarixi:dd.MM.yyyy} məzuniyyət " +
                          $"ödənişiniz ({mez.OdenenMebleg:N2} ₼) kartınıza köçürüldü.",
                    redirectUrl: Url.Action("Detail", "Mezuniyyet", new { area = "User", id = mez.Id }),
                    mezuniyyetId: mez.Id
                );
            }
            catch { /* bildiriş xətası əsas işləməni pozmasın */ }

            TempData["Success"] = $"Ödəniş icra edildi ({mez.OdenenMebleg:N2} ₼).";
            return RedirectToAction(nameof(Index));
        }

        private static readonly string[] AzAylar =
            { "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
              "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr" };

        // Avans ödənişi yalnız məzuniyyət başlama ayından ƏVVƏLKİ ayın maaşı bağlandıqdan
        // (həmin ay üçün IsciAyliqQazanc qeydi yarandıqdan) sonra edilə bilər. Əks halda
        // məzuniyyət pulunun 12 aylıq orta bazası natamam pəncərəyə düşür və səhv olur.
        private async Task<(bool baglidir, string ayAdi)> EvvelkiAyMaasiBaglidirAsync(Mezuniyyet mez)
        {
            var evvelki = new DateTime(mez.BaslamaTarixi.Year, mez.BaslamaTarixi.Month, 1).AddMonths(-1);
            var baglidir = await _unitOfWork.Repository<IsciAyliqQazanc>()
                .Query()
                .AnyAsync(x => x.IsciId == mez.IsciId && x.Il == evvelki.Year
                            && x.Ay == evvelki.Month && !x.Silinib);
            return (baglidir, $"{AzAylar[evvelki.Month]} {evvelki.Year}");
        }

        // Məzuniyyətin başlanğıcından bir iş günü əvvəlki tarix — həftəsonu və
        // bayram günlərini atlayır.
        private async Task<DateTime> EvvelkiIsGunuAsync(DateTime baslama)
        {
            var gun = baslama.Date.AddDays(-1);

            // Məzuniyyət başlayan aydakı (və əvvəlki ay sərhədində) bayramları
            var pencereBas = gun.AddDays(-14);
            var bayramlar = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x => x.Tarix >= pencereBas && x.Tarix <= baslama, izlemeden: true);

            while (gun.DayOfWeek == DayOfWeek.Saturday
                || gun.DayOfWeek == DayOfWeek.Sunday
                || bayramlar.Any(b => b.Tarix.Date == gun.Date))
            {
                gun = gun.AddDays(-1);
            }

            return gun;
        }
    }
}
