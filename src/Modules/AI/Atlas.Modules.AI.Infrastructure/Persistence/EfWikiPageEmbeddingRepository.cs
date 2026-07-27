using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Domain.Entities;

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
}
