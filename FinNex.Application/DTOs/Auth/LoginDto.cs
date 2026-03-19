
namespace FinNex.Application.DTOs.Auth
{
    public class LoginDto
    {
        public string? UserName { get; set; }
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
