using Atlas.Modules.AI.Application.Abstractions;
using Atlas.Modules.AI.Domain.Entities;

namespace Atlas.Modules.AI.Application.Tests.Fakes;

/// <summary>
/// FakeWikiPageEmbeddingRepository'nin kopyası - AMA AddRangeAsync burada
/// no-op DEĞİL, gerçekten eklenenleri topluyor. Wiki tarafında hiçbir test
/// "add" akışını (GenerateWikiPageEmbeddingsCommandHandler) doğrudan test
/// etmiyordu; Documents tarafında sıfır-vektör filtresi/idempotent silme gibi
/// gerçek mantık taşıyan GenerateDocumentEmbeddingsCommandHandler için bu
/// izlenebilirlik gerekli.
/// </summary>
public class FakeDocumentEmbeddingRepository : IDocumentEmbeddingRepository
{
    private readonly IReadOnlyList<DocumentEmbeddingSearchHit> _hits;

    public List<DocumentEmbedding> AddedEmbeddings { get; } = new();
    public List<Guid> DeletedDocumentIds { get; } = new();

    public FakeDocumentEmbeddingRepository(IReadOnlyList<DocumentEmbeddingSearchHit>? hits = null)
    {
        _hits = hits ?? [];
    }

    public Task AddRangeAsync(IEnumerable<DocumentEmbedding> embeddings, CancellationToken cancellationToken = default)
    {
        AddedEmbeddings.AddRange(embeddings);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DocumentEmbeddingSearchHit>> FindNearestAsync(
        float[] queryEmbedding, int limit, DateTime? fromUtc = null, DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var filtered = _hits.Where(h =>
            (fromUtc is null || h.Embedding.CreatedAtUtc >= fromUtc) &&
            (toUtc is null || h.Embedding.CreatedAtUtc < toUtc.Value.Date.AddDays(1)));

        return Task.FromResult(filtered.Take(limit).ToList() as IReadOnlyList<DocumentEmbeddingSearchHit>);
    }

    public Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        DeletedDocumentIds.Add(documentId);
        return Task.CompletedTask;
    }
}
