using Atlas.Modules.Wiki.Application.Abstractions;

namespace Atlas.Modules.Wiki.Infrastructure.Persistence;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly WikiDbContext _context;

    public EfUnitOfWork(WikiDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
