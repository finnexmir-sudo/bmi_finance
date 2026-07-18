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

            // HYS — bütün aktiv qeydlərin cəmi (işçi bir neçə şirkətdə HYS aça bilər)
            var hysAyBitis = new DateTime(baslama.Year, baslama.Month, 1).AddMonths(1).AddDays(-1);
            decimal hysMebleg = await _unitOfWork.Repository<IsciHYS>()
                .Query()
                .Where(x =>
                    !x.Silinib &&
                    x.IsciId == isciId.Value &&
                    x.BaslamaTarixi <= hysAyBitis &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= new DateTime(baslama.Year, baslama.Month, 1)))
                .SumAsync(x => (decimal?)x.Mebleg) ?? 0m;

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

            // Hər iki rejimdə (Ay sonu VƏ Qabaqcadan) aylıq brüt EYNİ cür hesablanır:
            //   ay brütü = işlənmiş günlərin maaşı + məzuniyyət pulu
            // və aylıq brüt üzərindən vergi tutulur. Yeganə fərq ödəniş qaydasıdır:
            //   • Ay sonu — hər şey bir ödənişlə (ay sonunda NET ödənilir)
            //   • Qabaqcadan — məzuniyyət pulu avans kimi vergisiz verilir,
            //                  ay sonunda ay nettindən avans çıxılır
            // Beləliklə işçinin aylıq cəmi NET-i HƏR İKİ REJİMDƏ EYNİDİR —
            // qanuni cəhətdən düzgün yanaşma.
            var ayProjections = new List<object>();
            decimal ayBrutCemi = 0;
            decimal ayNetCemi = 0;
            decimal ayTutulmaCemi = 0;

            foreach (var s in hesablama.AySliceleri)
            {
                // İşlənmiş iş günü = ayın iş günü − məzuniyyətin FAKTİKİ iş günü.
                // (s.IsGun PR #352 ilə həftəsonu daxildir, həftəsonu işçinin işləməyəcəyi
                // gün olduğundan kəsintidə nəzərə alınmır — yalnız HaqiqiIsGun istifadə olunur.)
                int islenmisIsGun = Math.Max(0, s.AyIsGun - s.HaqiqiIsGun);
                decimal islenmisMaas = (cariMaas > 0 && s.AyIsGun > 0)
                    ? Math.Round(cariMaas / s.AyIsGun * islenmisIsGun, 2)
                    : 0;
                decimal kesinti = Math.Max(0, cariMaas - islenmisMaas);

                // Məzuniyyət pulu həmişə ay brütünə daxil edilir (qanuni doğru)
                decimal odenisPay = s.Secilen;
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
                    // Məzuniyyət pulu (NET) = tam ay net − işlənmiş net
                    mezOdenisNet = ayTax.Net - islenmisTax.Net
                });

                ayBrutCemi += ayBrut;
                ayNetCemi += ayTax.Net;
                ayTutulmaCemi += ayTax.UmumiTutulma;
            }

            // Ay sonu rejimi üçün məzuniyyət pulu cəmi (brüt və net, ayrıca göstərmək üçün)
            decimal mezPuluBrutCemi = hesablama.CemiOdenis;
            decimal islenmisNetCemi = 0;
            decimal mezOdenisNetCemi = 0;
            foreach (var s in hesablama.AySliceleri)
            {
                // İşlənmiş günlər = AyIsGun − HaqiqiIsGun (faktiki iş günü).
                // IsGun-da həftəsonu da var, ona görə istifadəsi səhv im verir və
                // MezuniyyetOdenisController.Detail ilə uyğunsuzluq yaradırdı.
                int ig = Math.Max(0, s.AyIsGun - s.HaqiqiIsGun);
                decimal im = (cariMaas > 0 && s.AyIsGun > 0)
                    ? Math.Round(cariMaas / s.AyIsGun * ig, 2) : 0;
                var itax = await _maasHesablamaService
                    .TutulmalariHesablaAsync(im, new DateTime(s.Il, s.Ay, 1), isciId.Value);
                var ftax = await _maasHesablamaService
                    .TutulmalariHesablaAsync(im + s.Secilen, new DateTime(s.Il, s.Ay, 1), isciId.Value);
                islenmisNetCemi += itax.Net;
                mezOdenisNetCemi += ftax.Net - itax.Net;
            }

            // Qabaqcadan rejimi: məzuniyyət pulu vergili NET olaraq avans kimi ödənilir.
            // (Aylıq brüt üzərindən vergi hesablanır, vergi məz pulu hissəsinə mütənasib
            // şəkildə ayrılır.) İşçi avans olaraq tam net mezOdenisNetCemi-ni alır.
            // Ay sonu qalan ödəniş = işlənmiş günlər NET.
            decimal advanceBrut = 0;
            decimal advanceTutulma = 0;
            decimal advanceNet = 0;
            if (qabaqcadan && hesablama.CemiOdenis > 0)
            {
                advanceBrut = mezPuluBrutCemi;                      // məz pulu brüt (məlumat üçün)
                advanceNet = mezOdenisNetCemi;                      // işçinin əlinə çatan NET
                advanceTutulma = advanceBrut - advanceNet;          // məz hissəsinin vergisi
            }

            // NET-dən çıxılanlar: yalnız işçi HYS payı + avans
            // Hər iki rejimdə aylıq cəmi NET eynidir (ayNetCemi).
            decimal hysAvansToplu = hysMebleg + avansMebleg;
            decimal umumiNet = ayNetCemi - hysAvansToplu;
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
            var hrdirmi = User.IsInRole(RoleNames.HR);
            var mezList = mezResult.Success ? mezResult.Data!.ToList() : new();
            foreach (var m in mezList)
            {
                m.MuracietSahibiRehberdirmi = rehberdirmi;
                m.MuracietSahibiSobeReisidirmi = sobeReisidirmi;
                m.MuracietSahibiHrdirmi = hrdirmi;
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
                vm.VezifeAdi = dashResult.Data.VezifeAdi;
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
            // İşçi yalnız Əmək məzuniyyəti (Illik) və ya Öz hesabına (ödənişsiz, Ə.M. 129)
            // müraciət edə bilər. Xəstəlik/Ezamiyyət yalnız HR tərəfindən qeyd olunur —
            // icazəsiz növ gələrsə təhlükəsizlik üçün Illik-ə düşür.
            if (vm.Nov != (int)MezuniyyetNovu.Illik && vm.Nov != (int)MezuniyyetNovu.OzHesabina)
                vm.Nov = (int)MezuniyyetNovu.Illik;

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
                MuracietSahibiHrdirmi = User.IsInRole(RoleNames.HR),
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
            vm.MuracietSahibiHrdirmi = User.IsInRole(RoleNames.HR);

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

        // ── POST /User/Mezuniyyet/LegvTelebi — təsdiqlənmişi ləğv üçün HR-a müraciət ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LegvTelebi(int id, string sebeb)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _mezuniyyetService.LegvTelebiEtAsync(id, isciId.Value, sebeb);

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Index", "Muraciet");
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
