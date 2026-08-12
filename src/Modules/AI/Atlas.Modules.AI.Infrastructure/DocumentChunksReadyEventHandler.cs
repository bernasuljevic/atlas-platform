using Atlas.Modules.AI.Application.Documents.Commands;
using Atlas.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Atlas.Modules.AI.Infrastructure;

/// <summary>
/// WikiPageCreatedEventHandler'ın Documents tarafındaki karşılığı - Documents
/// modülünün kendi Outbox'ının yayınladığı DocumentChunksReadyEvent'i dinler
/// (Documents.Infrastructure'daki DocumentUploadedEventHandler bunu, format-özel
/// extraction + chunking BİTTİKTEN sonra yazıyor - AI hiçbir zaman ham dosya
/// içeriğiyle uğraşmıyor, bkz. event'teki mimari sınır notu).
///
/// AYNI best-effort gerekçe: embedding üretimi başarısız olursa (ör. Postgres
/// o an erişilemez), belge zaten Documents tarafında "Ready" işaretlenmiş
/// olacak - burada hatayı YUTUYORUZ (loglayıp durduruyoruz), aksi halde
/// OutboxProcessor'ın "başarısız" sayıp bu mesajı yeniden denemesi anlamsız
/// olurdu (extraction'ın kendisi başarılıydı, sorun sadece AI tarafında).
/// </summary>
public class DocumentChunksReadyEventHandler : INotificationHandler<DocumentChunksReadyEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<DocumentChunksReadyEventHandler> _logger;

    public DocumentChunksReadyEventHandler(ISender sender, ILogger<DocumentChunksReadyEventHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(DocumentChunksReadyEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _sender.Send(
                new GenerateDocumentEmbeddingsCommand(
                    notification.DocumentId, notification.ChunkTexts, notification.Title,
                    notification.DepartmentName, notification.Visibility),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DocumentId {DocumentId} için embedding üretimi başarısız oldu - belge işleme etkilenmedi.",
                notification.DocumentId);
        }
    }
}
