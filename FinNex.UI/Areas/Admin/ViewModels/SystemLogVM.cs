namespace FinNex.UI.Areas.Admin.ViewModels;

public class SystemLogVM
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = null!;
    public string Message { get; set; } = null!;
}

public class SystemLogPageVM
{
    public List<SystemLogVM> Logs { get; set; } = new();
    public string? SelectedLevel { get; set; }
    public string? SearchTerm { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 50;
}
