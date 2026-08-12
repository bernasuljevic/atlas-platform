using Atlas.Modules.AI.Domain.Entities;

namespace Atlas.Modules.AI.Application.Abstractions;

public interface IWikiPageEmbeddingRepository
{
    Task AddRangeAsync(IEnumerable<WikiPageEmbedding> embeddings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen vektöre pgvector'ın cosine distance operatörüyle (<c>&lt;=&gt;</c>)
    /// en yakın "limit" kadar chunk'ı, mesafe değeriyle BİRLİKTE döndürür -
    /// sıralama veritabanı tarafında yapılıyor (tüm satırları çekip bellekte
    /// sıralamak, tablo büyüdükçe ölçeklenmezdi). Görünürlük filtresi BURADA
    /// uygulanmıyor - repository güvenlik kuralını bilmemeli, o filtre
    /// Handler'da IWikiVisibilityChecker ile yapılıyor (bkz.
    /// SearchByMeaningQueryHandler).
    /// </summary>
    // fromUtc/toUtc opsiyonel - verilirse mesafe sıralamasından ÖNCE (bkz.
    // implementasyon) tarih aralığına göre daraltıyor: "bu tarih aralığındaki
    // sayfalar arasında en alakalı olanı bul".
    Task<IReadOnlyList<WikiPageEmbeddingSearchHit>> FindNearestAsync(
        float[] queryEmbedding, int limit, DateTime? fromUtc = null, DateTime? toUtc = null,
        CancellationToken cancellationToken = default);

    // WikiPageDeletedEvent geldiğinde çağrılıyor - bir sayfanın TÜM chunk'larını
    // (WikiPageId eşleşen her satırı) temizliyor. Bunsuz, silinen bir sayfanın
    // embedding'leri Postgres'te sonsuza kadar yetim kalır, arama onu "hayalet"
    // bir sonuç olarak göstermeye devam eder (canlı doğrulanmış bir bug).
    Task DeleteByWikiPageIdAsync(Guid wikiPageId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cosine distance 0 (aynı yön) ile 2 (tam ters yön) arasında - Handler bunu
/// kullanıcıya daha sezgisel bir "benzerlik skoru"na (1 - distance, yüksek
/// olan daha benzer) çevirecek.
/// </summary>
public record WikiPageEmbeddingSearchHit(WikiPageEmbedding Embedding, double Distance);
