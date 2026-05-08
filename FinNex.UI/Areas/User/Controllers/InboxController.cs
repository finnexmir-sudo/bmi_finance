// Areas/User/Controllers/InboxController.cs
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.User.ViewModels.Inbox;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class InboxController : Controller
    {
        private readonly IMesajService _mesajService;
        private readonly IBildirisService _bildirisService;
        private readonly IEvezediciTesdiqService _evezediciTesdiqService;
        private readonly IIsciService _isciService;
        private readonly IIcazeService _icazeService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJetonTeklifleriService _teklifService;

        public InboxController(
            IMesajService mesajService,
            IBildirisService bildirisService,
            IEvezediciTesdiqService evezediciTesdiqService,
            IIsciService isciService,
            IIcazeService icazeService,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork,
            IJetonTeklifleriService teklifService)
        {
            _mesajService = mesajService;
            _bildirisService = bildirisService;
            _evezediciTesdiqService = evezediciTesdiqService;
            _isciService = isciService;
            _icazeService = icazeService;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _teklifService = teklifService;
        }

        // ── GET /User/Inbox ──────────────────────────────────
        public async Task<IActionResult> Index(string tab = "bildirisler")
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var bildirislerResult = await _bildirisService.GetIscibildirisleriAsync(isciId.Value);
            var gelenler = await _mesajService.GetGelenlerAsync(isciId.Value);
            var gonderilenler = await _mesajService.GetGonderilenlerAsync(isciId.Value);
            var evezediciSorgular = await _evezediciTesdiqService.GetGozleyenlerAsync(isciId.Value);

            var vm = new InboxIndexVM
            {
                AktivTab = tab,
                Bildirisler = bildirislerResult.Success ? bildirislerResult.Data!.ToList() : new(),
                GelenMesajlar = gelenler.Success ? gelenler.Data!.ToList() : new(),
                GonderilenMesajlar = gonderilenler.Success ? gonderilenler.Data!.ToList() : new(),
                EvezediciSorgular = evezediciSorgular.Success ? evezediciSorgular.Data!.ToList() : new(),
            };

            // ── İcazə statusunu bildirişlərə əlavə et (ikona üçün) ──
            var icazeIds = vm.Bildirisler
                .Where(b => b.IcazeId.HasValue)
                .Select(b => b.IcazeId!.Value)
                .Distinct().ToList();

            if (icazeIds.Any())
            {
                var icazeler = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(x => icazeIds.Contains(x.Id), izlemeden: true);
                var statusMap = icazeler.ToDictionary(x => x.Id, x => x.Status);

                bool isRehber = User.IsInRole(RoleNames.Rehber);
                bool isHr     = User.IsInRole(RoleNames.HR);

                foreach (var b in vm.Bildirisler)
                {
                    if (!b.IcazeId.HasValue || !statusMap.TryGetValue(b.IcazeId.Value, out var s))
                        continue;

                    b.IcazeStatus = s;

                    // Rəhbər üçün: status HrTesdiqinde/Tesdiqlenib/ImtinaEdildi = Rəhbər artıq tesdiq etdi
                    if (isRehber)
                        b.TesdiqEdildi = s != IcazeStatus.RehberTesdiqinde;
                    // HR üçün: status Tesdiqlenib/ImtinaEdildi = HR artıq tesdiq etdi
                    else if (isHr)
                        b.TesdiqEdildi = s == IcazeStatus.Tesdiqlenib || s == IcazeStatus.ImtinaEdildi;
                }
            }

            // ── Gözlənənlər: mənim gözləyən müraciətlərim ──
            var menimResult = await _icazeService.GetIsciIcazeleriAsync(isciId.Value);
            if (menimResult.Success)
            {
                vm.MenimGozleyenIcazelarim = menimResult.Data!
                    .Where(x => x.Status == IcazeStatus.RehberTesdiqinde
                             || x.Status == IcazeStatus.HrTesdiqinde
                             || x.Status == IcazeStatus.SobeReisiTesdiqinde
                             || x.Status == IcazeStatus.Gozlemede)
                    .ToList();
            }

            // ── Gözlənənlər: mənim tesdiq etmeyim gərəkənlər ──
            if (User.IsInRole(RoleNames.Rehber))
            {
                var r = await _icazeService.GetRehberTesdiqindeAsync();
                if (r.Success) vm.TesdiqEtmeyimGerekenler = r.Data!.ToList();
            }
            else if (User.IsInRole(RoleNames.HR))
            {
                var r = await _icazeService.GetHrTesdiqindeAsync();
                if (r.Success) vm.TesdiqEtmeyimGerekenler = r.Data!.ToList();
            }

            ViewData["Title"] = "Gələn Qutusu";
            return View(vm);
        }

        // ── GET /User/Inbox/Mesaj/5 ───────────────────────────
        public async Task<IActionResult> Mesaj(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _mesajService.GetDetayAsync(id, isciId.Value);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = "Mesaj tapılmadı.";
                return RedirectToAction(nameof(Index), new { tab = "mesajlar" });
            }

            // Cavab üçün işçi siyahısı
            var iscilerResult = await _isciService.HamisiniGetirAsync(
                x => x.Id != isciId.Value && x.Status == IsciStatus.Aktiv);

            var vm = new MesajDetailPageVM
            {
                Mesaj = result.Data,
                CavabDto = new MesajCreateDto
                {
                    GonderenIsciId = isciId.Value,
                    AlanIsciId = result.Data.GonderenIsciId == isciId.Value
                        ? result.Data.AlanIsciId
                        : result.Data.GonderenIsciId,
                    CavabVerdigiMesajId = id
                }
            };

            ViewData["Title"] = "Mesaj Detalı";
            return View(vm);
        }

        // ── GET /User/Inbox/YeniMesaj ─────────────────────────
        public async Task<IActionResult> YeniMesaj(int? alanId = null)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var iscilerResult = await _isciService.HamisiniGetirAsync(
                x => x.Id != isciId.Value && x.Status == IsciStatus.Aktiv);

            var vm = new YeniMesajVM
            {
                GonderenIsciId = isciId.Value,
                AlanIsciId = alanId ?? 0,
                IsciList = iscilerResult.Success
                    ? iscilerResult.Data!
                        .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                        .ToList()
                    : new()
            };

            ViewData["Title"] = "Yeni Mesaj";
            return View(vm);
        }

        // ── POST /User/Inbox/YeniMesaj ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeniMesaj(YeniMesajVM vm)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            vm.GonderenIsciId = isciId.Value;

            if (!ModelState.IsValid)
            {
                var iscilerResult = await _isciService.HamisiniGetirAsync(
                    x => x.Id != isciId.Value && x.Status == IsciStatus.Aktiv);
                vm.IsciList = iscilerResult.Success
                    ? iscilerResult.Data!
                        .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                        .ToList()
                    : new();
                return View(vm);
            }

            var dto = new MesajCreateDto
            {
                GonderenIsciId = isciId.Value,
                AlanIsciId = vm.AlanIsciId,
                Movzu = vm.Movzu,
                Metn = vm.Metn
            };

            var result = await _mesajService.GonderAsync(dto);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index), new { tab = "mesajlar" });
        }

        // ── POST /User/Inbox/Cavabla ──────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cavabla(MesajCreateDto dto)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            dto.GonderenIsciId = isciId.Value;
            var result = await _mesajService.GonderAsync(dto);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Mesaj), new { id = dto.CavabVerdigiMesajId });
        }

        // ── GET /User/Inbox/TopluMesaj ────────────────────────
        [HttpGet]
        public async Task<IActionResult> TopluMesaj()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var iscilerResult = await _isciService.HamisiniGetirAsync(
                x => x.Id != isciId.Value && x.Status == IsciStatus.Aktiv);

            ViewBag.Isciler = iscilerResult.Success
                ? iscilerResult.Data!.Select(x => new SelectListItem($"{x.TamAd} ({x.SobeAdi ?? "-"})", x.Id.ToString())).ToList()
                : new List<SelectListItem>();

            ViewData["Title"] = "Toplu Mesaj";
            return View();
        }

        // ── POST /User/Inbox/TopluMesaj ───────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopluMesaj(string movzu, string metn, List<int> alanIsciIds)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            if (string.IsNullOrWhiteSpace(metn))
            {
                TempData["Error"] = "Mətn boş ola bilməz.";
                return RedirectToAction(nameof(TopluMesaj));
            }

            var result = await _mesajService.TopluGonderAsync(isciId.Value, alanIsciIds, movzu, metn);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index), new { tab = "gonderilenler" });
        }

        // ── POST /User/Inbox/BildirisOxu ─────────────────────
        [HttpPost]
        public async Task<IActionResult> BildirisOxu(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return Json(new { success = false });

            await _bildirisService.OxunduIsareEtAsync(id, isciId.Value);
            return Json(new { success = true });
        }

        // ── POST /User/Inbox/HamisiniOxu ─────────────────────
        [HttpPost]
        public async Task<IActionResult> HamisiniOxu()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return Json(new { success = false });

            await _bildirisService.HamisiniOxunduIsareEtAsync(isciId.Value);
            return Json(new { success = true });
        }

        // ── POST /User/Inbox/EvezediciQebul ──────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EvezediciQebul(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _evezediciTesdiqService.QebulEtAsync(id, isciId.Value);
            if (result.Success)
                _ = _teklifService.EvezediciQebulEdildiAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index), new { tab = "sorgular" });
        }

        // ── POST /User/Inbox/EvezediciRedd ───────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EvezediciRedd(int id, string? qeyd)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _evezediciTesdiqService.ReddEtAsync(id, isciId.Value, qeyd);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index), new { tab = "sorgular" });
        }

        // ── GET /User/Inbox/OxunmamisSayi (AJAX) ─────────────
        [HttpGet]
        public async Task<IActionResult> OxunmamisSayi()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null)
                return Json(new { bildiris = 0, mesaj = 0, cemi = 0 });

            var bildiris = await _bildirisService.OxunmamisSayiAsync(isciId.Value);
            var mesaj = await _mesajService.OxunmamisSayiAsync(isciId.Value);

            int b = bildiris.Success ? bildiris.Data : 0;
            int m = mesaj.Success ? mesaj.Data : 0;

            return Json(new { bildiris = b, mesaj = m, cemi = b + m });
        }

        private async Task<int?> GetIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private IActionResult RedirectToLogin() =>
            RedirectToAction("Login", "Account", new { area = "" });
    }
}