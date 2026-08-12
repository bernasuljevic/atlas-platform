using MediatR;

namespace Atlas.Shared.Contracts;

/// <summary>
/// DocumentUploadedEventHandler (Documents.Infrastructure), içerik çıkarımı +
/// chunking'i BİTİRDİKTEN sonra bu event'i yayınlıyor - AI modülü (Gün 5'te)
/// buna abone olup embedding üretecek. WikiPageCreatedEvent'in "Content" alanı
/// gibi düşünülebilir, tek fark: tek bir string değil, ZATEN CHUNK'LANMIŞ bir
/// liste taşıyor (AI, TextChunker'ı Documents için TEKRAR çalıştırmak zorunda
/// kalmasın diye - chunking Documents'ın kendi işi, format-özel extraction'ın
/// hemen ardından yapılması gerekiyor).
/// </summary>
public record DocumentChunksReadyEvent(
    Guid DocumentId, IReadOnlyList<string> ChunkTexts, string Title, string DepartmentName, string Visibility) : INotification;
