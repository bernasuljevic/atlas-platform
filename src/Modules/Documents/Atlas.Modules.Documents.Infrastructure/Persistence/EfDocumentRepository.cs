using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Documents.Infrastructure.Persistence;

// Vault'un EfPasswordEntryRepository'siyle BİREBİR aynı desen - her metot
// kendi SaveChangesAsync'ini çağırıyor (bkz. IDocumentRepository'deki not,
// P4'e kadar Outbox/IUnitOfWork yok).
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

    public async Task AddAsync(Document document, CancellationToken ct = default)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Document document, CancellationToken ct = default)
    {
        _context.Documents.Update(document);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Document document, CancellationToken ct = default)
    {
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(ct);
    }
}
