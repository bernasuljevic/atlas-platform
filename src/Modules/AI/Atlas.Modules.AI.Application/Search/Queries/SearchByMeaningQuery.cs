using MediatR;

namespace Atlas.Modules.AI.Application.Search.Queries;

/// <summary>
/// P5'ten (Documents→AI/RAG entegrasyonu) itibaren SearchWikiPagesByMeaningQuery'nin
/// yerine geçti - isim değişti çünkü artık gerçekten sadece wiki sayfalarını
/// değil, Documents'ın da chunk'larını arıyor. Davranış (semantik olarak)
/// AYNI: doğal dil sorgusunu embed edip pgvector benzerlik sıralamasıyla en
/// anlamlı chunk'ları döndürüyor - "TopN" artık kaynak TÜRÜNDEN bağımsız,
/// her biri kendi kaynağına (bir wiki sayfası ya da bir belge) göre
/// GRUPLANMIŞ sonuç sayısı.
///
/// FromUtc/ToUtc BİLEREK opsiyonel (Wiki'deki orijinal gerekçe korunuyor) -
/// normal semantik aramaya EK, isteğe bağlı bir daraltma.
/// </summary>
public record SearchByMeaningQuery(
    string QueryText,
    int TopN = 5,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null) : IRequest<IReadOnlyList<SemanticSearchResultDto>>;

/// <summary>
/// SourceType BİLEREK string (enum DEĞİL) - Visibility'nin WikiPageEmbedding/
/// DocumentEmbedding'de string tutulmasıyla AYNI gerekçe: bir enum, System.Text.Json
/// tarafından (bu projede global bir JsonStringEnumConverter kayıtlı olmadığı için)
/// varsayılan olarak SAYI (0/1) olarak serileştirilirdi - istemci tarafında
/// okunaksız ve kırılgan olurdu. SearchResultSourceTypes sabitleri, olası
/// değerleri (typo riski olmadan) tek yerde tutuyor.
/// </summary>
public record SemanticSearchResultDto(
    string SourceType, Guid ResourceId, string Title, string DepartmentName,
    string ChunkText, double Score, DateTime CreatedAtUtc);

public static class SearchResultSourceTypes
{
    public const string WikiPage = "WikiPage";
    public const string Document = "Document";
}
