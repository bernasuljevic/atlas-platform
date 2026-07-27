using MediatR;

namespace Atlas.Modules.AI.Application.WikiPages.Queries;

/// <summary>
/// Doğal dil sorgusunu embed edip pgvector benzerlik sıralamasıyla en anlamlı
/// wiki chunk'larını döndürür. "TopN", her biri kendi WikiPageId'sine göre
/// GRUPLANMIŞ (bir sayfadan en fazla bir sonuç) sonuç sayısı - chunk sayısı değil.
/// </summary>
public record SearchWikiPagesByMeaningQuery(string QueryText, int TopN = 5) : IRequest<IReadOnlyList<WikiSearchResultDto>>;

public record WikiSearchResultDto(Guid WikiPageId, string Title, string DepartmentName, string ChunkText, double Score);
