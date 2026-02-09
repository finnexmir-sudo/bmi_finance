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
            return View();
        }

        // ======================
        // LOGIN (POST)
        // ======================
        [HttpPost]
        [EnableRateLimiting("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null || !user.Aktivdir)
            {
                ModelState.AddModelError("", "Email və ya şifrə yanlışdır");
                return View(dto);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                dto.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Email və ya şifrə yanlışdır");
                return View(dto);
            }

            return RedirectToAction("Index", "Home");
        }

        // ======================
        // REGISTER (GET)
        // ======================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
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

            var user = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Ad = dto.Ad,
                Soyad = dto.Soyad,
                Aktivdir = true,
                EmailConfirmed = true
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
    }
}
