using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Shared.Contracts;
using Atlas.Shared.Text.Chunking;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Atlas.Modules.Documents.Infrastructure;

/// <summary>
/// Wiki'nin WikiPageCreatedEventHandler'ıyla (AI.Infrastructure) AYNI desende
/// çalışan, ama Documents'ın KENDİ İÇİNDE yaşayan bir abone - DocumentUploadedEvent'i
/// Documents'ın kendi Outbox'ı yayınlıyor, kendi Infrastructure'ı dinliyor
/// (cross-module DEĞİL, aynı modülün Api->Infrastructure sınırı içinde).
///
/// Akış: Extracting'e geç (kalıcı hale getir - yavaş bir extraction'da bu
/// durum gerçekten GÖZLENEBİLİR olsun diye AYRI bir SaveChanges) -> uygun
/// IDocumentProcessor'ı bul -> metni çıkar -> TextChunker (Shared.Text) ile
/// chunk'la -> DocumentChunksReadyEvent'i KENDİ Outbox'ına yaz (AI, Gün 5'te
/// buna abone olacak) -> Ready'e geç. Hata durumunda (desteklenmeyen uzantı,
/// bozuk dosya vb.) Failed + ProcessingError - RETHROW EDİLMİYOR, aksi halde
/// OutboxProcessor bu mesajı "başarısız" sayıp 5 kez yeniden dener (kalıcı
/// bozuk bir dosya için anlamsız) - Wiki'nin AI/Notifications event
/// handler'larındaki "hatayı yut, logla" best-effort deseniyle AYNI gerekçe.
/// </summary>
public class DocumentUploadedEventHandler : INotificationHandler<DocumentUploadedEvent>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IEnumerable<IDocumentProcessor> _processors;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentUploadedEventHandler> _logger;

    public DocumentUploadedEventHandler(
        IDocumentRepository documentRepository, IFileStorageService fileStorageService,
        IEnumerable<IDocumentProcessor> processors, IOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork, ILogger<DocumentUploadedEventHandler> logger)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _processors = processors;
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DocumentUploadedEvent notification, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(notification.DocumentId, cancellationToken);
        if (document is null)
        {
            // Belge, bu event işlenene kadar (ör. kullanıcı hemen sildi) ortadan
            // kalkmış olabilir - hata değil, sessizce çık.
            return;
        }

        document.MarkExtracting();
        await _documentRepository.UpdateAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var processor = _processors.FirstOrDefault(p => p.CanProcess(notification.FileExtension));
            if (processor is null)
                throw new NotSupportedException($"'{notification.FileExtension}' uzantısı için bir içerik işleyici kayıtlı değil.");

            await using var stream = await _fileStorageService.OpenReadAsync(notification.StorageKey, cancellationToken);
            var extractedText = await processor.ExtractAsync(stream, cancellationToken);

            if (string.IsNullOrWhiteSpace(extractedText))
                throw new InvalidOperationException("Belgeden çıkarılabilir bir metin bulunamadı (dosya boş olabilir).");

            var chunks = TextChunker.Chunk(extractedText);

            // Belgenin KENDİSİYLE (Ready durumu) AYNI transaction'da - AI,
            // Documents'ın Status=Ready dediği bir belgenin embedding'inin
            // henüz üretilmemiş olabileceğini kabul ediyor (Wiki'nin "sayfa
            // hemen görünür, embedding birkaç saniye sonra" eventual-consistency
            // modeliyle tutarlı, bkz. plan notu).
            _outboxWriter.Enqueue(new DocumentChunksReadyEvent(
                document.Id, chunks, notification.Title, notification.DepartmentName, notification.Visibility));

            document.MarkReady();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document {DocumentId} işlenemedi.", notification.DocumentId);
            document.MarkFailed(ex.Message);
        }

        await _documentRepository.UpdateAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
