namespace Atlas.Modules.Documents.Domain.Enums;

// Bir belgenin işlenme durumu - P4'te (Document processing pipeline) devreye
// giren gerçek geçişler: Uploaded (yüklendi, henüz metin çıkarımı başlamadı) ->
// Extracting (IDocumentProcessor çalışıyor) -> Ready (metin çıkarıldı, chunk'landı,
// AI'a devredildi - embedding'in BİTMESİNİ beklemiyoruz, bkz. proje notundaki
// "eventual consistency" kararı) ya da Failed (ProcessingError dolu, RETHROW
// edilmeden dead-letter'a düşürülmeden - admin/owner POST .../reprocess ile
// yeniden tetikleyebilir). Bugün (P3 Gün 1) sadece enum tanımlı - geçişleri
// tetikleyen Outbox/event akışı P4'te geliyor.
public enum DocumentStatus
{
    Uploaded = 0,
    Extracting = 1,
    Ready = 2,
    Failed = 3,
}
