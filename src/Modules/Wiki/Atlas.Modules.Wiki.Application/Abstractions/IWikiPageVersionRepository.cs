using Atlas.Modules.Wiki.Domain.Entities;

namespace Atlas.Modules.Wiki.Application.Abstractions;

public interface IWikiPageVersionRepository
{
    // IWikiPageRepository.AddAsync'teki AYNI desen - SaveChangesAsync BİLEREK
    // yok, IUnitOfWork'ün TEK SaveChanges'iyle (genelde WikiPage'in kendi
    // güncellemesiyle AYNI değişiklik kümesinde) atomik yazılır.
    Task AddAsync(WikiPageVersion version, CancellationToken ct = default);

    // En yeniden en eskiye - versiyon geçmişi UI'da genelde bu sırayla gösterilir.
    Task<IReadOnlyList<WikiPageVersion>> GetByWikiPageIdAsync(Guid wikiPageId, CancellationToken ct = default);

    Task<WikiPageVersion?> GetByWikiPageIdAndVersionNumberAsync(
        Guid wikiPageId, int versionNumber, CancellationToken ct = default);

    // Bir sayfa SİLİNİRKEN çağrılıyor - DocumentVersion'ın DeleteAllForDocumentAsync'iyle
    // AYNI "bulk delete" deseni (ExecuteDeleteAsync, satırları belleğe çekmeden).
    Task DeleteAllForWikiPageAsync(Guid wikiPageId, CancellationToken ct = default);
}
