using System.Text.RegularExpressions;
using Atlas.Modules.Wiki.Domain.Entities;
using Atlas.Modules.Wiki.Domain.Enums;
using Atlas.Shared.Contracts;
using MediatR;

namespace Atlas.Modules.Wiki.Application.WikiPages.Queries;

public class GetWikiDashboardQueryHandler : IRequestHandler<GetWikiDashboardQuery, WikiDashboardDto>
{
    // 150 -> 420 (2026-08-05, kullanıcı geri bildirimi: "yazıları daha fazla
    // gözüksün") - eskiden çoğu paragrafı ilk cümlesinde kesiyordu. Kısa bir
    // sayfa için bu limit hiç devreye girmiyor (bkz. MarkdownExcerptHelper.
    // Truncate - kısaltılmadıysa "…" eklenmiyor, frontend bunu "Devamı..."
    // linkini gösterip göstermeme kararında kullanıyor).
    private const int ExcerptLength = 420;
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
            .Take(request.RecentlyAddedCount)
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

        // İstatistik donut grafiği (Görsel Tasarım Yenileme Gün 2) -
        // PopularTags'teki AYNI "zaten bellekteki visiblePages'ten türet"
        // deseni, yeni bir sorgu YOK.
        var departmentBreakdown = visiblePages
            .GroupBy(p => p.DepartmentName)
            .Select(g => new WikiDepartmentCountDto(g.Key, g.Count()))
            .OrderByDescending(d => d.Count)
            .ToList();

        return new WikiDashboardDto(
            visiblePages.Count,
            visiblePages.Count(p => p.CreatedAtUtc >= oneWeekAgo),
            recentlyUpdatedAll.Count(p => p.UpdatedAtUtc >= oneWeekAgo),
            recentlyAdded,
            recentlyUpdated,
            departmentPages.Count,
            departmentSpecific,
            popularTags,
            departmentBreakdown);
    }

    private static bool IsVisibleTo(WikiPageDto page, string? viewerDepartmentName, bool viewerIsAdmin)
    {
        var visibility = Enum.Parse<WikiVisibility>(page.Visibility);
        return WikiVisibilityRules.IsVisibleTo(visibility, page.DepartmentName, viewerDepartmentName, viewerIsAdmin);
    }

    private static WikiDashboardCardDto ToCard(WikiPageDto p) => new(
        p.Id, p.Title, Excerpt(p.Content), p.CreatedByEmail, p.CreatedAtUtc, p.UpdatedAtUtc, p.DepartmentName,
        ExtractCoverImageUrl(p.Content));

    // WikiPageTable.jsx'teki truncateContent ile BENZER fikir, sadece backend
    // tarafında - kart önizlemesi ham içeriğin markdown işaretlerinden
    // arındırılmış ilk birkaç yüz karakteri (bkz. MarkdownExcerptHelper'daki
    // not - önceden ham içerik kısaltılıyordu, "[Başlık](wiki:GUID)" gibi
    // yarım kalmış sözdizimi önizlemeye sızıyordu).
    private static string Excerpt(string content) => MarkdownExcerptHelper.Truncate(content, ExcerptLength);

    // Ayrı bir "kapak görseli" alanı/yüklemesi YOK - sayfanın içeriğinde
    // markdown.jsx'in de anladığı AYNI "![alt](url)" söz dizimiyle geçen İLK
    // görsel, dashboard kartlarında (Haftanın Seçkin Maddesi kutusu, Son
    // Eklenen Makaleler ızgarası) küçük bir önizleme olarak kullanılıyor -
    // ekstra bir alan/endpoint eklemeden, zaten var olan içerikten türetiliyor.
    private static readonly Regex ImageMarkdownPattern = new(@"!\[[^\]]*\]\(([^)]+)\)", RegexOptions.Compiled);

    private static string? ExtractCoverImageUrl(string content)
    {
        var match = ImageMarkdownPattern.Match(content);
        return match.Success ? match.Groups[1].Value : null;
    }
}
