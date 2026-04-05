namespace FinNex.UI.Areas.Admin.ViewModels;

public class LoginLogVM
{
    public int Id { get; set; }
    public string UserName { get; set; } = null!;
    public string? FullName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccess { get; set; }
    public string? FailReason { get; set; }
    public DateTime LoginTime { get; set; }
}

public class LoginLogPageVM
{
    public List<LoginLogVM> Logs { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 50;
}
