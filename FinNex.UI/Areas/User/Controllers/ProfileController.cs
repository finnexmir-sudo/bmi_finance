using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using FinNex.UI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IDataProtector _protector;
        private readonly ISmtpMailService _smtp;
        /// <summary>Profil səhifəsində «Mail Ayarları» kartının görünüşü üçün.</summary>
        private readonly IUserPermissionService _permission;

        public ProfileController(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IDataProtectionProvider dpProvider,
            ISmtpMailService smtp,
            IUserPermissionService permission)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _protector = dpProvider.CreateProtector("MailSmtpParol");
            _permission = permission;
            _smtp = smtp;
        }

        // ── GET /User/Profile ──────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null) return RedirectToLogin();

            var isci = await _unitOfWork.Repository<Isci>()
                .GetirAsync(
                    x => x.Id == isciId.Value,
                    include: q => q
                        .Include(i => i.Maliye)
                        .Include(i => i.IsciTeyinatlari)
                            .ThenInclude(t => t.Departament)
                        .Include(i => i.IsciTeyinatlari)
                            .ThenInclude(t => t.Vezife)
                        .Include(i => i.MezuniyyetBalanslari));

            if (isci == null)
            {
                TempData["Error"] = "Profil məlumatları tapılmadı.";
                return RedirectToAction("Index", "Dashboard", new { area = "User" });
            }

            var appUser = await _userManager.GetUserAsync(User);
            ViewBag.MailSmtpEmail     = appUser?.MailSmtpEmail ?? "";
            ViewBag.MailSmtpHost      = appUser?.MailSmtpHost ?? "";
            ViewBag.MailSmtpKonfiqDir = !string.IsNullOrWhiteSpace(appUser?.MailSmtpParol);

            // Mail kartı görünsünmü — şərt `MailIcazesiAttribute` ilə EYNİDİR.
            // Razor-da qurmuruq: markup içində şərt qurmaq iki nüsxə yaradır və
            // biri dəyişəndə o biri köhnə qalır (CLAUDE.md — «Rol Prioriteti»).
            var mailIcazesiVar = User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Rehber);
            if (!mailIcazesiVar && appUser != null)
            {
                var pr = await _permission.HasPermissionAsync(appUser.Id, MailIcazesiAttribute.IcazeKodu);
                mailIcazesiVar = pr.Success && pr.Data;
            }
            ViewBag.MailIcazesiVar = mailIcazesiVar;

            return View(isci);
        }

        // ── POST /User/Profile/MailAyarlariYenile (AJAX) ──────
        // ⚠️ 01.09.2026: bu iki endpoint ƏVVƏL heç bir rol/icazə yoxlamasından
        // keçmirdi — yalnız Profil səhifəsindəki KART gizlədilirdi. Yəni icazəsi
        // olmayan istifadəçi birbaşa POST göndərib özünə SMTP qura bilərdi və
        // «Gələn Maillər» sinxronizasiyası onun üçün də işləyərdi. Kartı gizlətmək
        // qoruma DEYİL; qoruma buradadır.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [MailIcazesi]
        public async Task<IActionResult> MailAyarlariYenile(string? mailSmtpEmail, string? mailSmtpParol, string? mailSmtpHost)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return Json(new { success = false, message = "İstifadəçi tapılmadı." });

            appUser.MailSmtpEmail = mailSmtpEmail?.Trim();
            appUser.MailSmtpHost  = string.IsNullOrWhiteSpace(mailSmtpHost) ? null : mailSmtpHost.Trim();

            if (!string.IsNullOrWhiteSpace(mailSmtpParol))
                appUser.MailSmtpParol = _protector.Protect(mailSmtpParol);

            await _userManager.UpdateAsync(appUser);
            return Json(new { success = true, message = "Mail ayarları yadda saxlandı." });
        }

        // ── POST /User/Profile/MailSina (AJAX) ────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [MailIcazesi]
        public async Task<IActionResult> MailSina()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return Json(new { success = false, message = "İstifadəçi tapılmadı." });

            if (string.IsNullOrWhiteSpace(appUser.MailSmtpEmail) || string.IsNullOrWhiteSpace(appUser.MailSmtpParol))
                return Json(new { success = false, message = "Əvvəlcə mail və şifrəni yadda saxlayın." });

            var parol = _protector.Unprotect(appUser.MailSmtpParol);

            var (ok, xeta) = await _smtp.GonderAsync(
                kimeEmail: appUser.MailSmtpEmail,
                kimeAd: $"{appUser.Ad} {appUser.Soyad}".Trim(),
                movzu: "FinNex — SMTP sınaq maili",
                metin: $"Salam!\n\nBu mail SMTP konfiqurasiyasının yoxlanması üçün göndərilmişdir.\nSistem: FinNex\nTarix: {DateTime.Now:dd.MM.yyyy HH:mm}",
                fromEmail: appUser.MailSmtpEmail!,
                fromParol: parol,
                smtpHost: appUser.MailSmtpHost);

            return Json(new { success = ok, message = ok ? "Sınaq maili göndərildi!" : $"Xəta: {xeta}" });
        }

        // ── POST /User/Profile/UpdateContact (AJAX) ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateContact(string? telefon, string? email, string? unvan)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null)
                return Json(new { success = false, message = "İstifadəçi tapılmadı." });

            var isci = await _unitOfWork.Repository<Isci>().IdIleGetirAsync(isciId.Value);
            if (isci == null)
                return Json(new { success = false, message = "İşçi məlumatları tapılmadı." });

            isci.Telefon = telefon?.Trim();
            isci.Email = email?.Trim();
            isci.Unvan = unvan?.Trim();

            await _unitOfWork.Repository<Isci>().YenileAsync(isci);
            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "Əlaqə məlumatları yeniləndi." });
        }

        // ══ Köməkçi metodlar ══════════════════════════════════

        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }

        private IActionResult RedirectToLogin() =>
            RedirectToAction("Login", "Account", new { area = "" });
    }
}
