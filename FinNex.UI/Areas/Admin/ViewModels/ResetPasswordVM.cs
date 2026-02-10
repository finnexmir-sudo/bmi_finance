using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.Admin.ViewModels;

public class ResetPasswordVM
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Please confirm the new password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = null!;
}
