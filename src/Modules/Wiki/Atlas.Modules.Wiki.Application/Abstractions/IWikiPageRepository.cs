using Atlas.Modules.Wiki.Domain.Entities;

namespace Atlas.Modules.Wiki.Application.Abstractions;

public interface IWikiPageRepository
{
    Task<IReadOnlyList<WikiPage>> GetAllAsync(CancellationToken ct = default);
    Task<WikiPage?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // DİKKAT (Outbox Pattern Gün 2): AddAsync/DeleteAsync artık kendi
    // SaveChangesAsync()'ini ÇAĞIRMIYOR - sadece DbContext'in change tracker'ına
    // ekliyor/çıkarıyor. Kalıcı hale getirmek çağıranın (Handler) sorumluluğu -
    // IOutboxWriter.Enqueue ile AYNI SaveChanges'e (bkz. IUnitOfWork) dahil
    // edilebilsin diye, atomiklik böyle sağlanıyor.
    Task AddAsync(WikiPage page, CancellationToken ct = default);
    Task DeleteAsync(WikiPage page, CancellationToken ct = default);
}
