using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence;

public class EfWikiPageRepository : IWikiPageRepository
{
    private readonly WikiDbContext _context;

    public EfWikiPageRepository(WikiDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<WikiPage>> GetAllAsync(CancellationToken ct = default)
        => await _context.WikiPages.ToListAsync(ct);

    public async Task<WikiPage?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.WikiPages.FindAsync([id], ct);

    public async Task<IReadOnlyList<WikiPage>> GetByDepartmentAsync(string departmentName, CancellationToken ct = default)
        => await _context.WikiPages
            .Where(p => p.DepartmentName == departmentName)
            .ToListAsync(ct);

    public Task AddAsync(WikiPage page, CancellationToken ct = default)
    {
        // SaveChangesAsync BİLEREK burada yok - bkz. IWikiPageRepository'deki not.
        _context.WikiPages.Add(page);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WikiPage page, CancellationToken ct = default)
    {
        _context.WikiPages.Remove(page);
        return Task.CompletedTask;
    }
}
