using Atlas.Modules.Notifications.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace Atlas.Modules.Notifications.Api;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        // İstemciler (React) bu adrese SignalR client'ı ile bağlanacak.
        // Normal bir HTTP endpoint'i değil - SignalR bu adresi kendi
        // protokolüyle (WebSocket vb.) yönetiyor.
        app.MapHub<NotificationsHub>("/hubs/notifications");

        return app;
    }
}