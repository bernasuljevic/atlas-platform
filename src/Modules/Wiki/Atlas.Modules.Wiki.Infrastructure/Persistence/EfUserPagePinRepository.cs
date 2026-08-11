using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence;

public class EfUserPagePinRepository : IUserPagePinRepository
{
    private readonly WikiDbContext _context;

    public EfUserPagePinRepository(WikiDbContext context)
    {
        _context = context;
    }

    public async Task<UserPagePin?> GetAsync(Guid userId, Guid wikiPageId, CancellationToken ct = default)
        => await _context.UserPagePins
            .FirstOrDefaultAsync(p => p.UserId == userId && p.WikiPageId == wikiPageId, ct);

    public async Task<IReadOnlyList<UserPagePin>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _context.UserPagePins
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);

    public Task AddAsync(UserPagePin pin, CancellationToken ct = default)
    {
        _context.UserPagePins.Add(pin);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(UserPagePin pin, CancellationToken ct = default)
    {
        _context.UserPagePins.Remove(pin);
        return Task.CompletedTask;
    }
}
