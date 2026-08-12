using Atlas.Modules.Documents.Application.Abstractions;

namespace Atlas.Modules.Documents.Infrastructure.Persistence;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly DocumentsDbContext _context;

    public EfUnitOfWork(DocumentsDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
