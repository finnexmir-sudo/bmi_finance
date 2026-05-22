using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FinNex.UI.Hubs;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var isciId = Context.GetHttpContext()?.Request.Query["isciId"].ToString();
        if (!string.IsNullOrEmpty(isciId))
        {
            Context.Items["isciId"] = isciId;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"desktopUser_{isciId}");
            _logger.LogInformation("Desktop agent qoşuldu: isciId={IsciId} connectionId={ConnectionId}",
                isciId, Context.ConnectionId);
        }
        else
        {
            _logger.LogWarning("Desktop agent qoşuldu amma isciId yoxdur: connectionId={ConnectionId}",
                Context.ConnectionId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("isciId", out var val) && val is string isciId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"desktopUser_{isciId}");
            _logger.LogInformation("Desktop agent ayrıldı: isciId={IsciId} connectionId={ConnectionId}",
                isciId, Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
