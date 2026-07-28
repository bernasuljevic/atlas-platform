using MediatR;

namespace Atlas.Modules.AI.Application.WikiPages.Queries;

/// <summary>
/// Doğal dil sorgusunu embed edip pgvector benzerlik sıralamasıyla en anlamlı
/// wiki chunk'larını döndürür. "TopN", her biri kendi WikiPageId'sine göre
/// GRUPLANMIŞ (bir sayfadan en fazla bir sonuç) sonuç sayısı - chunk sayısı değil.
///
/// FromUtc/ToUtc BİLEREK opsiyonel (2026-07-28, kullanıcı isteğiyle eklendi) -
/// normal semantik aramaya EK, isteğe bağlı bir daraltma. Sayfanın embedding'inin
/// oluşturulma zamanına (WikiPageEmbedding.CreatedAtUtc - pratikte sayfanın
/// KENDİ oluşturulma zamanıyla aynı an, çünkü embedding sayfa oluşur oluşmaz
/// üretiliyor) göre filtreliyor, mesafe sıralamasından ÖNCE (repository
/// seviyesinde) uygulanıyor - "bu tarih aralığındaki sayfalar arasında en
/// alakalı olanı bul" mantığı.
/// </summary>
public record SearchWikiPagesByMeaningQuery(
    string QueryText,
    int TopN = 5,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null) : IRequest<IReadOnlyList<WikiSearchResultDto>>;

public record WikiSearchResultDto(
    Guid WikiPageId, string Title, string DepartmentName, string ChunkText, double Score, DateTime CreatedAtUtc);
