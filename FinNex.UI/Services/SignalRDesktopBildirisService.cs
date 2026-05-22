using FinNex.Application.Interfaces.Communication;
using FinNex.UI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FinNex.UI.Services;

/// <summary>
/// IDesktopBildirisService-in SignalR implementasiyası.
/// IHubContext<NotificationHub>-u inject edərək işçinin bağlı olan masaüstü
/// proqramlarına anlıq mesaj göndərir.
/// Bu klass UI layihəsindədir çünki IHubContext ASP.NET Core-a aiddir.
/// </summary>
public class SignalRDesktopBildirisService : IDesktopBildirisService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRDesktopBildirisService> _logger;

    public SignalRDesktopBildirisService(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRDesktopBildirisService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PushAsync(int isciId, string bashliq, string metn)
    {
        if (isciId <= 0) return;

        var group = $"desktopUser_{isciId}";
        _logger.LogInformation("Desktop push → group={Group} bashliq={Bashliq}", group, bashliq);

        await _hubContext.Clients
            .Group(group)
            .SendAsync("ReceiveDesktopNotification", new
            {
                bashliq,
                metn,
                tarix = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });

        _logger.LogInformation("Desktop push göndərildi → group={Group}", group);
    }
}
