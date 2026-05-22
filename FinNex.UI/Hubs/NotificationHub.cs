using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FinNex.UI.Hubs;

/// <summary>
/// Masaüstü köməkçi proqram üçün SignalR Hub-u.
/// JWT Bearer ilə autentifikasiya tələb olunur.
/// IsciId claims-dən yox, query string-dən oxunur:
///   ?access_token=...&isciId=42
/// Beləliklə JWT claim mapping fərqliliklərindən asılı olmur.
/// IsciId qoşulma zamanı Context.Items-ə saxlanılır ki,
/// OnDisconnectedAsync-də yenidən HTTP context-ə müraciət lazım olmasın.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var isciId = Context.GetHttpContext()?.Request.Query["isciId"].ToString();
        if (!string.IsNullOrEmpty(isciId))
        {
            Context.Items["isciId"] = isciId;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"desktopUser_{isciId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("isciId", out var val) && val is string isciId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"desktopUser_{isciId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
