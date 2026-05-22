using FinNex.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinNex.UI.Controllers;

/// <summary>
/// Masaüstü köməkçi proqram üçün JWT token endpointi.
/// İşçi öz FinNex istifadəçi adı və şifrəsi ilə giriş edir,
/// cavabda NotificationHub-a qoşmaq üçün lazım olan JWT alır.
/// Rate limiting "login" policy-si bu endpoint-ə də tətbiq olunur.
/// </summary>
[ApiController]
[Route("api/desktop")]
public class DesktopTokenController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;

    public DesktopTokenController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    /// <summary>
    /// POST /api/desktop/token
    /// Body: { "username": "...", "password": "..." }
    /// Uğurlu cavab: { "token": "eyJ...", "isciId": 42, "ad": "..." }
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> GetToken([FromBody] DesktopLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "İstifadəçi adı və şifrə tələb olunur." });

        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null || !user.Aktivdir)
            return Unauthorized(new { error = "İstifadəçi adı və ya şifrə yanlışdır." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return StatusCode(429, new { error = "Hesab müvəqqəti bloklanıb. Bir müddət sonra yenidən cəhd edin." });

            return Unauthorized(new { error = "İstifadəçi adı və ya şifrə yanlışdır." });
        }

        if (!user.IsciId.HasValue)
            return Forbid();

        var jwtSection = _configuration.GetSection("DesktopJwt");
        var secret = jwtSection["Secret"];
        if (string.IsNullOrEmpty(secret))
            return StatusCode(500, new { error = "Server konfiqurasiya xətası." });

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("isciId", user.IsciId.Value.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
            new Claim("ad", $"{user.Ad} {user.Soyad}"),
        };

        var expiryHours = double.TryParse(jwtSection["ExpiryHours"], out var h) ? h : 24.0;

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: creds);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            isciId = user.IsciId.Value,
            ad = $"{user.Ad} {user.Soyad}"
        });
    }
}

public class DesktopLoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
