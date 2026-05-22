using Microsoft.Extensions.Configuration;

namespace FinNex.DesktopAgent;

/// <summary>
/// appsettings.json-dan oxunan konfiqurasiya.
/// Bütün URL-lər burada mərkəzləşdirilib.
/// </summary>
public class AppConfig
{
    public string BaseUrl { get; init; } = "";
    public string TokenEndpoint { get; init; } = "/api/desktop/token";
    public string NotificationHubPath { get; init; } = "/notificationHub";

    public string FullHubUrl => BaseUrl.TrimEnd('/') + NotificationHubPath;
    public string FullTokenUrl => BaseUrl.TrimEnd('/') + TokenEndpoint;

    public static AppConfig Load()
    {
        var cfg = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var section = cfg.GetSection("FinNex");
        return new AppConfig
        {
            BaseUrl = section["BaseUrl"] ?? "",
            TokenEndpoint = section["TokenEndpoint"] ?? "/api/desktop/token",
            NotificationHubPath = section["NotificationHubPath"] ?? "/notificationHub"
        };
    }
}
