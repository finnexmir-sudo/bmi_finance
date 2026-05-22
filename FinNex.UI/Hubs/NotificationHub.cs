using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FinNex.UI.Hubs;

/// <summary>
/// Masaüstü köməkçi proqram üçün SignalR Hub-u.
/// Yalnız JWT Bearer token-i olan əlaqələri qəbul edir.
/// Hər qoşulan müştəri "desktopUser_{isciId}" qrupuna əlavə olunur.
/// Serverdən isci-yə gönderilən metodun adı: ReceiveDesktopNotification
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var isciId = Context.User?.FindFirstValue("isciId");
        if (!string.IsNullOrEmpty(isciId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"desktopUser_{isciId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var isciId = Context.User?.FindFirstValue("isciId");
        if (!string.IsNullOrEmpty(isciId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"desktopUser_{isciId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
