namespace FinNex.UI.Areas.Admin.ViewModels;

public class UserListVM
{
    public int Id { get; set; }
    public string UserName { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsActive { get; set; }
    public DateTime RegisteredAt { get; set; }
    public bool IsLockedOut { get; set; }
    public int? IsciId { get; set; }
}
