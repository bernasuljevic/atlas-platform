using Atlas.Modules.AI.Application.Documents.Commands;
using Atlas.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Atlas.Modules.AI.Infrastructure;

/// <summary>
/// WikiPageDeletedEventHandler'ın Documents tarafındaki karşılığı - BİREBİR
/// aynı desen (best-effort temizlik, hata yutuluyor).
/// </summary>
public class DocumentDeletedEventHandler : INotificationHandler<DocumentDeletedEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<DocumentDeletedEventHandler> _logger;

    public DocumentDeletedEventHandler(ISender sender, ILogger<DocumentDeletedEventHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(DocumentDeletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _sender.Send(new DeleteDocumentEmbeddingsCommand(notification.DocumentId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DocumentId {DocumentId} için embedding temizliği başarısız oldu - belge silme etkilenmedi.",
                notification.DocumentId);
        }
    }
}
