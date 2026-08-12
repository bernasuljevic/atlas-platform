using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Documents.Infrastructure.Persistence;

// P4 Gün 3'te Wiki'nin EfWikiPageRepository/EfWikiFolderRepository'siyle AYNI
// desene geçti - metotlar artık SaveChanges ÇAĞIRMIYOR, sadece change
// tracker'a ekliyor (bkz. IDocumentRepository'deki not).
public class EfDocumentRepository : IDocumentRepository
{
    private readonly DocumentsDbContext _context;

    public EfDocumentRepository(DocumentsDbContext context)
    {
        _context = context;
    }

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Documents.OrderByDescending(d => d.CreatedAtUtc).ToListAsync(ct);

    public Task AddAsync(Document document, CancellationToken ct = default)
    {
        _context.Documents.Add(document);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Document document, CancellationToken ct = default)
    {
        _context.Documents.Update(document);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Document document, CancellationToken ct = default)
    {
        _context.Documents.Remove(document);
        return Task.CompletedTask;
    }
}
