using Atlas.Modules.AI.Application.WikiPages.Commands;
using Atlas.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Atlas.Modules.AI.Infrastructure;

/// <summary>
/// Wiki modülünün yayınladığı WikiPageCreatedEvent'i dinler ve embedding
/// üretimini tetikler - Notifications.Infrastructure'daki WikiPageCreatedEventHandler
/// ile birebir aynı desen (aynı event'in ikinci abonesi). Wiki, AI'ın var
/// olduğundan habersiz.
///
/// ÖNEMLİ MİMARİ NOT (Gün 6 retrospektifinde tekrar ele alınacak): MediatR'ın
/// varsayılan IPublisher'ı, tüm abonelerini AYNI async çağrı zincirinde,
/// SIRAYLA ve senkron (await ile) çalıştırıyor. Yani şu an bir wiki sayfası
/// oluşturma isteği, HTTP yanıtını dönmeden önce hem Notifications'ın SignalR
/// yayınını HEM DE burada chunk'lama + embedding üretimi + PostgreSQL'e yazma
/// işlemini bitirmesini bekliyor - ayrıca bu, Wiki'nin SQL Server'ından TAMAMEN
/// FARKLI bir veritabanına (Postgres) yazıyor, yani iki veritabanı arasında
/// hiçbir atomicity garantisi yok. Eğer bu try/catch olmasaydı ve embedding
/// üretimi bir hata fırlatsaydı, wiki sayfası SQL Server'a zaten kaydedilmiş
/// olmasına rağmen istemciye "oluşturma başarısız" (500) dönerdi - yanıltıcı
/// ve yanlış bir davranış. Bu yüzden hatayı burada YUTUYORUZ (loglayıp
/// durduruyoruz): sayfa oluşturma her zaman başarılı sonuçlanır, ama embedding
/// üretimi sessizce başarısız olabilir ve BUNU KİMSE FARK ETMEZ. Bu tam olarak
/// Transactional Outbox Pattern'in çözdüğü problem (kalıcı, retry edilebilir
/// bir tetikleyici) - şimdilik bilinçli bir teknik borç olarak bırakıyoruz.
/// </summary>
public class WikiPageCreatedEventHandler : INotificationHandler<WikiPageCreatedEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<WikiPageCreatedEventHandler> _logger;

    public WikiPageCreatedEventHandler(ISender sender, ILogger<WikiPageCreatedEventHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(WikiPageCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _sender.Send(
                new GenerateWikiPageEmbeddingsCommand(
                    notification.PageId, notification.Title, notification.Content,
                    notification.DepartmentName, notification.Visibility),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "WikiPageId {WikiPageId} için embedding üretimi başarısız oldu - sayfa oluşturma etkilenmedi.",
                notification.PageId);
        }
    }
}
