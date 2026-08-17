using Atlas.Modules.Notifications.Application.Notifications.Queries;
using Atlas.Modules.Notifications.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        // Kalıcı bildirim geçmişi (2026-08-15) - token GEREKTİRİYOR, çünkü
        // sonuçlar zaten istek sahibinin departmanına göre filtreleniyor
        // (bkz. GetNotificationsQueryHandler), anonim bir istek kafa
        // karıştırıcı olurdu (AI arama endpoint'iyle AYNI gerekçe).
        app.MapGet("/api/notifications", async (IMediator mediator, int take = 10) =>
        {
            var notifications = await mediator.Send(new GetNotificationsQuery(take));
            return Results.Ok(notifications);
        })
        .WithName("GetNotifications")
        .RequireAuthorization();

        return app;
    }
}