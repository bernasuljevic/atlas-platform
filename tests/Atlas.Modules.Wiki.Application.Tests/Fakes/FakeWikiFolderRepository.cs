using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;

namespace Atlas.Modules.Wiki.Application.Tests.Fakes;

public class FakeWikiFolderRepository : IWikiFolderRepository
{
    public List<WikiFolder> AddedFolders { get; } = new();

    public Task<WikiFolder?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(AddedFolders.FirstOrDefault(f => f.Id == id));

    public Task<IReadOnlyList<WikiFolder>> GetByDepartmentAsync(string departmentName, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<WikiFolder>)AddedFolders
            .Where(f => f.DepartmentName == departmentName)
            .ToList());

    public Task AddAsync(WikiFolder folder, CancellationToken ct = default)
    {
        AddedFolders.Add(folder);
        return Task.CompletedTask;
    }
}
