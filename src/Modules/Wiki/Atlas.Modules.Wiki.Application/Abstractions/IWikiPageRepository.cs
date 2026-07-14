using Atlas.Modules.Wiki.Domain.Entities;

namespace Atlas.Modules.Wiki.Application.Abstractions;

public interface IWikiPageRepository
{
    Task<IReadOnlyList<WikiPage>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(WikiPage page, CancellationToken ct = default);
}
