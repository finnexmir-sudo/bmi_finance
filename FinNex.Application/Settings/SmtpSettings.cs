namespace FinNex.Application.Settings;

public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = false;
    public bool UseStartTls { get; set; } = true;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromName { get; set; } = "FinNex";
    public string FromEmail { get; set; } = "";
}
