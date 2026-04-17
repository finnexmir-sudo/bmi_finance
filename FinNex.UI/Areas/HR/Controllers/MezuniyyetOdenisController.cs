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

            // Məzuniyyət iş günü sayı (bu ay üçün)
            var mezAy = mez.BaslamaTarixi.Month;
            var mezIl = mez.BaslamaTarixi.Year;
            int ayIsGun = hesab.AySliceleri.FirstOrDefault()?.AyIsGun ?? 22;
            int mezIsGun = hesab.UmumiIsGun;
            int islenmisGun = Math.Max(0, ayIsGun - mezIsGun);
            decimal islenmisMaas = ayIsGun > 0 ? Math.Round(cariMaas / ayIsGun * islenmisGun, 2) : 0;

            // NET (yalnız işlənmiş günlər)
            var islenmisTax = await _maasHesablamaService
                .TutulmalariHesablaAsync(islenmisMaas, new DateTime(mezIl, mezAy, 1), mez.IsciId);

            // HYS
            var hysAyBitis = new DateTime(mezIl, mezAy, 1).AddMonths(1).AddDays(-1);
            var isciHys = await _unitOfWork.Repository<IsciHYS>()
                .Query()
                .FirstOrDefaultAsync(x =>
                    !x.Silinib && x.IsciId == mez.IsciId &&
                    x.BaslamaTarixi <= hysAyBitis &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= new DateTime(mezIl, mezAy, 1)));
            decimal hysMebleg = isciHys?.Mebleg ?? 0;

            // Avans
            var avanslar = await _unitOfWork.Repository<Avans>()
                .Query()
                .Where(x => !x.Silinib && x.IsciId == mez.IsciId &&
                    x.Il == mezIl && x.Ay == mezAy &&
                    (x.Status == AvansStatus.Tesdiqlenib || x.Status == AvansStatus.Odenilib))
                .ToListAsync();
            decimal avansMebleg = avanslar.Sum(x => x.Mebleg);

            // Maaş günü əlinə çatacaq
            decimal maasGuniNet = islenmisTax.Net - hysMebleg - avansMebleg - hesab.CemiOdenis;

            ViewBag.CariMaas = cariMaas;
            ViewBag.AyIsGun = ayIsGun;
            ViewBag.MezIsGun = mezIsGun;
            ViewBag.IslenmisGun = islenmisGun;
            ViewBag.IslenmisMaas = islenmisMaas;
            ViewBag.IslenmisNet = islenmisTax.Net;
            ViewBag.HysMebleg = hysMebleg;
            ViewBag.AvansMebleg = avansMebleg;
            ViewBag.MezPuluBrut = hesab.CemiOdenis;
            ViewBag.MaasGuniNet = maasGuniNet;

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
        public async Task<IActionResult> Planla(int id, string? redakteEdilmisMebleg)
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

            decimal? yekunMebleg = parsedMebleg ?? mez.OdenenMebleg;

            if (yekunMebleg == null || yekunMebleg <= 0)
            {
                TempData["Error"] = "Ödəniş məbləği düzgün deyil.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var appUser = await _userManager.GetUserAsync(User);
            var muhasibIsciId = appUser?.IsciId;

            // Planlı ödəniş tarixi — məzuniyyətdən bir iş günü əvvəl
            var planliTarix = await EvvelkiIsGunuAsync(mez.BaslamaTarixi);

            mez.OdenenMebleg = yekunMebleg;
            mez.OdenisStatus = MezuniyyetOdenisStatus.PlanliOdenis;
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
