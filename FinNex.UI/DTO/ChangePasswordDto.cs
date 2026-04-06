using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.DTO
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "İstifadəçi adı mütləqdir")]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Cari şifrə mütləqdir")]
        public string CurrentPassword { get; set; } = null!;

        [Required(ErrorMessage = "Yeni şifrə mütləqdir")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Yeni şifrənin təkrarı mütləqdir")]
        public string ConfirmNewPassword { get; set; } = null!;
    }
}
