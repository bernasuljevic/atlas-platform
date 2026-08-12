using MediatR;

namespace Atlas.Shared.Contracts;

/// <summary>
/// WikiPageCreatedEvent'in Documents modülündeki karşılığı - AMA tek yönlü
/// DEĞİL, Documents modülünün KENDİ İÇİNDE de bir abonesi var
/// (DocumentUploadedEventHandler, Documents.Infrastructure): bu event hem
/// "belge işlenmeye hazır" sinyalini Documents'ın kendi extraction akışına,
/// hem (Gün 5'te) "embedding üretilmeye hazır" sinyalini AI modülüne taşıyacak
/// - ama AI'a giden asıl event bu DEĞİL, DocumentChunksReadyEvent (extraction
/// BİTTİKTEN sonra yayınlanan, ikinci bir event) - AI hiçbir zaman ham dosya
/// içeriğiyle uğraşmıyor, sadece zaten metne çevrilmiş chunk'ları alıyor
/// (Wiki'nin markdown-to-text kendi işi olması, AI'ın sadece düz metin
/// almasıyla AYNI mimari sınır).
///
/// StorageKey/ContentType/FileExtension - içerik çıkarımı için gereken teknik
/// bilgi. Title/DepartmentName/Visibility - denormalize, WikiPageCreatedEvent'teki
/// AYNI gerekçe (DocumentChunksReadyEvent'e taşınacak, AI'ın Documents DB'sine
/// geri sorgu atmasına gerek kalmasın diye).
/// </summary>
public record DocumentUploadedEvent(
    Guid DocumentId, string StorageKey, string ContentType, string FileExtension,
    string Title, string DepartmentName, string Visibility) : INotification;
