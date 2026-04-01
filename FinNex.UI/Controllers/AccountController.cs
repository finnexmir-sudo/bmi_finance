using FinNex.Domain;
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

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // ======================
        // LOGIN (GET)
        // ======================
        [HttpGet]
        public IActionResult Login()
        {

            return View(new LoginDto());
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

            var user = await _userManager.FindByNameAsync(dto.UserName);
            var roles = await _userManager.GetRolesAsync(user);

            if (user == null || !user.Aktivdir)
            {
                ModelState.AddModelError("", "İstifadəçi adı və ya şifrə yanlışdır");
                return View(dto);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                dto.Password,
                dto.RememberMe,
                lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "İstifadəçi adı və ya şifrə yanlışdır");
                return View(dto);
            }

            //if (roles.Contains("Admin"))
            //{
            //    return RedirectToAction("Index", "Home");
            //}
            ////return RedirectToAction("Index", "Home");

            //// "Index" action-ı, "Home" controller-i, "User" area-sı
            //else
            //{
            //    return RedirectToAction("Index", "Dashboard", new { area = "User" });
            //}
            return RedirectToAction("Index", "Dashboard", new { area = "User" });

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

            // 🔴 Username təkrarı YOXLA
            var exists = await _userManager.FindByNameAsync(dto.UserName);
            if (exists != null)
            {
                ModelState.AddModelError("UserName", "Bu istifadəçi adı artıq mövcuddur");
                return View(dto);
            }

            var user = new AppUser
            {
                UserName = dto.UserName, // 🔥 LOGIN BUNUNLA OLACAQ
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

            await _userManager.AddToRoleAsync(user, "Viewer");

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
[Authorize]
public IActionResult ChangePassword()
{
    return View(new UI.DTO.ChangePasswordDto());
}

// POST
[HttpPost]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ChangePassword(Application.DTOs.Auth.ChangePasswordDto dto)
{
    if (!ModelState.IsValid)
        return View(dto);

    if (dto.NewPassword != dto.ConfirmNewPassword)
    {
        ModelState.AddModelError(nameof(dto.ConfirmNewPassword), "Yeni şifrələr uyğun deyil.");
        return View(dto);
    }

    var user = await _userManager.GetUserAsync(User);
    if (user == null)
        return RedirectToAction("Login");

    var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

    if (result.Succeeded)
    {
        await _signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Şifrəniz uğurla yeniləndi.";
        return RedirectToAction(nameof(ChangePassword));
    }

    foreach (var error in result.Errors)
        ModelState.AddModelError(string.Empty, error.Description);

    TempData["ErrorMessage"] = "Şifrə dəyişdirilə bilmədi. Cari şifrənizi yoxlayın.";
    return View(dto);
}
    }
}
