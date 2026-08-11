using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Application.WikiPages;
using Atlas.Modules.Wiki.Application.WikiPages.Queries;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.Favorites.Queries;

public class GetFavoritePagesQueryHandler : IRequestHandler<GetFavoritePagesQuery, IReadOnlyList<WikiPageDto>>
{
    private readonly IUserPageFavoriteRepository _favoriteRepository;
    private readonly ISender _sender;
    private readonly ICurrentUserAccessor _currentUser;

    public GetFavoritePagesQueryHandler(
        IUserPageFavoriteRepository favoriteRepository, ISender sender, ICurrentUserAccessor currentUser)
    {
        _favoriteRepository = favoriteRepository;
        _sender = sender;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WikiPageDto>> Handle(GetFavoritePagesQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return [];

        var favorites = await _favoriteRepository.GetByUserAsync(_currentUser.UserId.Value, cancellationToken);
        if (favorites.Count == 0)
            return [];

        // GetWikiPagesQueryHandler'daki AYNI cache'lenmiş ham veri - ekstra bir
        // DB sorgusu değil, CachingBehavior bunu zaten 30sn cache'liyor.
        var allPages = await _sender.Send(new GetAllWikiPagesRawQuery(), cancellationToken);
        var pagesById = allPages.ToDictionary(p => p.Id);

        var viewerDepartment = _currentUser.Department;
        var viewerIsAdmin = _currentUser.IsAdmin;

        // En son favoriye eklenen en üstte. Erişimi sonradan kaybedilmiş bir
        // sayfa (departman değişti, sayfa silindi vb.) BURADA sessizce elenir -
        // favorite kaydı DB'de durmaya devam eder ama listede hiç görünmez
        // (kullanıcının kendi talebi: "yetkisi olmayan kullanıcının Favorilerinde
        // görünmemeli").
        return favorites
            .OrderByDescending(f => f.CreatedAtUtc)
            .Select(f => pagesById.GetValueOrDefault(f.WikiPageId))
            .Where(page => page is not null && IsVisibleTo(page, viewerDepartment, viewerIsAdmin))
            .Select(page => page!)
            .ToList();
    }

    // GetWikiPagesQueryHandler'daki AYNI yardımcı - tek doğruluk kaynağı hâlâ
    // Domain'deki WikiVisibilityRules, burada sadece cache'lenmiş DTO'ya uygulanıyor.
    private static bool IsVisibleTo(WikiPageDto page, string? viewerDepartmentName, bool viewerIsAdmin)
    {
        var visibility = Enum.Parse<WikiVisibility>(page.Visibility);
        return WikiVisibilityRules.IsVisibleTo(visibility, page.DepartmentName, viewerDepartmentName, viewerIsAdmin);
    }
}
