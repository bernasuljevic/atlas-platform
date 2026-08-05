using Atlas.Modules.Wiki.Domain.Entities;

namespace Atlas.Modules.Wiki.Application.Abstractions;

public interface IWikiFolderRepository
{
    Task<WikiFolder?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Klasör ağacı sorgusu (Gün 3) için - bir departmanın TÜM klasörlerini
    // (hiyerarşi düz bir liste olarak) çeker, ağaç Handler'da bellekte kurulur.
    Task<IReadOnlyList<WikiFolder>> GetByDepartmentAsync(string departmentName, CancellationToken ct = default);

    // SaveChangesAsync BİLEREK burada yok - IWikiPageRepository'deki ile aynı
    // gerekçe, kalıcı hale getirmek IUnitOfWork'ün sorumluluğu.
    Task AddAsync(WikiFolder folder, CancellationToken ct = default);
}
