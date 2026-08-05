using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence;

public class EfWikiFolderRepository : IWikiFolderRepository
{
    private readonly WikiDbContext _context;

    public EfWikiFolderRepository(WikiDbContext context)
    {
        _context = context;
    }

    public async Task<WikiFolder?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.WikiFolders.FindAsync([id], ct);

    public async Task<IReadOnlyList<WikiFolder>> GetByDepartmentAsync(string departmentName, CancellationToken ct = default)
        => await _context.WikiFolders
            .Where(f => f.DepartmentName == departmentName)
            .ToListAsync(ct);

    public Task AddAsync(WikiFolder folder, CancellationToken ct = default)
    {
        _context.WikiFolders.Add(folder);
        return Task.CompletedTask;
    }
}
