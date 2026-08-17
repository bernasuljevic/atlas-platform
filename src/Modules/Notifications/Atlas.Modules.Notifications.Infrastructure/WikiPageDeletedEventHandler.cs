using Atlas.Modules.Notifications.Application.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Atlas.Modules.Notifications.Infrastructure;

/// <summary>
/// AI.Infrastructure'daki WikiPageDeletedEventHandler ile BIREBIR ayni desen
/// (try/catch, rethrow YOK - sayfa silme her zaman basarili sonuclanmali,
/// bildirim gecmisi temizligi basarisiz olsa bile).
///
/// 2026-08-17'de bulunan gercek bir bug'in duzeltmesi: Wiki modulu bir sayfa
/// silinince WikiPageDeletedEvent yayinliyordu, AI bunu dinleyip kendi
/// embedding'lerini temizliyordu ama Notifications hic dinlemiyordu - silinen
/// her sayfanin WikiPageCreatedEventHandler'in yazdigi kalici NotificationEntry
/// kaydi sonsuza kadar tabloda "yetim" olarak kaliyordu (canli dogrulandi:
/// birden fazla test/temizlik dongusunde biriken 11 yetim kayit bulundu).
/// </summary>
public class WikiPageDeletedEventHandler : INotificationHandler<WikiPageDeletedEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<WikiPageDeletedEventHandler> _logger;

    public WikiPageDeletedEventHandler(
        INotificationRepository notificationRepository, ILogger<WikiPageDeletedEventHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task Handle(WikiPageDeletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationRepository.DeleteAllForResourceAsync(notification.PageId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "WikiPageId {WikiPageId} icin bildirim gecmisi temizligi basarisiz oldu - sayfa silme etkilenmedi.",
                notification.PageId);
        }
    }
}
