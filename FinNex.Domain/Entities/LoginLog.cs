namespace FinNex.Domain.Entities;

public class LoginLog
{
    public int Id { get; set; }
    public string UserName { get; set; } = null!;
    public string? FullName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccess { get; set; }
    public string? FailReason { get; set; }
    public DateTime LoginTime { get; set; } = DateTime.Now;
}
