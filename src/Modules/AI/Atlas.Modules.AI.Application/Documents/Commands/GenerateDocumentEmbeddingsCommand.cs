using MediatR;

namespace Atlas.Modules.AI.Application.Documents.Commands;

/// <summary>
/// GenerateWikiPageEmbeddingsCommand'ın Documents tarafındaki karşılığı - ama
/// TextChunker'ı BURADA ÇAĞIRMIYOR. Wiki'de chunking AI'ın işiydi (ham
/// Content'i alıp bölüyordu); Documents'ta chunking ZATEN Documents.Infrastructure'da
/// (DocumentUploadedEventHandler) yapıldı - format-özel extraction'ın hemen
/// ardından, çünkü PDF/DOCX/PPTX/XLSX'ten metin çıkarmak Documents'ın işi, AI
/// hiçbir zaman ham dosya içeriğiyle uğraşmıyor (bkz. DocumentChunksReadyEvent'teki
/// mimari sınır notu). Bu yüzden burada `Content` değil, zaten bölünmüş
/// `ChunkTexts` alıyoruz - istemci bu Command'ı hiç bilmiyor, sadece
/// DocumentChunksReadyEventHandler (AI.Infrastructure) tetikliyor.
/// </summary>
public record GenerateDocumentEmbeddingsCommand(
    Guid DocumentId, IReadOnlyList<string> ChunkTexts, string Title, string DepartmentName, string Visibility) : IRequest;
