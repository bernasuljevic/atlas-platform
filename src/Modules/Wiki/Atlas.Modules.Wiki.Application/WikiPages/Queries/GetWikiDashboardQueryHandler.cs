using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

public class GetWikiDashboardQueryHandler : IRequestHandler<GetWikiDashboardQuery, WikiDashboardDto>
{
    private const int ExcerptLength = 150;
    private const int PopularTagsCount = 5;

    private readonly ISender _sender;
    private readonly ICurrentUserAccessor _currentUser;

    // GetWikiPagesQueryHandler'daki AYNI desen: ham (cache'lenen) veriyi
    // GetAllWikiPagesRawQuery üzerinden çekip görünürlük filtresini burada
    // uyguluyoruz - Dashboard'un kendi ayrı bir cache'e ya da veritabanı
    // sorgusuna ihtiyacı yok, zaten var olan 30 saniyelik cache'i paylaşıyor.
    public GetWikiDashboardQueryHandler(ISender sender, ICurrentUserAccessor currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    public async Task<WikiDashboardDto> Handle(GetWikiDashboardQuery request, CancellationToken cancellationToken)
    {
        var allPageDtos = await _sender.Send(new GetAllWikiPagesRawQuery(), cancellationToken);

        var effectiveDepartment = _currentUser.IsAuthenticated ? _currentUser.Department : null;
        var viewerIsAdmin = _currentUser.IsAuthenticated && _currentUser.IsAdmin;

        var visiblePages = allPageDtos
            .Where(p => IsVisibleTo(p, effectiveDepartment, viewerIsAdmin))
            .ToList();

        var oneWeekAgo = DateTime.UtcNow.AddDays(-7);

        var recentlyAdded = visiblePages
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(request.ItemsPerSection)
            .Select(ToCard)
            .ToList();

        var recentlyUpdatedAll = visiblePages
            .Where(p => p.UpdatedAtUtc is not null)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .ToList();
        var recentlyUpdated = recentlyUpdatedAll.Take(request.ItemsPerSection).Select(ToCard).ToList();

        // Departmanı olmayan (ya da giriş yapmamış) bir kullanıcı için bu liste
        // BİLEREK boş - göstermek üzere anlamlı bir "kendi departmanı" yok,
        // frontend bu durumda bölümü hiç render etmiyor.
        var departmentPages = effectiveDepartment is null
            ? new List<WikiPageDto>()
            : visiblePages
                .Where(p => string.Equals(p.DepartmentName, effectiveDepartment, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.CreatedAtUtc)
                .ToList();
        var departmentSpecific = departmentPages.Take(request.ItemsPerSection).Select(ToCard).ToList();

        // "Popüler Kategoriler" - ayrı bir Category kavramı YOK, WikiPage.Tags
        // (bkz. o entity'deki not) üzerinden hesaplanan bir etiket sıklığı
        // (frequency) listesi. GetAllWikiPagesRawQuery zaten (cache'lenmiş)
        // burada duruyor - ekstra bir sorgu/endpoint gerekmedi.
        var popularTags = visiblePages
            .Where(p => p.Tags is not null)
            .SelectMany(p => p.Tags!.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .GroupBy(tag => tag)
            .Select(g => new WikiTagCountDto(g.Key, g.Count()))
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Tag)
            .Take(PopularTagsCount)
            .ToList();

        return new WikiDashboardDto(
            visiblePages.Count,
            visiblePages.Count(p => p.CreatedAtUtc >= oneWeekAgo),
            recentlyUpdatedAll.Count(p => p.UpdatedAtUtc >= oneWeekAgo),
            recentlyAdded,
            recentlyUpdated,
            departmentPages.Count,
            departmentSpecific,
            popularTags);
    }

    private static bool IsVisibleTo(WikiPageDto page, string? viewerDepartmentName, bool viewerIsAdmin)
    {
        var visibility = Enum.Parse<WikiVisibility>(page.Visibility);
        return WikiVisibilityRules.IsVisibleTo(visibility, page.DepartmentName, viewerDepartmentName, viewerIsAdmin);
    }

    private static WikiDashboardCardDto ToCard(WikiPageDto p) => new(
        p.Id, p.Title, Excerpt(p.Content), p.CreatedByEmail, p.CreatedAtUtc, p.UpdatedAtUtc, p.DepartmentName);

    // WikiPageTable.jsx'teki truncateContent ile AYNI fikir, sadece backend
    // tarafında - kart önizlemesi ham içeriğin (henüz markdown olarak render
    // edilmemiş) ilk birkaç yüz karakteri.
    private static string Excerpt(string content)
    {
        var trimmed = content.Trim();
        return trimmed.Length <= ExcerptLength ? trimmed : trimmed[..ExcerptLength].TrimEnd() + "…";
    }
}
