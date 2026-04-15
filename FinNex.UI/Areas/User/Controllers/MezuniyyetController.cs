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

            // Hər ay üçün gözlənilən maaşın sadə proyeksiyası:
            //   base - (cariMaas / ayIsGun × bu ayın mez. iş günü)
            //        + (əgər AySonuOdenis) həmin ayın MAX(MH, ƏH)
            decimal cariMaas = hesablama.CariMaas;
            bool qabaqcadan = odenisTipi == (int)MezuniyyetOdenisTipi.QabaqcadanOdenis;

            var ayProjections = hesablama.AySliceleri.Select(s =>
            {
                decimal kesinti = (cariMaas > 0 && s.AyIsGun > 0 && s.IsGun > 0)
                    ? Math.Round(cariMaas / s.AyIsGun * s.IsGun, 2)
                    : 0;
                decimal odenisPay = qabaqcadan ? 0 : s.Secilen;
                decimal ayMaas = Math.Max(0, cariMaas - kesinti + odenisPay);
                return new
                {
                    il = s.Il,
                    ay = s.Ay,
                    ayAdi = s.AyAdi,
                    teqvimGun = s.TeqvimGun,
                    isGun = s.IsGun,
                    ayIsGun = s.AyIsGun,
                    mh = s.MH,
                    eh = s.EH,
                    secilen = s.Secilen,
                    qalib = s.Qalib,
                    kesinti,
                    odenisPay,
                    ayMaas
                };
            }).ToList();

            decimal ayMaasCemi = ayProjections.Sum(x => x.ayMaas);
            decimal umumiYekun = qabaqcadan
                ? ayMaasCemi + hesablama.CemiOdenis  // qabaqcadan ayrıca
                : ayMaasCemi;                         // ay sonu — hər şey maaşın içində

            return Json(new
            {
                success = true,
                teqvimGun = hesablama.UmumiTeqvimGun,
                isGun = hesablama.UmumiIsGun,
                cariMaas,
                S = hesablama.Son12AyCemi,
                qeydSayi = hesablama.Son12AyQeydSayi,
                cemiOdenis = hesablama.CemiOdenis,
                odenisTipi,
                qabaqcadan,
                aylar = ayProjections,
                ayMaasCemi,
                umumiYekun
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
