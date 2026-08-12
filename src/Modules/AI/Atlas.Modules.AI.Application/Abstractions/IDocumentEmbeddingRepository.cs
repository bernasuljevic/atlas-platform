using Atlas.Modules.AI.Domain.Entities;

namespace Atlas.Modules.AI.Application.Abstractions;

// IWikiPageEmbeddingRepository'nin BİREBİR kopyası - DocumentId üzerinden.
// Bilerek AYRI bir interface/tablo (bkz. DocumentEmbedding'deki not) - ortak
// bir "IEmbeddingRepository<TResourceId>" soyutlaması İCAT EDİLMEDİ, çünkü
// FindNearestAsync'in filtre/sıralama mantığı ikisinde de aynı ama TResourceId
// generic'i sadece bir "WikiPageId" ile "DocumentId"yi aynı isimle çağırmak
// için var olurdu - okunurluğu artırmaz, sadece dolaylılık ekler.
public interface IDocumentEmbeddingRepository
{
    Task AddRangeAsync(IEnumerable<DocumentEmbedding> embeddings, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentEmbeddingSearchHit>> FindNearestAsync(
        float[] queryEmbedding, int limit, DateTime? fromUtc = null, DateTime? toUtc = null,
        CancellationToken cancellationToken = default);

    // DocumentDeletedEvent geldiğinde çağrılıyor - DeleteByWikiPageIdAsync'in
    // AYNI gerekçesi (hayalet arama sonucu bug'ı, bkz. CLAUDE.md).
    Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
}

public record DocumentEmbeddingSearchHit(DocumentEmbedding Embedding, double Distance);
