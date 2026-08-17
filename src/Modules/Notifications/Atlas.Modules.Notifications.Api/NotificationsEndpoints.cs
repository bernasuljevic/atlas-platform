using Atlas.Modules.Notifications.Application.Notifications.Commands;
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

        // Header'daki zil ikonundaki unread badge için (2026-08-17) - AYRI
        // bir endpoint, GET /api/notifications'ın (Take ile sınırlı) DTO
        // listesini TAMAMEN çekmeden sadece sayıyı öğrenebilmek için.
        app.MapGet("/api/notifications/unread-count", async (IMediator mediator) =>
        {
            var count = await mediator.Send(new GetUnreadNotificationCountQuery());
            return Results.Ok(new { count });
        })
        .WithName("GetUnreadNotificationCount")
        .RequireAuthorization();

        app.MapPost("/api/notifications/{id:guid}/read", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new MarkNotificationReadCommand(id));
            return Results.NoContent();
        })
        .WithName("MarkNotificationRead")
        .RequireAuthorization();

        app.MapPost("/api/notifications/read-all", async (IMediator mediator) =>
        {
            await mediator.Send(new MarkAllNotificationsReadCommand());
            return Results.NoContent();
        })
        .WithName("MarkAllNotificationsRead")
        .RequireAuthorization();

        return app;
    }
}
