using Atlas.Modules.Wiki.Domain.Entities;

namespace Atlas.Modules.Wiki.Application.Abstractions;

public interface IUserPageFavoriteRepository
{
    Task<UserPageFavorite?> GetAsync(Guid userId, Guid wikiPageId, CancellationToken ct = default);

    Task<IReadOnlyList<UserPageFavorite>> GetByUserAsync(Guid userId, CancellationToken ct = default);

    // SaveChangesAsync BİLEREK burada yok - IWikiFolderRepository'deki aynı
    // gerekçe, kalıcı hale getirmek IUnitOfWork'ün sorumluluğu.
    Task AddAsync(UserPageFavorite favorite, CancellationToken ct = default);

    Task RemoveAsync(UserPageFavorite favorite, CancellationToken ct = default);
}
