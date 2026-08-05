using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

public class SearchWikiPageSuggestionsQueryHandler
    : IRequestHandler<SearchWikiPageSuggestionsQuery, IReadOnlyList<WikiSearchSuggestionDto>>
{
    private const int ExcerptLength = 100;

    private readonly ISender _sender;
    private readonly ICurrentUserAccessor _currentUser;

    public SearchWikiPageSuggestionsQueryHandler(ISender sender, ICurrentUserAccessor currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WikiSearchSuggestionDto>> Handle(
        SearchWikiPageSuggestionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return Array.Empty<WikiSearchSuggestionDto>();

        var allPageDtos = await _sender.Send(new GetAllWikiPagesRawQuery(), cancellationToken);

        var effectiveDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;

        var visiblePages = allPageDtos.Where(p => IsVisibleTo(p, effectiveDepartment, viewerIsAdmin));

        // Başlık eşleşmeleri İÇERİK eşleşmelerinden ÖNCE geliyor - kullanıcı
        // genelde aradığı sayfanın başlığını (ya da bir parçasını) yazıyor,
        // o zaman en alakalı sonuç en üstte çıkmalı. Etiket eşleşmeleri ikisinin
        // ARASINDA - bir etiket başlıktan daha kesin bir sinyal ("bu sayfa GERÇEKTEN
        // bu konuyla ilgili etiketlenmiş") ama içerikte geçen bir kelimeden daha
        // güçlü. Aynı sayfa birden fazla listede olmasın diye her katman kendinden
        // önceki(ler)de eşleşmiş sayfaları hariç tutuyor.
        var titleMatches = visiblePages
            .Where(p => p.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var tagMatches = visiblePages
            .Where(p => !p.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase)
                && p.Tags != null && p.Tags.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var contentMatches = visiblePages
            .Where(p => !p.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase)
                && (p.Tags is null || !p.Tags.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
                && p.Content.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return titleMatches
            .Concat(tagMatches)
            .Concat(contentMatches)
            .Take(request.Limit)
            .Select(p => new WikiSearchSuggestionDto(p.Id, p.Title, p.DepartmentName, Excerpt(p.Content)))
            .ToList();
    }

    private static bool IsVisibleTo(WikiPageDto page, string? viewerDepartmentName, bool viewerIsAdmin)
    {
        var visibility = Enum.Parse<WikiVisibility>(page.Visibility);
        return WikiVisibilityRules.IsVisibleTo(visibility, page.DepartmentName, viewerDepartmentName, viewerIsAdmin);
    }

    // GetWikiDashboardQueryHandler'daki AYNI düzeltme (bkz. MarkdownExcerptHelper) -
    // arama önerilerinde de ham markdown sızmasın diye paylaşılan yardımcıya taşındı.
    private static string Excerpt(string content) => MarkdownExcerptHelper.Truncate(content, ExcerptLength);
}
