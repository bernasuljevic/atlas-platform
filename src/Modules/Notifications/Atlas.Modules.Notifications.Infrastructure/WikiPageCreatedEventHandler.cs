using Atlas.Modules.Notifications.Application.Abstractions;
using Atlas.Modules.Notifications.Domain.Entities;
using Atlas.Shared.Contracts;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Atlas.Modules.Notifications.Infrastructure;

/// <summary>
/// Wiki modülünün yayınladığı WikiPageCreatedEvent'i dinler ve bağlı tüm
/// istemcilere SignalR üzerinden bir bildirim gönderir. Wiki bu sınıfın
/// varlığından habersiz - sadece event'i "havaya" yayınladı, biz burada onu
/// yakalayıp SignalR'a özgü bir işe (Hub üzerinden mesaj gönderme) çeviriyoruz.
///
/// 2026-08-15: Artık AYRICA kalıcı bir NotificationEntry de yazıyor (kullanıcı
/// isteği - "diğerlerinin yazdıkları" bildirim geçmişi). Kalıcı yazma
/// best-effort - AI'ın WikiPageCreatedEventHandler'ıyla AYNI gerekçe
/// (try/catch, rethrow YOK): bu yazı başarısız olsa bile Outbox'ın kendisi
/// tekrar denemeyecek/kalıcı olarak başarısız SAYMAYACAK, sadece o bildirim
/// geçmişte görünmeyecek - kritik bir işlem değil.
/// </summary>
public class WikiPageCreatedEventHandler : INotificationHandler<WikiPageCreatedEvent>
{
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<WikiPageCreatedEventHandler> _logger;

    public WikiPageCreatedEventHandler(
        IHubContext<NotificationsHub> hubContext, INotificationRepository notificationRepository,
        ILogger<WikiPageCreatedEventHandler> logger)
    {
        _hubContext = hubContext;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task Handle(WikiPageCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Reindex yeniden-yayını GERÇEK bir "yeni sayfa" değil (bkz.
        // WikiPageCreatedEvent'teki IsReindexReplay notu) - ne SignalR toast'ı
        // ne kalıcı bildirim kaydı burada uygun, ikisi de haftalar önce
        // oluşturulmuş bir sayfayı "az önce oluşturuldu" gibi gösterirdi.
        if (notification.IsReindexReplay)
            return;

        // Clients.All: şu an bağlı olan HERKESE gönder (departman/kullanıcı ayrımı yok,
        // ileride "sadece o departmandaki kullanıcılara gönder" gibi bir hedefleme
        // eklenebilir - Clients.Group(...) ile).
        // "WikiPageCreated": istemci tarafında dinlenecek olay adı - React'ta
        // connection.on("WikiPageCreated", ...) ile eşleşecek, birazdan bakacağız.
        await _hubContext.Clients.All.SendAsync(
            "WikiPageCreated",
            new { notification.PageId, notification.Title, notification.DepartmentName },
            cancellationToken);

        try
        {
            var entry = NotificationEntry.Create(
                "WikiPageCreated", notification.PageId, notification.Title, notification.DepartmentName,
                notification.Visibility, notification.CreatedByEmail);
            await _notificationRepository.AddAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Bildirim geçmişine kalıcı kayıt yazılamadı (WikiPageId={PageId}) - SignalR toast'ı yine de gönderildi.",
                notification.PageId);
        }
    }
}