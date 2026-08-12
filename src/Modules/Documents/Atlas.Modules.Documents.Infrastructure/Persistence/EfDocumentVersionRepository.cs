using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Modules.Documents.Infrastructure.Persistence;

public class EfDocumentVersionRepository : IDocumentVersionRepository
{
    private readonly DocumentsDbContext _context;

    public EfDocumentVersionRepository(DocumentsDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(DocumentVersion version, CancellationToken ct = default)
    {
        _context.DocumentVersions.Add(version);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<DocumentVersion>> GetByDocumentIdAsync(Guid documentId, CancellationToken ct = default)
        => await _context.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

    public async Task<DocumentVersion?> GetByDocumentIdAndVersionNumberAsync(
        Guid documentId, int versionNumber, CancellationToken ct = default)
        => await _context.DocumentVersions
            .FirstOrDefaultAsync(v => v.DocumentId == documentId && v.VersionNumber == versionNumber, ct);

    public async Task DeleteAllForDocumentAsync(Guid documentId, CancellationToken ct = default)
        => await _context.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .ExecuteDeleteAsync(ct);
}
