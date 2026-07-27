using Atlas.Modules.AI.Application.WikiPages.Commands;
using Atlas.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Atlas.Modules.AI.Infrastructure;

/// <summary>
/// WikiPageCreatedEventHandler ile BİREBİR aynı desen (aynı gerekçelerle
/// try/catch ile hatayı yutuyor - sayfa silme her zaman başarılı sonuçlanmalı,
/// AI'ın embedding temizliği başarısız olsa bile). Bu da Outbox Pattern'in
/// çözdüğü aynı sınıf teknik borç: temizlik best-effort, kalıcı/retry
/// edilebilir değil.
/// </summary>
public class WikiPageDeletedEventHandler : INotificationHandler<WikiPageDeletedEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<WikiPageDeletedEventHandler> _logger;

    public WikiPageDeletedEventHandler(ISender sender, ILogger<WikiPageDeletedEventHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(WikiPageDeletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _sender.Send(new DeleteWikiPageEmbeddingsCommand(notification.PageId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "WikiPageId {WikiPageId} için embedding temizliği başarısız oldu - sayfa silme etkilenmedi.",
                notification.PageId);
        }
    }
}
