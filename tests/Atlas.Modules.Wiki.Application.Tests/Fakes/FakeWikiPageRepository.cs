using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;

namespace Atlas.Modules.Wiki.Application.Tests.Fakes;

public class FakeWikiPageRepository : IWikiPageRepository
{
    public List<WikiPage> AddedPages { get; } = new();

    public Task<IReadOnlyList<WikiPage>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<WikiPage>)AddedPages);

    public Task<WikiPage?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(AddedPages.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<WikiPage>> GetByDepartmentAsync(string departmentName, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<WikiPage>)AddedPages
            .Where(p => p.DepartmentName == departmentName)
            .ToList());

    public Task AddAsync(WikiPage page, CancellationToken ct = default)
    {
        AddedPages.Add(page);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WikiPage page, CancellationToken ct = default)
    {
        AddedPages.Remove(page);
        return Task.CompletedTask;
    }
}
