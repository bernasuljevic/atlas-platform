using Atlas.Modules.AI.Domain.Entities;

namespace Atlas.Modules.AI.Application.Abstractions;

public interface IWikiPageEmbeddingRepository
{
    Task AddRangeAsync(IEnumerable<WikiPageEmbedding> embeddings, CancellationToken cancellationToken = default);
}
