namespace Atlas.Modules.Wiki.Application.WikiPages;

// "Popüler Makaleler" BİLEREK burada yok - görüntülenme sayısı takibi henüz
// yok, kullanıcı bunu kendisi "ileride" olarak işaretledi; şimdiden yer
// tutucu bir alan eklemek (hep 0 dönen) YAGNI ihlali olurdu.
public record WikiDashboardDto(
    int TotalPageCount,
    int AddedThisWeekCount,
    int UpdatedThisWeekCount,
    IReadOnlyList<WikiDashboardCardDto> RecentlyAdded,
    IReadOnlyList<WikiDashboardCardDto> RecentlyUpdated,
    int DepartmentSpecificCount,
    IReadOnlyList<WikiDashboardCardDto> DepartmentSpecific,
    IReadOnlyList<WikiTagCountDto> PopularTags);

public record WikiDashboardCardDto(
    Guid Id,
    string Title,
    string Excerpt,
    string? CreatedByEmail,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string DepartmentName,
    string? CoverImageUrl);

public record WikiTagCountDto(string Tag, int Count);
