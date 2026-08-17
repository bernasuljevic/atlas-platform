using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence;

public class EfWikiPageVersionRepository : IWikiPageVersionRepository
{
    private readonly WikiDbContext _context;

    public EfWikiPageVersionRepository(WikiDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(WikiPageVersion version, CancellationToken ct = default)
    {
        _context.WikiPageVersions.Add(version);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<WikiPageVersion>> GetByWikiPageIdAsync(Guid wikiPageId, CancellationToken ct = default)
        => await _context.WikiPageVersions
            .Where(v => v.WikiPageId == wikiPageId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

    public async Task<WikiPageVersion?> GetByWikiPageIdAndVersionNumberAsync(
        Guid wikiPageId, int versionNumber, CancellationToken ct = default)
        => await _context.WikiPageVersions
            .FirstOrDefaultAsync(v => v.WikiPageId == wikiPageId && v.VersionNumber == versionNumber, ct);

    public async Task DeleteAllForWikiPageAsync(Guid wikiPageId, CancellationToken ct = default)
        => await _context.WikiPageVersions
            .Where(v => v.WikiPageId == wikiPageId)
            .ExecuteDeleteAsync(ct);
}
