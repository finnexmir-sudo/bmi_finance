namespace FinNex.UI.Areas.Admin.ViewModels;

public class DashboardVM
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int TotalRoles { get; set; }
    public List<RecentUserVM> RecentUsers { get; set; } = new();
}

public class RecentUserVM
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public DateTime RegisteredAt { get; set; }
    public bool IsActive { get; set; }
}
