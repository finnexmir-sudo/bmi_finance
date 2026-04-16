using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.HR;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.User.ViewModels.Mezuniyyet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class MezuniyyetController : Controller
    {
        private readonly IMezuniyyetService _mezuniyyetService;
        private readonly IDashboardService _dashboardService;
        private readonly IIsciService _isciService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEvezediciTesdiqService _evezediciTesdiqService;
        private readonly IIsciAyliqQazancService _ayliqQazancService;
        private readonly IMaasHesablamaService _maasHesablamaService;
        private readonly IUnitOfWork _unitOfWork;

        public MezuniyyetController(
            IMezuniyyetService mezuniyyetService,
            IDashboardService dashboardService,
            IIsciService isciService,
            UserManager<AppUser> userManager,
            IEvezediciTesdiqService evezediciTesdiqService,
            IIsciAyliqQazancService ayliqQazancService,
            IMaasHesablamaService maasHesablamaService,
            IUnitOfWork unitOfWork)
        {
            _mezuniyyetService = mezuniyyetService;
            _dashboardService = dashboardService;
            _isciService = isciService;
            _userManager = userManager;
            _evezediciTesdiqService = evezediciTesdiqService;
            _ayliqQazancService = ayliqQazancService;
            _maasHesablamaService = maasHesablamaService;
            _unitOfWork = unitOfWork;
        }

        // ── GET /User/Mezuniyyet/Preview?baslama=2026-04-10&bitme=2026-04-17&odenisTipi=1 ──
        // Real-time məzuniyyət ödənişi + ay-ay bölünmə + gözlənilən aylıq maaş preview (JSON)
        [HttpGet]
        public async Task<IActionResult> Preview(DateTime baslama, DateTime bitme, int odenisTipi = 1)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return Json(new { success = false, message = "İşçi tapılmadı." });

            if (bitme < baslama)
                return Json(new { success = false, message = "Bitmə tarixi başlama tarixindən sonra olmalıdır." });

            var hesablama = await _maasHesablamaService
                .MezuniyyetOdenisiDetalliHesablaAsync(isciId.Value, baslama, bitme);

            decimal cariMaas = hesablama.CariMaas;
            bool qabaqcadan = odenisTipi == (int)MezuniyyetOdenisTipi.QabaqcadanOdenis;

            // HYS — işçinin aktiv HYS-ını tap
            var hysAyBitis = new DateTime(baslama.Year, baslama.Month, 1).AddMonths(1).AddDays(-1);
            var isciHys = await _unitOfWork.Repository<IsciHYS>()
                .Query()
                .Where(x =>
                    !x.Silinib &&
                    x.IsciId == isciId.Value &&
                    x.BaslamaTarixi <= hysAyBitis &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= new DateTime(baslama.Year, baslama.Month, 1)))
                .FirstOrDefaultAsync();
            decimal hysMebleg = isciHys?.Mebleg ?? 0;

            // İşəgötürən HYS faizi
            var hysIsvParam = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .Where(x => x.Aktivdir && !x.Silinib && x.Nov == MaasParametrNovu.HysIsegoturenFaizi)
                .OrderByDescending(x => x.BaslamaTarixi)
                .FirstOrDefaultAsync();
            decimal hysIsv = hysMebleg > 0 ? Math.Round(hysMebleg * ((hysIsvParam?.Deyer ?? 15m) / 100m), 2) : 0;

            // Avans — bu aydakı təsdiqlənmiş avans
            var avanslar = await _unitOfWork.Repository<Avans>()
                .Query()
                .Where(x =>
                    !x.Silinib &&
                    x.IsciId == isciId.Value &&
                    x.Il == baslama.Year &&
                    x.Ay == baslama.Month &&
                    (x.Status == AvansStatus.Tesdiqlenib || x.Status == AvansStatus.Odenilib))
                .ToListAsync();
            decimal avansMebleg = avanslar.Sum(x => x.Mebleg);

            // Hər ay üçün brüt (baza - məzuniyyət kəsintisi + (əgər ay sonu) məz. ödənişi),
            // sonra tutulmalar və net. Qabaqcadan halında məzuniyyət ödənişi ayrıca
            // hesablanır və ayrıca tutulmalara tabe olur.
            var ayProjections = new List<object>();
            decimal ayBrutCemi = 0;
            decimal ayNetCemi = 0;
            decimal ayTutulmaCemi = 0;

            foreach (var s in hesablama.AySliceleri)
            {
                // İşlənmiş iş günləri = ayın iş günü − məzuniyyət iş günü.
                int islenmisIsGun = Math.Max(0, s.AyIsGun - s.IsGun);
                decimal islenmisMaas = (cariMaas > 0 && s.AyIsGun > 0)
                    ? Math.Round(cariMaas / s.AyIsGun * islenmisIsGun, 2)
                    : 0;
                decimal kesinti = Math.Max(0, cariMaas - islenmisMaas);
                decimal odenisPay = qabaqcadan ? 0 : s.Secilen;

                // "Ay sonu maaşla" rejimində ayrı-ayrı hesablayırıq:
                // 1) Yalnız işlənmiş günlərin maaşı (məzuniyyət pulu olmadan)
                // 2) Məzuniyyət pulu ayrıca
                decimal ayBrut = islenmisMaas + odenisPay;

                // Tutulmalar tam brüt üzrə (işlənmiş + məz pulu birlikdə)
                var ayTax = await _maasHesablamaService
                    .TutulmalariHesablaAsync(ayBrut, new DateTime(s.Il, s.Ay, 1), isciId.Value);

                // Yalnız işlənmiş günlərin maaşı üzrə tutulmalar (ayrıca göstərmək üçün)
                var islenmisTax = await _maasHesablamaService
                    .TutulmalariHesablaAsync(islenmisMaas, new DateTime(s.Il, s.Ay, 1), isciId.Value);

                ayProjections.Add(new
                {
                    il = s.Il,
                    ay = s.Ay,
                    ayAdi = s.AyAdi,
                    teqvimGun = s.TeqvimGun,
                    isGun = s.IsGun,
                    ayIsGun = s.AyIsGun,
                    islenmisIsGun,
                    islenmisMaas,
                    mh = s.MH,
                    eh = s.EH,
                    secilen = s.Secilen,
                    qalib = s.Qalib,
                    kesinti,
                    odenisPay,
                    ayBrut,
                    standartGuzest = ayTax.StandartGuzest,
                    isciGuzesti = ayTax.IsciGuzesti,
                    isciGuzestiAd = ayTax.IsciGuzestiAd,
                    vergilenecek = ayTax.Vergilenecek,
                    gelirVergisi = ayTax.GelirVergisi,
                    dsmfIsci = ayTax.DsmfIsci,
                    issizlikIsci = ayTax.IssizlikIsci,
                    itss = ayTax.Itss,
                    tutulmalarCemi = ayTax.UmumiTutulma,
                    ayNet = ayTax.Net,
                    // Ayrıca: yalnız işlənmiş günlər maaşı (NET)
                    islenmisNet = islenmisTax.Net,
                    islenmisTutulma = islenmisTax.UmumiTutulma,
                    // Məzuniyyət pulu (NET): brüt − tutulma fərqi
                    mezOdenisNet = ayTax.Net - islenmisTax.Net
                });

                ayBrutCemi += ayBrut;
                ayNetCemi += ayTax.Net;
                ayTutulmaCemi += ayTax.UmumiTutulma;
            }

            // Ay sonu maaşla: məzuniyyət pulu cəmi (brüt və net)
            decimal mezPuluBrutCemi = hesablama.CemiOdenis;
            decimal islenmisNetCemi = 0;
            decimal mezOdenisNetCemi = 0;
            if (!qabaqcadan)
            {
                foreach (var s in hesablama.AySliceleri)
                {
                    int ig = Math.Max(0, s.AyIsGun - s.IsGun);
                    decimal im = (cariMaas > 0 && s.AyIsGun > 0)
                        ? Math.Round(cariMaas / s.AyIsGun * ig, 2) : 0;
                    var itax = await _maasHesablamaService
                        .TutulmalariHesablaAsync(im, new DateTime(s.Il, s.Ay, 1), isciId.Value);
                    var ftax = await _maasHesablamaService
                        .TutulmalariHesablaAsync(im + s.Secilen, new DateTime(s.Il, s.Ay, 1), isciId.Value);
                    islenmisNetCemi += itax.Net;
                    mezOdenisNetCemi += ftax.Net - itax.Net;
                }
            }

            // Qabaqcadan ödəniş: ayrıca brüt → tutulmalar → net
            decimal advanceBrut = 0;
            decimal advanceTutulma = 0;
            decimal advanceNet = 0;
            if (qabaqcadan && hesablama.CemiOdenis > 0)
            {
                advanceBrut = hesablama.CemiOdenis;
                var advTax = await _maasHesablamaService
                    .TutulmalariHesablaAsync(advanceBrut, baslama, isciId.Value);
                advanceTutulma = advTax.UmumiTutulma;
                advanceNet = advTax.Net;
            }

            // NET-dən çıxılanlar: yalnız işçi HYS payı + avans
            // İşəgötürən HYS (hysIsv) NET-ə təsir etmir — GROSS-a əlavə olunub geri çıxılır
            decimal hysAvansToplu = hysMebleg + avansMebleg;
            decimal umumiNet = qabaqcadan
                ? ayNetCemi + advanceNet - hysAvansToplu
                : ayNetCemi - hysAvansToplu;
            if (umumiNet < 0) umumiNet = 0;

            return Json(new
            {
                success = true,
                teqvimGun = hesablama.UmumiTeqvimGun,
                isGun = hesablama.UmumiIsGun,
                cariMaas,
                hysMebleg,
                hysIsv,
                avansMebleg,
                S = hesablama.Son12AyCemi,
                sDuzelmis = hesablama.Son12AyDuzelmisCemi,
                qeydSayi = hesablama.Son12AyQeydSayi,
                cemiOdenis = hesablama.CemiOdenis,
                // Ay sonu maaşla: ayrıca məzuniyyət pulu (net) və qalan maaş (net)
                mezPuluBrutCemi,
                mezOdenisNetCemi,
                islenmisNetCemi,
                odenisTipi,
                qabaqcadan,
                aylar = ayProjections,
                emsalCedveli = hesablama.QazancEmsallari.Select(k => new
                {
                    ayAdi = k.AyAdi,
                    statMaas = k.StatMaas,
                    qazanc = k.Qazanc,
                    emsal = k.Emsal,
                    duzelmisQazanc = k.DuzelmisQazanc
                }),
                tarixceXeberdarliqlari = hesablama.TarixceXeberdarliqlari,
                ayBrutCemi,
                ayTutulmaCemi,
                ayNetCemi,
                advanceBrut,
                advanceTutulma,
                advanceNet,
                umumiNet
            });
        }

        // ── GET /User/Mezuniyyet ────────────────────────────────
        public async Task<IActionResult> Index(string? nov = null, string? status = null)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var mezResult = await _mezuniyyetService.GetIsciMezuniyyetleriAsync(isciId.Value);
            var dashResult = await _dashboardService.GetDashboardAsync(User.Identity!.Name!);

            var rehberdirmi = User.IsInRole(RoleNames.Rehber);
            var sobeReisidirmi = User.IsInRole(RoleNames.SobeReisi);
            var mezList = mezResult.Success ? mezResult.Data!.ToList() : new();
            foreach (var m in mezList)
            {
                m.MuracietSahibiRehberdirmi = rehberdirmi;
                m.MuracietSahibiSobeReisidirmi = sobeReisidirmi;
            }

            var vm = new MezuniyyetIndexVM
            {
                Mezuniyyetler = mezList,
                FilterNov = nov,
                FilterStatus = status,
            };

            if (dashResult.Success && dashResult.Data != null)
            {
                vm.IllikToplamGun = dashResult.Data.IllikToplamGun;
                vm.IllikIstifadeGun = dashResult.Data.IllikIstifadeGun;
                vm.XestelikToplamGun = dashResult.Data.XestelikToplamGun;
                vm.XestelikIstifadeGun = dashResult.Data.XestelikIstifadeGun;
                vm.EzamiyyetToplamGun = dashResult.Data.EzamiyyetToplamGun;
                vm.EzamiyyetIstifadeGun = dashResult.Data.EzamiyyetIstifadeGun;
            }

            return View(vm);
        }

        // ── GET /User/Mezuniyyet/Create ────────────────────────
        public async Task<IActionResult> Create()
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var vm = await BuildCreateVMAsync(isciId.Value);
            return View(vm);
        }

        // ── POST /User/Mezuniyyet/Create ───────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MezuniyyetCreateVM vm)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            vm.IsciId = isciId.Value;
            vm.Nov = 1; // İşçi yalnız İllik məzuniyyət müraciəti edə bilər

            if (vm.BitmeTarixi < vm.BaslamaTarixi)
                ModelState.AddModelError("BitmeTarixi", "Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");

            if (!ModelState.IsValid)
            {
                await FillVMDropdownsAsync(vm, isciId.Value);
                await FillVMBalanceAsync(vm);
                return View(vm);
            }

            var createDto = new MezuniyyetCreateDto
            {
                IsciId = isciId.Value,
                EvezEdenIsciId = vm.EvezEdenIsciId,
                Nov = (MezuniyyetNovu)vm.Nov,
                BaslamaTarixi = vm.BaslamaTarixi,
                BitmeTarixi = vm.BitmeTarixi,
                Qeyd = vm.Qeyd,
                OdenisTipi = vm.OdenisTipi == 2
                    ? MezuniyyetOdenisTipi.QabaqcadanOdenis
                    : MezuniyyetOdenisTipi.AySonuOdenis,
                MuracietSahibiRehberdirmi = User.IsInRole(RoleNames.Rehber),
                MuracietSahibiSobeReisidirmi = User.IsInRole(RoleNames.SobeReisi),
            };

            var result = await _mezuniyyetService.YaratAsync(createDto);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                await FillVMDropdownsAsync(vm, isciId.Value);
                await FillVMBalanceAsync(vm);
                return View(vm);
            }

            TempData["Success"] = result.Message ?? "Müraciət uğurla göndərildi.";
            return RedirectToAction(nameof(Index));
        }

        // ── GET /User/Mezuniyyet/Detail/5 ─────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _mezuniyyetService.IdIleGetirAsync(id);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = "Müraciət tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var dto = result.Data!;
            if (dto.IsciId != isciId.Value)
            {
                TempData["Error"] = "Bu müraciətə baxmaq icazəniz yoxdur.";
                return RedirectToAction(nameof(Index));
            }

            var vm = MezuniyyetDetailVM.FromDto(dto);
            vm.MuracietSahibiRehberdirmi = User.IsInRole(RoleNames.Rehber);
            vm.MuracietSahibiSobeReisidirmi = User.IsInRole(RoleNames.SobeReisi);

            // Əvəzedici statusunu yoxla
            var evezResult = await _evezediciTesdiqService.GetByMezuniyyetAsync(id);
            if (evezResult.Success && evezResult.Data != null)
            {
                vm.EvezediciSecildi = true;
                vm.EvezediciTesdiqlenib = evezResult.Data.Status == FinNex.Domain.Entities.Communication.EvezediciTesdiqStatus.Qebul
                    ? true
                    : evezResult.Data.Status == FinNex.Domain.Entities.Communication.EvezediciTesdiqStatus.Redd
                        ? false
                        : null;
            }

            return View(vm);
        }

        // ── POST /User/Mezuniyyet/Legv ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Legv(int id)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _mezuniyyetService.LegvEtAsync(id, isciId.Value);

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ══ Köməkçi metodlar ══════════════════════════════════

        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private async Task<MezuniyyetCreateVM> BuildCreateVMAsync(int isciId)
        {
            var vm = new MezuniyyetCreateVM { IsciId = isciId };
            await FillVMDropdownsAsync(vm, isciId);
            await FillVMBalanceAsync(vm);
            return vm;
        }

        private async Task FillVMDropdownsAsync(MezuniyyetCreateVM vm, int isciId)
        {
            var iscilerResult = await _isciService.HamisiniGetirAsync(
                x => x.Id != isciId && x.Status == IsciStatus.Aktiv,
                izlemeden: true);

            vm.EvezEdenList = iscilerResult.Success
                ? iscilerResult.Data!
                    .OrderBy(x => x.TamAd)
                    .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                    .ToList()
                : new();
        }

        private async Task FillVMBalanceAsync(MezuniyyetCreateVM vm)
        {
            var dashResult = await _dashboardService.GetDashboardAsync(User.Identity!.Name!);
            if (dashResult.Success && dashResult.Data != null)
            {
                vm.IllikToplamGun = dashResult.Data.IllikToplamGun;
                vm.IllikIstifadeGun = dashResult.Data.IllikIstifadeGun;
                vm.XestelikToplamGun = dashResult.Data.XestelikToplamGun;
                vm.XestelikIstifadeGun = dashResult.Data.XestelikIstifadeGun;
                vm.EzamiyyetToplamGun = dashResult.Data.EzamiyyetToplamGun;
                vm.EzamiyyetIstifadeGun = dashResult.Data.EzamiyyetIstifadeGun;
            }
        }

        private IActionResult RedirectToLogin() =>
            RedirectToAction("Login", "Account", new { area = "" });
    }
}
