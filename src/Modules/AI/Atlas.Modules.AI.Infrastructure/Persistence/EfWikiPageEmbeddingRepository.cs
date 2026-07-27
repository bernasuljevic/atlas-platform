using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Atlas.Modules.AI.Infrastructure.Persistence;

public class EfWikiPageEmbeddingRepository : IWikiPageEmbeddingRepository
{
    private readonly AiDbContext _context;

    public EfWikiPageEmbeddingRepository(AiDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<WikiPageEmbedding> embeddings, CancellationToken cancellationToken = default)
    {
        _context.WikiPageEmbeddings.AddRange(embeddings);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WikiPageEmbeddingSearchHit>> FindNearestAsync(
        float[] queryEmbedding, int limit, CancellationToken cancellationToken = default)
    {
        var vector = new Vector(queryEmbedding);

        // CosineDistance, Pgvector.EntityFrameworkCore'un LINQ'ü Postgres'in
        // "<=>" operatörüne çeviren extension metodu - sıralama VE mesafe
        // hesaplaması Postgres tarafında yapılıyor, .NET tarafına sadece
        // zaten sıralı "limit" kadar satır (mesafesiyle birlikte) geliyor.
        var rows = await _context.WikiPageEmbeddings
            .Select(e => new { Embedding = e, Distance = e.Embedding.CosineDistance(vector) })
            .OrderBy(x => x.Distance)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new WikiPageEmbeddingSearchHit(x.Embedding, x.Distance))
            .ToList();
    }
}
