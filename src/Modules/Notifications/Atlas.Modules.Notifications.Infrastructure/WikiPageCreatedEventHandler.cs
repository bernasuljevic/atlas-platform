using Atlas.Shared.Contracts;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Atlas.Modules.Notifications.Infrastructure;

/// <summary>
/// Wiki modülünün yayınladığı WikiPageCreatedEvent'i dinler ve bağlı tüm
/// istemcilere SignalR üzerinden bir bildirim gönderir. Wiki bu sınıfın
/// varlığından habersiz - sadece event'i "havaya" yayınladı, biz burada onu
/// yakalayıp SignalR'a özgü bir işe (Hub üzerinden mesaj gönderme) çeviriyoruz.
/// </summary>
public class WikiPageCreatedEventHandler : INotificationHandler<WikiPageCreatedEvent>
{
    private readonly IHubContext<NotificationsHub> _hubContext;

    public WikiPageCreatedEventHandler(IHubContext<NotificationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task Handle(WikiPageCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Clients.All: şu an bağlı olan HERKESE gönder (departman/kullanıcı ayrımı yok,
        // ileride "sadece o departmandaki kullanıcılara gönder" gibi bir hedefleme
        // eklenebilir - Clients.Group(...) ile).
        // "WikiPageCreated": istemci tarafında dinlenecek olay adı - React'ta
        // connection.on("WikiPageCreated", ...) ile eşleşecek, birazdan bakacağız.
        await _hubContext.Clients.All.SendAsync(
            "WikiPageCreated",
            new { notification.PageId, notification.Title, notification.DepartmentName },
            cancellationToken);
    }
}