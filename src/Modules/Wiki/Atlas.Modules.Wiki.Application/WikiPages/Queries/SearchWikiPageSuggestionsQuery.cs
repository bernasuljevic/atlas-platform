using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

// SearchByMeaningQuery'den (AI modülü) BİLEREK AYRI: o embedding
// üretip pgvector'da anlam bazlı arama yapıyor - her tuş vuruşunda çağırmak
// pratik değil. Bu sorgu sadece başlık/içerik üzerinde basit bir alt-metin
// (substring) eşleşmesi yapıyor, GetAllWikiPagesRawQuery'nin zaten var olan
// 30 saniyelik cache'ini paylaşıyor - maliyeti neredeyse sıfır, gerçek
// zamanlı (harf harf) çağrılmaya uygun.
public record SearchWikiPageSuggestionsQuery(string Query, int Limit = 8)
    : IRequest<IReadOnlyList<WikiSearchSuggestionDto>>;

public record WikiSearchSuggestionDto(Guid Id, string Title, string DepartmentName, string Excerpt);
