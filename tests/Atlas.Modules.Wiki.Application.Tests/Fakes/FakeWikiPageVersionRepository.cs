using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;

namespace Atlas.Modules.Wiki.Application.Tests.Fakes;

public class FakeWikiPageVersionRepository : IWikiPageVersionRepository
{
    public List<WikiPageVersion> Versions { get; } = new();

    public Task AddAsync(WikiPageVersion version, CancellationToken ct = default)
    {
        Versions.Add(version);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WikiPageVersion>> GetByWikiPageIdAsync(Guid wikiPageId, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<WikiPageVersion>)Versions
            .Where(v => v.WikiPageId == wikiPageId)
            .OrderByDescending(v => v.VersionNumber)
            .ToList());

    public Task<WikiPageVersion?> GetByWikiPageIdAndVersionNumberAsync(
        Guid wikiPageId, int versionNumber, CancellationToken ct = default)
        => Task.FromResult(Versions.FirstOrDefault(v => v.WikiPageId == wikiPageId && v.VersionNumber == versionNumber));

    public Task DeleteAllForWikiPageAsync(Guid wikiPageId, CancellationToken ct = default)
    {
        Versions.RemoveAll(v => v.WikiPageId == wikiPageId);
        return Task.CompletedTask;
    }
}
