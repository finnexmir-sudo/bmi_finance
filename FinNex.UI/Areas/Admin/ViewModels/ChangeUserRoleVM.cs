using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.Admin.ViewModels;

public class ChangeUserRoleVM
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public IList<string> CurrentRoles { get; set; } = new List<string>();

    [Required(ErrorMessage = "Role is required.")]
    public string SelectedRole { get; set; } = null!;

    public List<string> AvailableRoles { get; set; } = new();
}
