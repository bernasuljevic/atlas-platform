using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence;

public class EfWikiCommentRepository : IWikiCommentRepository
{
    private readonly WikiDbContext _context;

    public EfWikiCommentRepository(WikiDbContext context)
    {
        _context = context;
    }

    public async Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Comments.FindAsync([id], ct);

    public async Task<IReadOnlyList<Comment>> GetByPageIdAsync(Guid? pageId, CancellationToken ct = default)
        => await _context.Comments
            .Where(c => c.PageId == pageId)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(ct);

    public Task AddAsync(Comment comment, CancellationToken ct = default)
    {
        _context.Comments.Add(comment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Comment comment, CancellationToken ct = default)
    {
        _context.Comments.Remove(comment);
        return Task.CompletedTask;
    }
}
