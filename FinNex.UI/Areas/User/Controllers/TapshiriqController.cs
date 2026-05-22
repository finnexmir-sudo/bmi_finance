// Areas/User/Controllers/TapshiriqController.cs
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.User.ViewModels.Tapshiriq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class TapshiriqController : Controller
    {
        private readonly ITapshiriqService _tapshiriqService;
        private readonly IIsciService _isciService;
        private readonly IGelenMailService _gelenMailService;
        private readonly IDesktopBildirisService _desktopBildiris;
        private readonly UserManager<AppUser> _userManager;

        public TapshiriqController(
            ITapshiriqService tapshiriqService,
            IIsciService isciService,
            IGelenMailService gelenMailService,
            IDesktopBildirisService desktopBildiris,
            UserManager<AppUser> userManager)
        {
            _tapshiriqService = tapshiriqService;
            _isciService = isciService;
            _gelenMailService = gelenMailService;
            _desktopBildiris = desktopBildiris;
            _userManager = userManager;
        }

        // ── GET /User/Tapshiriq ──────────────────────────────
        public async Task<IActionResult> Index(string tab = "menim")
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var menim = await _tapshiriqService.GetMenimTapshiriqlarimAsync(isciId.Value);
            var verdiklerim = await _tapshiriqService.GetVerdiklerimAsync(isciId.Value);
            var mailTapshiriqlari = await _gelenMailService.GetMailTapshiriqlariAsync(isciId.Value);

            var vm = new TapshiriqIndexVM
            {
                AktivTab = tab,
                MenimTapshiriqlar = menim.Success ? menim.Data!.ToList() : new(),
                VerdiklerimTapshiriqlar = verdiklerim.Success ? verdiklerim.Data!.ToList() : new(),
                MailTapshiriqlari = mailTapshiriqlari,
            };

            ViewData["Title"] = "Tapşırıqlar";
            return View(vm);
        }

        // ── GET /User/Tapshiriq/Detay/5 ─────────────────────
        public async Task<IActionResult> Detay(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();
            ViewBag.IsciId = isciId.Value;

            var result = await _tapshiriqService.GetDetayAsync(id, isciId.Value);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = "Tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = result.Data.Bashliq;
            return View(result.Data);
        }

        // ── GET /User/Tapshiriq/Yarat ────────────────────────
        public async Task<IActionResult> Yarat()
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var vm = await BuildYaratVMAsync(isciId.Value);
            ViewData["Title"] = "Tapşırıq Yarat";
            return View(vm);
        }

        // ── POST /User/Tapshiriq/Yarat ───────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yarat(TapshiriqYaratVM vm)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            if (!ModelState.IsValid)
            {
                var freshVm = await BuildYaratVMAsync(isciId.Value);
                freshVm.TeyinOlunanIsciId = vm.TeyinOlunanIsciId;
                freshVm.Bashliq = vm.Bashliq;
                freshVm.Tesvir = vm.Tesvir;
                freshVm.SonTarix = vm.SonTarix;
                freshVm.Prioritet = vm.Prioritet;
                return View(freshVm);
            }

            var dto = new TapshiriqCreateDto
            {
                Bashliq = vm.Bashliq,
                Tesvir = vm.Tesvir,
                YaradanIsciId = isciId.Value,
                TeyinOlunanIsciId = vm.TeyinOlunanIsciId,
                SonTarix = vm.SonTarix,
                Prioritet = vm.Prioritet,
                Qeyd = vm.Qeyd
            };

            var result = await _tapshiriqService.YaratAsync(dto);
            TempData[result.Success ? "Success" : "Error"] = result.Message;

            if (result.Success)
            {
                try
                {
                    await _desktopBildiris.PushAsync(
                        vm.TeyinOlunanIsciId,
                        "Yeni Tapşırıq Təyini",
                        $"Sizə yeni bir tapşırıq təyin edildi: {vm.Bashliq}");
                }
                catch { /* Desktop push xətası əsas əməliyyatı pozmasın */ }
            }

            return RedirectToAction(nameof(Index), new { tab = "verdiklerim" });
        }

        // ── POST /User/Tapshiriq/StatusYenile ────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StatusYenile(TapshiriqUpdateDto dto)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _tapshiriqService.StatusYenileAsync(dto, isciId.Value);
            TempData[result.Success ? "Success" : "Error"] = result.Message;

            if (result.Success)
            {
                try
                {
                    var detay = await _tapshiriqService.GetDetayAsync(dto.Id, isciId.Value);
                    if (detay.Success && detay.Data != null)
                    {
                        await _desktopBildiris.PushAsync(
                            detay.Data.YaradanIsciId,
                            "Tapşırıq Statusu Dəyişdi",
                            $"{detay.Data.Bashliq} tapşırığının statusu yeniləndi.");
                    }
                }
                catch { /* Desktop push xətası əsas əməliyyatı pozmasın */ }
            }

            return RedirectToAction(nameof(Detay), new { id = dto.Id });
        }

        // ── POST /User/Tapshiriq/SherhEleveEt ────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SherhEleveEt(int tapshiriqId, string metn)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _tapshiriqService.SherhEleveEtAsync(tapshiriqId, isciId.Value, metn);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Detay), new { id = tapshiriqId });
        }

        // ── POST /User/Tapshiriq/Sil ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _tapshiriqService.SilAsync(id, isciId.Value);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // POST /User/Tapshiriq/MailIcraEt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MailIcraEt(int mailId)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            await _gelenMailService.IcraEtAsync(mailId, isciId.Value);
            TempData["Success"] = "Tapşırıq tamamlandı olaraq qeyd edildi.";
            return RedirectToAction(nameof(MailGoruntusu), new { mailId });
        }

        // POST /User/Tapshiriq/MailImtina
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MailImtina(int mailId, string? sebeb)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            await _gelenMailService.ImtinaEtAsync(mailId, isciId.Value, sebeb);
            TempData["Info"] = "İmtina qeyd edildi.";
            return RedirectToAction(nameof(Index), new { tab = "mail" });
        }

        // GET /User/Tapshiriq/MailGoruntusu/5
        public async Task<IActionResult> MailGoruntusu(int mailId)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var mail = await _gelenMailService.GetDetailAsync(mailId);
            if (mail == null) return NotFound();

            // Yalnız tapşırılan işçi görə bilər
            var tapshirildi = mail.TapalanIsciler.Any(ti => ti.IsciId == isciId.Value);
            if (!tapshirildi) return Forbid();

            await _gelenMailService.IsciOxuduIsareEtAsync(mailId, isciId.Value);

            ViewBag.IsciId = isciId.Value;
            ViewData["Title"] = mail.Movzu;
            return View(mail);
        }

        private async Task<TapshiriqYaratVM> BuildYaratVMAsync(int isciId)
        {
            var iscilerResult = await _isciService.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv);

            return new TapshiriqYaratVM
            {
                YaradanIsciId = isciId,
                IsciList = iscilerResult.Success
                    ? iscilerResult.Data!
                        .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                        .ToList()
                    : new()
            };
        }

        private async Task<int?> GetIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private IActionResult RedirectToLogin() =>
            RedirectToAction("Login", "Account", new { area = "" });

        // GET /User/Tapshiriq/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var result = await _tapshiriqService.GetDetayAsync(id, isciId.Value);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = "Tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var t = result.Data;
            if (t.YaradanIsciId != isciId.Value)
            {
                TempData["Error"] = "Yalnız yaradan redaktə edə bilər.";
                return RedirectToAction(nameof(Detay), new { id });
            }

            if (t.Status == TapshiriqStatus.Tamamlandi || t.Status == TapshiriqStatus.LegvEdildi)
            {
                TempData["Error"] = "Tamamlanmış və ya ləğv edilmiş tapşırıq redaktə edilə bilməz.";
                return RedirectToAction(nameof(Detay), new { id });
            }

            var iscilerResult = await _isciService.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv);

            var vm = new TapshiriqEditVM
            {
                Id = t.Id,
                Bashliq = t.Bashliq,
                Tesvir = t.Tesvir,
                TeyinOlunanIsciId = t.TeyinOlunanIsciId,
                SonTarix = t.SonTarix,
                Prioritet = t.Prioritet,
                Qeyd = t.Qeyd,
                IsciList = iscilerResult.Success
                    ? iscilerResult.Data!
                        .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                        .ToList()
                    : new()
            };

            ViewData["Title"] = "Tapşırığı Redaktə Et";
            return View(vm);
        }

        // POST /User/Tapshiriq/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TapshiriqEditVM vm)
        {
            var isciId = await GetIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            if (!ModelState.IsValid)
            {
                var iscilerResult = await _isciService.HamisiniGetirAsync(
                    x => x.Status == IsciStatus.Aktiv);
                vm.IsciList = iscilerResult.Success
                    ? iscilerResult.Data!
                        .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                        .ToList()
                    : new();
                return View(vm);
            }

            var dto = new TapshiriqEditDto
            {
                Id = vm.Id,
                Bashliq = vm.Bashliq,
                Tesvir = vm.Tesvir,
                TeyinOlunanIsciId = vm.TeyinOlunanIsciId,
                SonTarix = vm.SonTarix,
                Prioritet = vm.Prioritet,
                Qeyd = vm.Qeyd,
                YaradanIsciId = isciId.Value
            };

            var result = await _tapshiriqService.EditAsync(dto);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Detay), new { id = vm.Id });
        }
    }
}