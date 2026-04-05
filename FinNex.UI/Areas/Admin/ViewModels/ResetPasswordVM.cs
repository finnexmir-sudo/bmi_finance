using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.Admin.ViewModels;

public class ResetPasswordVM
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Şifrə mütləqdir.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifrə")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Please confirm the new password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = null!;
}
