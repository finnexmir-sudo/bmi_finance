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

    public SignalRDesktopBildirisService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PushAsync(int isciId, string bashliq, string metn)
    {
        if (isciId <= 0) return;

        await _hubContext.Clients
            .Group($"desktopUser_{isciId}")
            .SendAsync("ReceiveDesktopNotification", new
            {
                bashliq,
                metn,
                tarix = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });
    }
}
