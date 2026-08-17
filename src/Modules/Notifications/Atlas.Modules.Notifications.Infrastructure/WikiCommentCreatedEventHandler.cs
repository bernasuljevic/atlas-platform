using Atlas.Modules.Notifications.Application.Abstractions;
using Atlas.Modules.Notifications.Domain.Entities;
using Atlas.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Atlas.Modules.Notifications.Infrastructure;

/// <summary>
/// WikiPageCreatedEventHandler ile AYNI iskelet (SignalR + best-effort kalıcı
/// yazma), TEK farkı: WikiPageCreatedEvent BROADCAST (herkese açık) bir
/// bildirim yazarken, bu handler event'in TAŞIDIĞI RecipientUserIds
/// listesindeki HER kullanıcı için AYRI, HEDEFLENMİŞ (TargetUserId dolu) bir
/// NotificationEntry yazıyor - "tartışmaya cevap geldi" bildirimi SADECE
/// ilgili kişileri (sayfa sahibi + önceki yorumcular) ilgilendiriyor, bir
/// departmana broadcast etmek YANLIŞ olurdu.
///
/// SignalR - NotificationsHub şu an kullanıcı-bazlı hedefleme (Clients.User)
/// KURULU DEĞİL (sadece Clients.All broadcast var, bkz. WikiPageCreatedEventHandler) -
/// bu yüzden BİLEREK targeted bir realtime push YAPILMIYOR, sadece kalıcı
/// kayıt yazılıyor. İstemci, header'daki zil ikonunun periyodik/panel-açılışı
/// fetch'iyle (bkz. frontend) yeni bildirimi görecek - true per-user SignalR
/// targeting AYRI, gelecekteki bir iş (IUserIdProvider kurulumu gerektirir).
/// </summary>
public class WikiCommentCreatedEventHandler : INotificationHandler<WikiCommentCreatedEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<WikiCommentCreatedEventHandler> _logger;

    public WikiCommentCreatedEventHandler(
        INotificationRepository notificationRepository, ILogger<WikiCommentCreatedEventHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task Handle(WikiCommentCreatedEvent notification, CancellationToken cancellationToken)
    {
        var title = notification.PageTitle ?? "Anasayfa Tartışması";
        var resourceId = notification.PageId ?? Guid.Empty;

        foreach (var recipientId in notification.RecipientUserIds)
        {
            try
            {
                var entry = NotificationEntry.Create(
                    "DiscussionReply", resourceId, title, notification.DepartmentName, notification.Visibility,
                    notification.AuthorEmail, recipientId);
                await _notificationRepository.AddAsync(entry, cancellationToken);
            }
            catch (Exception ex)
            {
                // WikiPageCreatedEventHandler'daki AYNI best-effort gerekçe -
                // bir alıcı için yazma başarısız olsa bile DİĞER alıcılar
                // etkilenmemeli, yorum ekleme işlemi zaten tamamlandı.
                _logger.LogWarning(
                    ex,
                    "CommentId {CommentId} için {RecipientId} kullanıcısına bildirim yazılamadı.",
                    notification.CommentId, recipientId);
            }
        }
    }
}
