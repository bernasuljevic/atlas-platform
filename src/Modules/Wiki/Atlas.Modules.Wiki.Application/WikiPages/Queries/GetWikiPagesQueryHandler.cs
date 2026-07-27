using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

public class GetWikiPagesQueryHandler : IRequestHandler<GetWikiPagesQuery, PagedResult<WikiPageDto>>
{
    private readonly ISender _sender;
    private readonly ICurrentUserAccessor _currentUser;

    public GetWikiPagesQueryHandler(ISender sender, ICurrentUserAccessor currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<WikiPageDto>> Handle(GetWikiPagesQuery request, CancellationToken cancellationToken)
    {
        // Cache okuma/yazma artık burada değil - CachingBehavior, GetAllWikiPagesRawQuery
        // pipeline'ına otomatik giriyor (bkz. Shared.CQRS/Behaviors/CachingBehavior.cs).
        // Bu Handler artık sadece "filtrele + sayfala" sorumluluğunu taşıyor.
        var allPageDtos = await _sender.Send(new GetAllWikiPagesRawQuery(), cancellationToken);

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
