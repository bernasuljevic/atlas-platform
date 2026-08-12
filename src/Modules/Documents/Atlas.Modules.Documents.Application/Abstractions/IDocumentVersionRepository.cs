using Atlas.Modules.Documents.Domain.Entities;

namespace Atlas.Modules.Documents.Application.Abstractions;

public interface IDocumentVersionRepository
{
    // IDocumentRepository'nin AddAsync/UpdateAsync'iyle AYNI desen -
    // SaveChangesAsync BİLEREK yok, IUnitOfWork'ün TEK SaveChanges'iyle
    // (genelde Document'ın kendi güncellemesiyle AYNI değişiklik kümesinde)
    // atomik yazılır.
    Task AddAsync(DocumentVersion version, CancellationToken ct = default);

    // En yeniden en eskiye - versiyon geçmişi UI'da genelde bu sırayla gösterilir.
    Task<IReadOnlyList<DocumentVersion>> GetByDocumentIdAsync(Guid documentId, CancellationToken ct = default);

    Task<DocumentVersion?> GetByDocumentIdAndVersionNumberAsync(
        Guid documentId, int versionNumber, CancellationToken ct = default);

    // Bir belge SİLİNİRKEN çağrılıyor - AI'ın DeleteByWikiPageIdAsync'iyle AYNI
    // "bulk delete" deseni (ExecuteDeleteAsync, satırları belleğe çekmeden).
    // ÖNEMLİ: bu SADECE DB satırlarını siler, diskteki dosyaları SİLMEZ -
    // DeleteDocumentCommandHandler bu metottan ÖNCE GetByDocumentIdAsync ile
    // versiyonları çekip her birinin dosyasını ayrı ayrı silmeli.
    Task DeleteAllForDocumentAsync(Guid documentId, CancellationToken ct = default);
}
