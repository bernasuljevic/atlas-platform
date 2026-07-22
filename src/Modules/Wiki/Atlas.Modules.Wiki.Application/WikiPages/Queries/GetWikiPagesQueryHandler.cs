using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Caching.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

public class GetWikiPagesQueryHandler : IRequestHandler<GetWikiPagesQuery, IReadOnlyList<WikiPageDto>>
{
    private const string AllPagesCacheKey = "wiki-pages:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly IWikiPageRepository _wikiPageRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ICacheService _cache;

    public GetWikiPagesQueryHandler(
        IWikiPageRepository wikiPageRepository,
        ICurrentUserAccessor currentUser,
        ICacheService cache)
    {
        _wikiPageRepository = wikiPageRepository;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<IReadOnlyList<WikiPageDto>> Handle(GetWikiPagesQuery request, CancellationToken cancellationToken)
    {
        var cachedPages = await _cache.GetAsync<List<WikiPageDto>>(AllPagesCacheKey, cancellationToken);

        List<WikiPageDto> allPageDtos;

        if (cachedPages is not null)
        {
            allPageDtos = cachedPages;
        }
        else
        {
            var pagesFromDb = await _wikiPageRepository.GetAllAsync(cancellationToken);

            allPageDtos = pagesFromDb
                .Select(p => new WikiPageDto(
                    p.Id, p.Title, p.Content, p.DepartmentName,
                    p.Visibility.ToString(), p.CreatedByUserId, p.CreatedAtUtc))
                .ToList();

            await _cache.SetAsync(AllPagesCacheKey, allPageDtos, CacheDuration, cancellationToken);
        }

        // Giriş yapmamış bir kullanıcı departman parametresi gönderse bile,
        // bunu SESSİZCE yok sayıyoruz - hata fırlatmıyoruz, sadece filtreyi
        // devre dışı bırakıyoruz. Sonuç: giriş yapmamış herkes sadece Public
        // sayfaları görür, departman adını tahmin etmek işe yaramaz.
        var effectiveDepartment = _currentUser.IsAuthenticated ? request.ViewerDepartmentName : null;

        return allPageDtos
            .Where(p => IsVisibleTo(p, effectiveDepartment))
            .ToList();
    }

    // Cache'lenen veri DTO olduğu için (Visibility burada string), önce enum'a geri
    // çeviriyoruz, sonra WikiPage ile AYNI paylaşılan kuralı (WikiVisibilityRules)
    // çağırıyoruz - kod tekrarı yok, tek doğruluk kaynağı Domain katmanında.
    private static bool IsVisibleTo(WikiPageDto page, string? viewerDepartmentName)
    {
        var visibility = Enum.Parse<WikiVisibility>(page.Visibility);
        return WikiVisibilityRules.IsVisibleTo(visibility, page.DepartmentName, viewerDepartmentName);
    }
}