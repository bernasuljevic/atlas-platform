using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

/// <summary>
/// POST /api/wiki/reindex'in Documents modülündeki karşılığı - AMA bulk/Admin
/// bir bakım aracı DEĞİL, TEK bir belgeyi hedefliyor ve owner-or-Admin (Delete/
/// Update ile AYNI yetki deseni). Gerekçe: Wiki'nin reindex'i "embedding
/// sağlayıcısı değişti, HERKESİ yeniden işle" senaryosu için var; Documents'ta
/// asıl ihtiyaç "bu TEK belge Failed durumunda kaldı (ör. o an desteklenmeyen
/// bir uzantıydı, sonradan bir processor eklendi ya da geçici bir disk hatası
/// vardı) - sahibi/Admin elle yeniden tetiklesin" - bu yüzden Admin-only bulk
/// yerine owner-or-Admin tekil bir komut seçildi.
///
/// Handler YENİ bir extraction akışı YAZMIYOR - DocumentUploadedEvent'i (var
/// olan StorageKey/ContentType/FileExtension ile) Outbox'a yeniden yazıyor,
/// zaten var olan DocumentUploadedEventHandler (Documents.Infrastructure) bunu
/// ilk yüklemedekiyle BİREBİR AYNI şekilde işliyor - Wiki'nin reindex'inin
/// WikiPageCreatedEvent'i yeniden yayınlamasıyla AYNI fikir.
/// </summary>
public record ReprocessDocumentCommand(Guid Id) : IRequest, IAuditableCommand
{
    public string AuditAction => "Document.ReprocessRequested";
    public string? AuditResourceId => Id.ToString();
    public string? AuditDetails { get; set; }
}
