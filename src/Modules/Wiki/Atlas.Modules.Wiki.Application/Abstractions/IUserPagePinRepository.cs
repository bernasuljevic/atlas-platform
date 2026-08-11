using Atlas.Modules.Wiki.Domain.Entities;

namespace Atlas.Modules.Wiki.Application.Abstractions;

public interface IUserPagePinRepository
{
    Task<UserPagePin?> GetAsync(Guid userId, Guid wikiPageId, CancellationToken ct = default);

    Task<IReadOnlyList<UserPagePin>> GetByUserAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(UserPagePin pin, CancellationToken ct = default);

    Task RemoveAsync(UserPagePin pin, CancellationToken ct = default);
}
