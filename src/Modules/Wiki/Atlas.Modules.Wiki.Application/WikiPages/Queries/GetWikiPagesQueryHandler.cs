using Atlas.Modules.Wiki.Application.Abstractions;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Caching.Abstractions;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

public class GetWikiPagesQueryHandler : IRequestHandler<GetWikiPagesQuery, PagedResult<WikiPageDto>>
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

    public async Task<PagedResult<WikiPageDto>> Handle(GetWikiPagesQuery request, CancellationToken cancellationToken)
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

        // Departman artık TAMAMEN ICurrentUserAccessor'dan (JWT'deki imzalı claim)
        // geliyor - istemcinin göndereceği hiçbir değer bunu değiştiremez. Giriş
        // yapmamış ya da departmansız bir kullanıcı için bu zaten null olur,
        // dolayısıyla sadece Public sayfalar görünür.
        var effectiveDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;

        // Sayfalama, filtrelemeden SONRA bellekte uygulanıyor - cache'te hâlâ
        // TÜM sayfalar (filtresiz, sayfalanmamış) duruyor, her pageNumber/pageSize
        // kombinasyonu için ayrı bir cache key açmıyoruz.
        // OrderBy şart: Skip/Take'in tutarlı sonuç vermesi için sıralama sabit olmalı,
        // en yeni sayfa en üstte.
        var visiblePages = allPageDtos
            .Where(p => IsVisibleTo(p, effectiveDepartment))
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToList();

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var pageItems = visiblePages
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<WikiPageDto>(pageItems, pageNumber, pageSize, visiblePages.Count);
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