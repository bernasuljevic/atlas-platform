using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Domain.Entities;

namespace Atlas.Modules.AI.Application.Tests.Fakes;

/// <summary>
/// Gerçek pgvector sıralamasını simüle etmiyor - test, hangi aday chunk'ların
/// (ve hangi mesafeyle) döndüğünü baştan biliyor, tıpkı Postgres'ten öyle
/// dönmüş gibi. Bu sayede Handler'ın filtreleme/gruplama/sıralama mantığı,
/// gerçek veritabanı bağlantısı olmadan test edilebiliyor.
/// </summary>
public class FakeWikiPageEmbeddingRepository : IWikiPageEmbeddingRepository
{
    private readonly IReadOnlyList<WikiPageEmbeddingSearchHit> _hits;

    public FakeWikiPageEmbeddingRepository(IReadOnlyList<WikiPageEmbeddingSearchHit> hits)
    {
        _hits = hits;
    }

    public Task AddRangeAsync(IEnumerable<WikiPageEmbedding> embeddings, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<WikiPageEmbeddingSearchHit>> FindNearestAsync(
        float[] queryEmbedding, int limit, CancellationToken cancellationToken = default)
        => Task.FromResult(_hits.Take(limit).ToList() as IReadOnlyList<WikiPageEmbeddingSearchHit>);
}
