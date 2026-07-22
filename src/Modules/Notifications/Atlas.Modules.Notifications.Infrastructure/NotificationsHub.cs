using Microsoft.AspNetCore.SignalR;

namespace Atlas.Modules.Notifications.Infrastructure;

public class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[SignalR] İstemci bağlandı: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[SignalR] İstemci ayrıldı: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}