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
        private readonly IUnitOfWork _unitOfWork;

        public MezuniyyetController(
            IMezuniyyetService mezuniyyetService,
            IDashboardService dashboardService,
            IIsciService isciService,
            UserManager<AppUser> userManager,
            IEvezediciTesdiqService evezediciTesdiqService,
            IIsciAyliqQazancService ayliqQazancService,
            IUnitOfWork unitOfWork)
        {
            _mezuniyyetService = mezuniyyetService;
            _dashboardService = dashboardService;
            _isciService = isciService;
            _userManager = userManager;
            _evezediciTesdiqService = evezediciTesdiqService;
            _ayliqQazancService = ayliqQazancService;
            _unitOfWork = unitOfWork;
        }

        // ── GET /User/Mezuniyyet/Preview?baslama=2026-04-10&bitme=2026-04-17 ──
        // Real-time məzuniyyət ödənişi preview (JSON)
        [HttpGet]
        public async Task<IActionResult> Preview(DateTime baslama, DateTime bitme)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return Json(new { success = false, message = "İşçi tapılmadı." });

            if (bitme < baslama)
                return Json(new { success = false, message = "Bitmə tarixi başlama tarixindən sonra olmalıdır." });

            // İşçinin cari maaşı
            var maliye = await _unitOfWork.Repository<IsciMaliye>()
                .GetirAsync(x => x.IsciId == isciId.Value);
            decimal cariMaas = maliye?.CariMaas ?? 0;

            // Bayram günlərini gətir (məzuniyyət dövründə)
            var bayramlar = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x => x.Tarix >= baslama && x.Tarix <= bitme && !x.Silinib);
            var bayramTarixleri = bayramlar.Select(x => x.Tarix.Date).ToHashSet();

            // GS (təqvim günü) və İGS (iş günü) hesabla
            int teqvimGun = 0, isGun = 0;
            for (var t = baslama; t <= bitme; t = t.AddDays(1))
            {
                teqvimGun++;
                if (t.DayOfWeek != DayOfWeek.Saturday &&
                    t.DayOfWeek != DayOfWeek.Sunday &&
                    !bayramTarixleri.Contains(t.Date))
                {
                    isGun++;
                }
            }

            // S = Son 12 ayın cəmi
            decimal S = await _ayliqQazancService.Son12AyCemiQazancAsync(isciId.Value);
            int qeydSayi = await _ayliqQazancService.Son12AyQeydSayiAsync(isciId.Value);

            // Cari ayın iş gün sayı (məzuniyyət başladığı ay)
            var ayBaslangic = new DateTime(baslama.Year, baslama.Month, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);
            var ayBayramlar = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x => x.Tarix >= ayBaslangic && x.Tarix <= ayBitis && !x.Silinib);
            var ayBayramTarix = ayBayramlar.Select(x => x.Tarix.Date).ToHashSet();
            int ayIsGun = 0;
            for (var t = ayBaslangic; t <= ayBitis; t = t.AddDays(1))
            {
                if (t.DayOfWeek != DayOfWeek.Saturday &&
                    t.DayOfWeek != DayOfWeek.Sunday &&
                    !ayBayramTarix.Contains(t.Date))
                    ayIsGun++;
            }
            if (ayIsGun == 0) ayIsGun = 22;

            // Formula
            decimal MH = 0;
            if (S > 0 && teqvimGun > 0)
                MH = Math.Round(S / 12m / 30.4m * teqvimGun, 2);

            decimal EH = 0;
            if (cariMaas > 0 && ayIsGun > 0 && isGun > 0)
                EH = Math.Round(cariMaas / ayIsGun * isGun, 2);

            decimal odenis = Math.Max(MH, EH);

            return Json(new
            {
                success = true,
                teqvimGun,
                isGun,
                cariMaas,
                ayIsGun,
                S,
                qeydSayi,
                MH,
                EH,
                odenis,
                qalib = MH > EH ? "MH" : "EH"
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
