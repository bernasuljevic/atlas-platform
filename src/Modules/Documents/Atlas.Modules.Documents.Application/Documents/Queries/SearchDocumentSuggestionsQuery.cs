using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Queries;

/// <summary>
/// Wiki'nin SearchWikiPageSuggestionsQuery'sinin Documents tarafındaki karşılığı -
/// AYNI hafif, gerçek-zamanlı (harf harf çağrılabilir) öneri deseni. WikiEditorPage'in
/// link penceresi (P5 Gün 4) artık İKİ ayrı öneri endpoint'ini (bunu VE Wiki'ninkini)
/// birlikte çağırıp tek bir listede birleştiriyor - "document:GUID" içerik-referans
/// bloğunun (P2'de ertelenmişti, bkz. CLAUDE.md) bağlandığı yer burası.
///
/// Wiki'ninkinden TEK farkı: içerik (chunk metni) üzerinde arama YAPMIYOR - Document
/// entity kendi çıkarılmış metnini SAKLAMIYOR (o metin sadece AI'ın embedding'lerinde,
/// bkz. DocumentChunksReadyEvent), Documents'ın buraya erişmesi modül izolasyonunu
/// ihlal ederdi. Title + Tags eşleşmesi yeterli - "içerik bazlı bul" zaten AI'ın
/// SearchByMeaningQuery'sinin işi.
/// </summary>
public record SearchDocumentSuggestionsQuery(string Query, int Limit = 8)
    : IRequest<IReadOnlyList<DocumentSearchSuggestionDto>>;

public record DocumentSearchSuggestionDto(Guid Id, string Title, string DepartmentName, string? Excerpt);
