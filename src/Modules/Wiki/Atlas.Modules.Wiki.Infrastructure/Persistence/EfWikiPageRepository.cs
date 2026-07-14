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

    public async Task AddAsync(WikiPage page, CancellationToken ct = default)
    {
        _context.WikiPages.Add(page);
        await _context.SaveChangesAsync(ct);
    }
}
