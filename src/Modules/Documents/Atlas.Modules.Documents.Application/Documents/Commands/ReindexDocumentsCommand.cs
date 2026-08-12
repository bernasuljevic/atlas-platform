using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Commands;

/// <summary>
/// POST /api/wiki/reindex'in Documents modülündeki karşılığı - Wiki'nin reindex'iyle
/// AYNI senaryo için: "embedding sağlayıcısı değişti (ör. Fake'ten gerçek bir
/// sağlayıcıya geçiş), var olan TÜM belgeler yeni sağlayıcıyla yeniden işlensin."
/// ReprocessDocumentCommand'ın YERİNE geçmiyor - o hâlâ "TEK bir belge Failed
/// kaldı, sahibi/Admin elle tetiklesin" senaryosu için var (owner-or-Admin, tekil).
/// Bu komut Admin-only VE bulk - ikisi farklı ihtiyaçlara hizmet ediyor, bkz.
/// ReprocessDocumentCommand'daki güncellenmiş not.
///
/// Wiki'nin ReindexWikiPagesCommand'ından TEK mimari farkı: Wiki'nin reindex'i
/// (Outbox Pattern'den ÖNCE yazıldığı için) hâlâ IPublisher.Publish kullanıyor -
/// burada BİLEREK IOutboxWriter kullanıyoruz, çünkü Documents'ın geri kalanı
/// (Upload/Delete/Reprocess) zaten Outbox'a yazıyor; bulk bir reindex'i senkron
/// IPublisher.Publish ile yapmak, tam da Outbox'ın çözdüğü "yüzlerce belgeyi
/// tek bir istek içinde, atomiklik/crash-safety garantisi olmadan işleme"
/// riskini geri getirirdi.
/// </summary>
public record ReindexDocumentsCommand : IRequest<int>;
