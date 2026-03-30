using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.Admin.ViewModels;

public class ChangeUserRoleVM
{
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public string FullName { get; set; } = "";

    public IList<string> CurrentRoles { get; set; } = new List<string>();
    public List<string> SelectedRoles { get; set; } = new();
    public List<string> AvailableRoles { get; set; } = new();
}
