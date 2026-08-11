using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence;

public class EfUserPageFavoriteRepository : IUserPageFavoriteRepository
{
    private readonly WikiDbContext _context;

    public EfUserPageFavoriteRepository(WikiDbContext context)
    {
        _context = context;
    }

    public async Task<UserPageFavorite?> GetAsync(Guid userId, Guid wikiPageId, CancellationToken ct = default)
        => await _context.UserPageFavorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.WikiPageId == wikiPageId, ct);

    public async Task<IReadOnlyList<UserPageFavorite>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _context.UserPageFavorites
            .Where(f => f.UserId == userId)
            .ToListAsync(ct);

    public Task AddAsync(UserPageFavorite favorite, CancellationToken ct = default)
    {
        _context.UserPageFavorites.Add(favorite);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(UserPageFavorite favorite, CancellationToken ct = default)
    {
        _context.UserPageFavorites.Remove(favorite);
        return Task.CompletedTask;
    }
}
