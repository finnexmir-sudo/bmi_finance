using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities;
using FinNex.UI.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FinNex.UI.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly AppDbContext _db;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ILogger<AccountController> logger,
            AppDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _db = db;
        }

        // ======================
        // LOGIN (GET)
        // ======================
        [HttpGet]
        public IActionResult Login()
        {
            var lastUser = Request.Cookies["LastUserName"] ?? "";
            return View(new LoginDto { UserName = lastUser });
        }

        // ======================
        // LOGIN (POST)
        [HttpPost]
        [EnableRateLimiting("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();
            if (userAgent.Length > 500) userAgent = userAgent[..500];

            var user = await _userManager.FindByNameAsync(dto.UserName);

            if (user == null || !user.Aktivdir)
            {
                // Uğursuz giriş — istifadəçi tapılmadı / deaktiv
                await LogLoginAttempt(dto.UserName, null, ip, userAgent, false,
                    user == null ? "İstifadəçi tapılmadı" : "İstifadəçi deaktivdir");

                ModelState.AddModelError("", "İstifadəçi adı və ya şifrə yanlışdır");
                return View(dto);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                dto.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                // Uğursuz giriş — şifrə yanlış / kilidlənib
                var reason = result.IsLockedOut ? "Hesab kilidlənib" : "Yanlış şifrə";
                await LogLoginAttempt(dto.UserName, $"{user.Ad} {user.Soyad}", ip, userAgent, false, reason);

                ModelState.AddModelError("", "İstifadəçi adı və ya şifrə yanlışdır");
                return View(dto);
            }

            // Uğurlu giriş
            await LogLoginAttempt(dto.UserName, $"{user.Ad} {user.Soyad}", ip, userAgent, true, null);

            // İstifadəçi adını yadda saxla (yalnız ad, şifrə yox)
            Response.Cookies.Append("LastUserName", dto.UserName, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                IsEssential = false,
                SameSite = SameSiteMode.Lax
            });

            return RedirectToAction("Index", "Dashboard", new { area = "User" });
        }

        private async Task LogLoginAttempt(string userName, string? fullName, string? ip, string? userAgent, bool success, string? failReason)
        {
            _db.LoginLogs.Add(new LoginLog
            {
                UserName = userName,
                FullName = fullName,
                IpAddress = ip,
                UserAgent = userAgent,
                IsSuccess = success,
                FailReason = failReason,
                LoginTime = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }


        // ======================
        // REGISTER (GET)
        // ======================
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterDto());
        }

        // ======================
        // REGISTER (POST)
        // ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            // Username təkrarı YOXLA
            var exists = await _userManager.FindByNameAsync(dto.UserName);
            if (exists != null)
            {
                ModelState.AddModelError("UserName", "Bu istifadəçi adı artıq mövcuddur");
                return View(dto);
            }

            var user = new AppUser
            {
                UserName = dto.UserName,
                Ad = dto.Ad,
                Soyad = dto.Soyad,
                Email = dto.Email,
                EmailConfirmed = true,
                Aktivdir = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(dto);
            }

            await _userManager.AddToRoleAsync(user, RoleNames.Viewer);

            return RedirectToAction("Login");
        }


        // ======================
        // LOGOUT
        // ======================
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // ======================
        // ACCESS DENIED
        // ======================
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new UI.DTO.ChangePasswordDto());
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(UI.DTO.ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            // İstifadəçi adına görə user tap
            var user = await _userManager.FindByNameAsync(dto.UserName);
            if (user == null)
            {
                ModelState.AddModelError(nameof(dto.UserName), "Bu istifadəçi adı tapılmadı.");
                return View(dto);
            }

            ViewBag.FullName = !string.IsNullOrWhiteSpace(user.Ad)
                ? $"{user.Ad} {user.Soyad}".Trim()
                : user.UserName;

            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                ModelState.AddModelError(nameof(dto.ConfirmNewPassword), "Yeni şifrələr uyğun deyil.");
                return View(dto);
            }

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Şifrəniz uğurla yeniləndi. Yeni şifrə ilə daxil olun.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            TempData["ErrorMessage"] = "Şifrə dəyişdirilə bilmədi. Cari şifrənizi yoxlayın.";
            return View(dto);
        }
    }
}
