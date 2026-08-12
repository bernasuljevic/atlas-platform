using Atlas.Modules.Documents.Application.Abstractions;
using Atlas.Modules.Documents.Domain.Entities;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Documents.Application.Documents.Queries;

public class SearchDocumentSuggestionsQueryHandler
    : IRequestHandler<SearchDocumentSuggestionsQuery, IReadOnlyList<DocumentSearchSuggestionDto>>
{
    private const int ExcerptLength = 100;

    private readonly IDocumentRepository _documentRepository;
    private readonly IWikiVisibilityChecker _visibilityChecker;
    private readonly ICurrentUserAccessor _currentUser;

    public SearchDocumentSuggestionsQueryHandler(
        IDocumentRepository documentRepository, IWikiVisibilityChecker visibilityChecker, ICurrentUserAccessor currentUser)
    {
        _documentRepository = documentRepository;
        _visibilityChecker = visibilityChecker;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<DocumentSearchSuggestionDto>> Handle(
        SearchDocumentSuggestionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return Array.Empty<DocumentSearchSuggestionDto>();

        var allDocuments = await _documentRepository.GetAllAsync(cancellationToken);

        var effectiveDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;

        var visibleDocuments = allDocuments.Where(d =>
            _visibilityChecker.IsVisibleTo(d.Visibility.ToString(), d.DepartmentName, effectiveDepartment, viewerIsAdmin));

        // SearchWikiPageSuggestionsQueryHandler'daki AYNI katmanlama fikri
        // (başlık > etiket) - ama içerik katmanı YOK (bkz. Query'deki not).
        var titleMatches = visibleDocuments
            .Where(d => d.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var tagMatches = visibleDocuments
            .Where(d => !d.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase)
                && d.Tags != null && d.Tags.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return titleMatches
            .Concat(tagMatches)
            .Take(request.Limit)
            .Select(d => new DocumentSearchSuggestionDto(d.Id, d.Title, d.DepartmentName, Excerpt(d)))
            .ToList();
    }

    // Description düz metin (markdown DEĞİL) - Wiki'nin MarkdownExcerptHelper'ına
    // ihtiyaç yok, basit bir kırpma yeterli.
    private static string? Excerpt(Document document)
    {
        if (string.IsNullOrWhiteSpace(document.Description))
            return null;

        return document.Description.Length <= ExcerptLength
            ? document.Description
            : document.Description[..ExcerptLength] + "...";
    }
}
