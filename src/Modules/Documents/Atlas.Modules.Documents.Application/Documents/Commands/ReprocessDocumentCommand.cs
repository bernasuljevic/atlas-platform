using Atlas.Shared.CQRS.Behaviors;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

/// <summary>
/// TEK bir belgeyi hedefleyen, owner-or-Admin (Delete/Update ile AYNI yetki
/// deseni) bir araç - "bu TEK belge Failed durumunda kaldı (ör. o an
/// desteklenmeyen bir uzantıydı, sonradan bir processor eklendi ya da geçici
/// bir disk hatası vardı) - sahibi/Admin elle yeniden tetiklesin" senaryosu
/// için. **Bulk/Admin-only eşdeğeri artık VAR** - `ReindexDocumentsCommand`
/// ("embedding sağlayıcısı değişti, HERKESİ yeniden işle" senaryosu, Wiki'nin
/// `ReindexWikiPagesCommand`'ıyla aynı gerekçe) - ikisi birbirinin YERİNE
/// geçmiyor, farklı ihtiyaçlara hizmet eden iki ayrı araç.
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
