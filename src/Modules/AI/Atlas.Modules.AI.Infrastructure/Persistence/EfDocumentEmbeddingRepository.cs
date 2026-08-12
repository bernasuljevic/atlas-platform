using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Atlas.Modules.AI.Infrastructure.Persistence;

// EfWikiPageEmbeddingRepository'nin BİREBİR kopyası - tarih filtresi Kind=Utc
// düzeltmesi (Ders #18) ve NaN/Infinity savunması (Ders #15) dahil.
public class EfDocumentEmbeddingRepository : IDocumentEmbeddingRepository
{
    private readonly AiDbContext _context;

    public EfDocumentEmbeddingRepository(AiDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<DocumentEmbedding> embeddings, CancellationToken cancellationToken = default)
    {
        _context.DocumentEmbeddings.AddRange(embeddings);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentEmbeddingSearchHit>> FindNearestAsync(
        float[] queryEmbedding, int limit, DateTime? fromUtc = null, DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var vector = new Vector(queryEmbedding);

        var query = _context.DocumentEmbeddings.AsQueryable();

        if (fromUtc is not null)
        {
            var from = DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc);
            query = query.Where(e => e.CreatedAtUtc >= from);
        }

        if (toUtc is not null)
        {
            var exclusiveUpperBound = DateTime.SpecifyKind(toUtc.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(e => e.CreatedAtUtc < exclusiveUpperBound);
        }

        var rows = await query
            .Select(e => new { Embedding = e, Distance = e.Embedding.CosineDistance(vector) })
            .OrderBy(x => x.Distance)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Where(x => !double.IsNaN(x.Distance) && !double.IsInfinity(x.Distance))
            .Select(x => new DocumentEmbeddingSearchHit(x.Embedding, x.Distance))
            .ToList();
    }

    public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await _context.DocumentEmbeddings
            .Where(e => e.DocumentId == documentId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
